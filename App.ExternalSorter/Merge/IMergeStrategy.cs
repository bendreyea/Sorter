namespace App.ExternalSorter.Merge;

/// <summary>
/// Defines a strategy for merging sorted files.
/// </summary>
public interface IMergeStrategy
{
    /// <summary>
    /// Merges the specified sorted files into a single sorted file.
    /// </summary>
    /// <param name="files">The list of sorted files to merge.</param>
    /// <param name="comparer">The comparer to use for sorting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The path to the merged sorted file.</returns>
    Task<string> Merge(IEnumerable<string> files, IComparer<string> comparer, CancellationToken cancellationToken);
}
