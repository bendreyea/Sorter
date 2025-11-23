namespace App.ExternalSorter.Merge.Tournament;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>K‑way merge based on a tournament tree.</summary>
public sealed class TournamentTree<T>
{
    private readonly IEnumerator<T>[] _enumerators; // one per input
    private readonly T[]   _leafValues;             // values that are still "alive"
    private readonly int[] _tree;                  // winner index per node (‑1 = no active leaf)
    private readonly IComparer<T> _cmp;
    private readonly int _leafBase;                // start of leaves inside _tree
    private readonly int _k;                       // original input count
    private readonly bool[] _hasValue;             // track which enumerators have values
    private int _activeLeaves;

    /// <summary>
    /// Initializes a new instance of the <see cref="TournamentTree{T}"/> class.
    /// </summary>
    /// <param name="inputs">The sorted input sequences to merge.</param>
    /// <param name="comparer">The comparer to determine sort order.</param>
    public TournamentTree(IEnumerable<IEnumerable<T>> inputs,
                          IComparer<T>? comparer = null)
    {
        _cmp         = comparer ?? Comparer<T>.Default;
        _enumerators = inputs.Select(s => s.GetEnumerator()).ToArray();
        _k           = _enumerators.Length;

        if (_k == 0)
            throw new ArgumentException("At least one sequence is required.", nameof(inputs));

        // next power‑of‑two ≥ k
        int size = 1;
        while (size < _k) size <<= 1;

        _leafBase   = size;
        _leafValues = new T[_k];              // Only allocate what we need
        _tree       = new int[size * 2];      // index 1 is the root
        _hasValue   = new bool[_k];           // track validity

        // Initialize leaves
        for (int i = 0; i < _k; i++)
        {
            int leafIdx = _leafBase + i;

            if (_enumerators[i].MoveNext())
            {
                _leafValues[i] = _enumerators[i].Current;
                _tree[leafIdx] = i;
                _hasValue[i] = true;
                _activeLeaves++;
            }
            else
            {
                _tree[leafIdx] = -1;          // inactive
                _hasValue[i] = false;
            }
        }

        // Initialize empty leaves beyond k
        for (int i = _k; i < size; i++)
        {
            _tree[_leafBase + i] = -1;
        }

        // ---------- build internal nodes ----------
        for (int n = _leafBase - 1; n >= 1; n--)
            _tree[n] = Combine(_tree[n << 1], _tree[(n << 1) + 1]);
    }

    /// <summary>Return all inputs merged into increasing order.</summary>
    public IEnumerable<T> Merge()
    {
        // Fast path for single active sequence
        if (_activeLeaves == 1)
        {
            for (int i = 0; i < _k; i++)
            {
                if (_hasValue[i])
                {
                    yield return _leafValues[i];
                    while (_enumerators[i].MoveNext())
                    {
                        yield return _enumerators[i].Current;
                    }
                    _activeLeaves = 0;
                    break;
                }
            }
            Dispose();
            yield break;
        }

        // Use batching to reduce yield overhead
        const int BatchSize = 64;
        var buffer = ArrayPool<T>.Shared.Rent(BatchSize);
        int bufferPos = 0;

        try
        {
            while (_activeLeaves > 0)
            {
                int winner = _tree[1];            // root
                
                if (winner == -1)
                    break;

                // Batch results
                buffer[bufferPos++] = _leafValues[winner];
                
                if (bufferPos >= BatchSize)
                {
                    for (int i = 0; i < bufferPos; i++)
                        yield return buffer[i];
                    bufferPos = 0;
                }

                // advance the winning enumerator
                if (_enumerators[winner].MoveNext())
                {
                    _leafValues[winner] = _enumerators[winner].Current;
                    // Don't update the leaf node here - it stays the same
                }
                else
                {
                    _tree[_leafBase + winner] = -1;   // leaf goes inactive
                    _hasValue[winner] = false;
                    _activeLeaves--;
                    
                    // Fast path: if only one sequence left, stream it directly
                    if (_activeLeaves == 1)
                    {
                        // Yield buffered items first
                        for (int i = 0; i < bufferPos; i++)
                            yield return buffer[i];
                        bufferPos = 0;
                        
                        // Find and stream the last sequence
                        for (int i = 0; i < _k; i++)
                        {
                            if (_hasValue[i])
                            {
                                yield return _leafValues[i];
                                while (_enumerators[i].MoveNext())
                                {
                                    yield return _enumerators[i].Current;
                                }
                                _activeLeaves = 0;
                                break;
                            }
                        }
                        break;
                    }
                }

                // Recalculate path from the leaf to root
                UpdatePath(_leafBase + winner);
            }
            
            // Yield remaining buffered items
            for (int i = 0; i < bufferPos; i++)
                yield return buffer[i];
        }
        finally
        {
            ArrayPool<T>.Shared.Return(buffer);
            Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePath(int leafIndex)
    {
        // Update from the parent of the leaf up to the root
        int node = leafIndex >> 1;
        while (node >= 1)
        {
            int left = node << 1;
            int right = left + 1;
            _tree[node] = Combine(_tree[left], _tree[right]);
            node >>= 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Combine(int ia, int ib)
    {
        // Handle invalid indices
        if (ia == -1) 
            return ib;
        
        if (ib == -1)
            return ia;
        
        // Both are valid, compare their values
        return _cmp.Compare(_leafValues[ia], _leafValues[ib]) <= 0 ? ia : ib;
    }

    private void Dispose()
    {
        foreach (var e in _enumerators)
            e?.Dispose();
    }
}