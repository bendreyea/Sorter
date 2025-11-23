namespace App.ExternalSorter.Merge.PolyPhase;

using System.Runtime.CompilerServices;

/// <summary>
/// Implements polyphase merge sort strategy for in-memory sequences.
/// </summary>
/// <typeparam name="T">The type of elements to merge.</typeparam>
public class PolyPhaseStrategy<T>
{
    /// <summary>
    /// Merges multiple sorted sequences using the polyphase merge algorithm.
    /// </summary>
    /// <param name="sortedSequences">The sorted sequences to merge.</param>
    /// <param name="comparer">The comparer to determine sort order.</param>
    /// <returns>A single sorted sequence containing all elements.</returns>
    public IEnumerable<T> Merge(IEnumerable<IEnumerable<T>> sortedSequences, IComparer<T> comparer)
    {
        // Convert to arrays for fastest access
        var runs = sortedSequences.Select(seq => seq as T[] ?? seq.ToArray()).ToList();
        
        if (runs.Count == 0)
            return Array.Empty<T>();
        if (runs.Count == 1)
            return runs[0];
        
        // For small counts, use direct merge (faster than polyphase overhead)
        if (runs.Count <= 4)
            return MergeArraysDirect(runs, comparer);
        
        var tapes = DistributeRuns(runs);
        RunMergePhases(tapes, comparer);
        
        // Find the tape with remaining runs
        var finalTape = tapes[0].Count > 0 ? tapes[0] : (tapes[1].Count > 0 ? tapes[1] : tapes[2]);
        
        // Final merge phase
        while (finalTape.Count > 1)
        {
            var runA = finalTape.Dequeue();
            var runB = finalTape.Dequeue();
            finalTape.Enqueue(MergeTwoArrays(runA, runB, comparer));
        }
        
        return finalTape.Count > 0 ? finalTape.Dequeue() : Array.Empty<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] MergeTwoArrays(T[] left, T[] right, IComparer<T> comparer)
    {
        var result = new T[left.Length + right.Length];
        int i = 0, j = 0, k = 0;
        
        // Main merge loop - unrolled for better performance
        int leftLen = left.Length;
        int rightLen = right.Length;
        
        while (i < leftLen && j < rightLen)
        {
            if (comparer.Compare(left[i], right[j]) <= 0)
                result[k++] = left[i++];
            else
                result[k++] = right[j++];
        }
        
        // Copy remaining elements
        if (i < leftLen)
        {
            left.AsSpan(i).CopyTo(result.AsSpan(k));
        }
        else if (j < rightLen)
        {
            right.AsSpan(j).CopyTo(result.AsSpan(k));
        }
        
        return result;
    }

    private static Queue<T[]>[] DistributeRuns(List<T[]> runs)
    {
        var runCount = runs.Count;
        var (a, b, dummy) = runCount.FibonacciSplit();

        var tape1 = new Queue<T[]>(a);
        var tape2 = new Queue<T[]>(b);
        var tape3 = new Queue<T[]>(Math.Max(1, dummy)); // Ensure capacity

        int idx = 0;
        for (; idx < a && idx < runs.Count; idx++)
            tape1.Enqueue(runs[idx]);
        for (; idx < a + b && idx < runs.Count; idx++)
            tape2.Enqueue(runs[idx]);

        // Create dummy runs only if needed (empty arrays are cheap)
        for (int d = 0; d < dummy; d++)
            tape3.Enqueue(Array.Empty<T>());

        return new[] { tape1, tape2, tape3 };
    }
    
    private static void RunMergePhases(Queue<T[]>[] tapes, IComparer<T> comparer)
    {
        int output = tapes[0].Count == 0 ? 0 : (tapes[1].Count == 0 ? 1 : 2);

        while ((tapes[0].Count + tapes[1].Count + tapes[2].Count) > 1)
        {
            int in1 = (output + 1) % 3;
            int in2 = (output + 2) % 3;

            // Swap if needed
            if (tapes[in1].Count == 0 && tapes[in2].Count > 0)
                (in1, in2) = (in2, in1);

            int mergePairs = Math.Min(tapes[in1].Count, tapes[in2].Count);
            if (mergePairs == 0)
                break;

            // Merge all pairs in this phase
            for (int i = 0; i < mergePairs; i++)
            {
                var runA = tapes[in1].Dequeue();
                var runB = tapes[in2].Dequeue();
                
                // Skip empty dummy runs
                if (runA.Length == 0)
                    tapes[output].Enqueue(runB);
                else if (runB.Length == 0)
                    tapes[output].Enqueue(runA);
                else
                    tapes[output].Enqueue(MergeTwoArrays(runA, runB, comparer));
            }

            // Next output tape is the one that's now empty
            output = tapes[in1].Count == 0 ? in1 : in2;
        }
    }
    
    // Optimized direct merge for small numbers of runs
    private static T[] MergeArraysDirect(List<T[]> arrays, IComparer<T> comparer)
    {
        if (arrays.Count == 1)
            return arrays[0];
        
        if (arrays.Count == 2)
            return MergeTwoArrays(arrays[0], arrays[1], comparer);
        
        // For 3-4 arrays, use tree merge
        var result = MergeTwoArrays(arrays[0], arrays[1], comparer);
        for (int i = 2; i < arrays.Count; i++)
        {
            result = MergeTwoArrays(result, arrays[i], comparer);
        }
        return result;
    }
}