namespace App.ExternalSorter.Merge.Tournament;


/// <summary>
/// Implements tournament tree merge strategy for in-memory sequences.
/// </summary>
/// <typeparam name="T">The type of elements to merge.</typeparam>
public class TournamentMergeStrategy<T>
{
    /// <summary>
    /// Merges multiple sorted sequences using a tournament tree algorithm.
    /// </summary>
    /// <param name="sortedSequences">The sorted sequences to merge.</param>
    /// <param name="comparer">The comparer to determine sort order.</param>
    /// <returns>A single sorted sequence containing all elements.</returns>
    public IEnumerable<T> Merge(IEnumerable<IEnumerable<T>> sortedSequences, IComparer<T> comparer)
    {
        
        TournamentTree<T> tournamentTree = new TournamentTree<T>(sortedSequences, comparer);
        return tournamentTree.Merge();
    }
}