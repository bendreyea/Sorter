namespace App.ExternalSorter.Merge.Tournament;

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>K‑way merge of async sorted sequences using a tournament tree.</summary>
public sealed class AsyncTournamentTree<T> : IAsyncDisposable
{
    private readonly IList<IAsyncEnumerator<T>> _enums; // one per input
    private readonly T[] _leafVals; // current winners from each leaf
    private readonly int[] _tree; // winner index per node (‑1 = no active)
    private readonly IComparer<T> _cmp;
    private readonly int _leafBase; // first leaf index inside _tree
    private readonly int _k; // actual number of sequences
    private readonly bool[] _hasValue; // track which enumerators have values

    private int _activeLeaves; // how many enumerators still have data

    private AsyncTournamentTree(
        IList<IAsyncEnumerator<T>> enums,
        IComparer<T> cmp,
        int leafBase,
        T[] leafVals,
        int[] tree,
        int k,
        bool[] hasValue)
    {
        _enums = enums;
        _cmp = cmp;
        _leafBase = leafBase;
        _leafVals = leafVals;
        _tree = tree;
        _k = k;
        _hasValue = hasValue;
    }

    /// <summary>
    /// Creates and initializes a new async tournament tree for merging k sorted sequences.
    /// </summary>
    /// <param name="k">The number of sequences to merge.</param>
    /// <param name="enums">The async enumerators for each sequence.</param>
    /// <param name="comparer">The comparer to determine sort order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An initialized tournament tree ready for merging.</returns>
    public static async ValueTask<AsyncTournamentTree<T>> CreateAsync(
        int k,
        List<IAsyncEnumerator<T>> enums,
        IComparer<T>? comparer = null,
        CancellationToken ct = default)
    {
        if (k <= 0)
            throw new ArgumentException("Number of sequences must be positive", nameof(k));
        if (enums.Count != k)
            throw new ArgumentException($"Expected {k} enumerators but got {enums.Count}", nameof(enums));
        
        /* smallest power of two ≥ k */
        int size = 1;
        while (size < k)
            size <<= 1;
        
        int leafBase = size;
        var leafVals = new T[k]; // Only allocate what we need
        var tree = new int[size * 2];
        var hasValue = new bool[k];
        var cmp = comparer ?? Comparer<T>.Default;

        int activeLeaves = 0;

        // Parallel initialization for better performance with many sequences
        if (k > 8)
        {
            var tasks = new Task<bool>[k];
            for (int i = 0; i < k; i++)
            {
                int idx = i; // Capture for closure
                tasks[i] = enums[idx].MoveNextAsync().AsTask();
            }
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            for (int i = 0; i < k; i++)
            {
                int leaf = leafBase + i;
                if (tasks[i].Result)
                {
                    leafVals[i] = enums[i].Current;
                    tree[leaf] = i;
                    hasValue[i] = true;
                    activeLeaves++;
                }
                else
                {
                    tree[leaf] = -1;
                    hasValue[i] = false;
                }
            }
        }
        else
        {
            // Sequential for small k
            for (int i = 0; i < k; i++)
            {
                int leaf = leafBase + i;
                if (await enums[i].MoveNextAsync().ConfigureAwait(false))
                {
                    leafVals[i] = enums[i].Current;
                    tree[leaf] = i;
                    hasValue[i] = true;
                    activeLeaves++;
                }
                else
                {
                    tree[leaf] = -1;
                    hasValue[i] = false;
                }
            }
        }

        // Initialize empty leaves
        for (int i = k; i < size; i++)
        {
            tree[leafBase + i] = -1;
        }

        /* build internal nodes bottom‑up */
        for (int n = leafBase - 1; n >= 1; --n)
        {
            int left = n << 1;
            int right = left + 1;
            tree[n] = Combine(tree[left], tree[right], leafVals, cmp);
        }

        var tt = new AsyncTournamentTree<T>(enums, cmp, leafBase, leafVals, tree, k, hasValue)
        {
            _activeLeaves = activeLeaves
        };
        
        return tt;
    }

    /// <summary>
    /// Asynchronously merges all sequences in sorted order.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of merged elements in sorted order.</returns>
    public async IAsyncEnumerable<T> MergeAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        // Fast path for single active sequence
        if (_activeLeaves == 1)
        {
            for (int i = 0; i < _k; i++)
            {
                if (_hasValue[i])
                {
                    yield return _leafVals[i];
                    while (await _enums[i].MoveNextAsync().ConfigureAwait(false))
                    {
                        ct.ThrowIfCancellationRequested();
                        yield return _enums[i].Current;
                    }
                    _activeLeaves = 0;
                    break;
                }
            }
            yield break;
        }

        // Buffer for batching yields (reduces async overhead)
        const int BatchSize = 64;
        var buffer = ArrayPool<T>.Shared.Rent(BatchSize);
        int bufferPos = 0;

        try
        {
            while (_activeLeaves > 0)
            {
                ct.ThrowIfCancellationRequested();
                
                int winner = _tree[1]; // root holds current global minimum
                
                if (winner == -1)
                    break; // No more elements
                
                // Batch results to reduce async overhead
                buffer[bufferPos++] = _leafVals[winner];
                
                if (bufferPos >= BatchSize)
                {
                    for (int i = 0; i < bufferPos; i++)
                        yield return buffer[i];
                    bufferPos = 0;
                }

                /* advance that enumerator */
                if (await _enums[winner].MoveNextAsync().ConfigureAwait(false))
                {
                    _leafVals[winner] = _enums[winner].Current;
                }
                else
                {
                    _tree[_leafBase + winner] = -1;
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
                                yield return _leafVals[i];
                                while (await _enums[i].MoveNextAsync().ConfigureAwait(false))
                                {
                                    ct.ThrowIfCancellationRequested();
                                    yield return _enums[i].Current;
                                }
                                _activeLeaves = 0;
                                break;
                            }
                        }
                        yield break;
                    }
                }

                /* Optimized bubble up - only update path to root */
                UpdatePath(_leafBase + winner);
            }
            
            // Yield remaining buffered items
            for (int i = 0; i < bufferPos; i++)
                yield return buffer[i];
        }
        finally
        {
            ArrayPool<T>.Shared.Return(buffer);
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePath(int leafIndex)
    {
        int node = leafIndex >> 1;
        while (node >= 1)
        {
            int left = node << 1;
            int right = left + 1;
            _tree[node] = Combine(_tree[left], _tree[right], _leafVals, _cmp);
            node >>= 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Combine(int ia, int ib, T[] leafVals, IComparer<T> cmp)
    {
        if (ia == -1) return ib;
        if (ib == -1) return ia;
        return cmp.Compare(leafVals[ia], leafVals[ib]) <= 0 ? ia : ib;
    }

    /// <summary>
    /// Disposes all async enumerators used by the tournament tree.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        // Parallel disposal for many enumerators
        if (_enums.Count > 8)
        {
            var tasks = new Task[_enums.Count];
            for (int i = 0; i < _enums.Count; i++)
            {
                var enumerator = _enums[i];
                if (enumerator != null)
                    tasks[i] = enumerator.DisposeAsync().AsTask();
                else
                    tasks[i] = Task.CompletedTask;
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        else
        {
            foreach (var e in _enums)
                if (e != null)
                    await e.DisposeAsync().ConfigureAwait(false);
        }
    }
}