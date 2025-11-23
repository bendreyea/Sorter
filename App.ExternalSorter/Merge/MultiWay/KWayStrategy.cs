namespace App.ExternalSorter.Merge.MultiWay;

/// <summary>
/// Implements a k-way merge strategy using a priority queue to efficiently merge multiple sorted sequences.
/// </summary>
/// <typeparam name="T">The type of elements in the sequences.</typeparam>
public class KWayStrategy<T>
{
    /// <summary>
    /// Merges multiple sorted sequences into a single sorted sequence using a k-way merge algorithm with a min-heap.
    /// </summary>
    /// <param name="sortedSequences">The collection of sorted sequences to merge.</param>
    /// <param name="comparer">The comparer used to determine the order of elements.</param>
    /// <returns>An enumerable of merged elements in sorted order.</returns>
    public IEnumerable<T> Merge(
        IEnumerable<IEnumerable<T>> sortedSequences,
        IComparer<T> comparer)
    {
        // 1) Materialize each input once into an array:
        var arrays = sortedSequences
            .Select(seq => seq as T[] ?? seq.ToArray())
            .Where(arr => arr.Length > 0)
            .ToArray();

        int k = arrays.Length;
        if (k == 0)
            yield break;

        // 2) Estimate total length and pre‑allocate result capacity:
        int totalLength = 0;
        foreach (var arr in arrays)
            totalLength += arr.Length;

        // 3) Use a min‐heap of (listIndex, elementIndex, value):
        //    ValueTuple<int,int,T> is a struct, so no per‑element heap alloc.
        var pq = new PriorityQueue<(int listIndex, int elementIndex, T value), T>(comparer);

        // 4) Seed the heap with the first element of each array:
        for (int i = 0; i < k; i++)
        {
            pq.Enqueue((i, 0, arrays[i][0]), arrays[i][0]);
        }

        // 5) Repeatedly pull the min and push its successor:
        //    We yield directly to the caller; if you really need a List,
        //    you could Collect into one with capacity = totalLength.
        while (pq.TryDequeue(out var node, out _))
        {
            yield return node.value;

            int nextIdx = node.elementIndex + 1;
            var source = arrays[node.listIndex];
            if (nextIdx < source.Length)
            {
                var nextValue = source[nextIdx];
                pq.Enqueue((node.listIndex, nextIdx, nextValue), nextValue);
            }
        }
    }
}
