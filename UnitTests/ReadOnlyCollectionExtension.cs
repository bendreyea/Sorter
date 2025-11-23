namespace App.ExternalSorter.UnitTests;

public static class ReadOnlyCollectionExtension
{
    public static List<List<T>> SplitList<T>(this IReadOnlyCollection<T> source, int n)
    {
        var result = new List<List<T>>();
        var totalSize = source.Count;

        // Calculate the size of each chunk
        var chunkSize = totalSize / n;
        // Remainder to distribute across the first chunks
        var remainder = totalSize % n; 

        using var enumerator = source.GetEnumerator();

        for (var i = 0; i < n; i++)
        {
            // Calculate the size for the current chunk
            var currentChunkSize = chunkSize + (i < remainder ? 1 : 0);
            var chunk = new List<T>();

            for (int j = 0; j < currentChunkSize && enumerator.MoveNext(); j++)
            {
                chunk.Add(enumerator.Current);
            }

            result.Add(chunk);
        }

        return result;
    }
}