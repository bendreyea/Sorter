namespace App.Generator;

using System.Text;
using App.FileSystem;
using FileSystem.Interfaces;

/// <summary>
/// Generates random data files.
/// </summary>
public class FileGenerator
{
    private readonly IFileSystem _fileSystem;
    private readonly IDistribution _numberDistribution;
    private readonly IDistribution _wordCountDistribution;
    private readonly string[] _words;
    private const int BatchSize = 10000; // Number of lines per batch
    private const int BufferCapacity = 81920;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileGenerator"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system to use.</param>
    public FileGenerator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _numberDistribution = new PoissonDistribution(lambda: 5.0);
        _wordCountDistribution = new PoissonDistribution(lambda: 3.0);
        _words = new[]
        {
            "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew",
            "Jackfruit", "Kiwi", "Lemon", "Mango", "Nectarine", "Orange",
            "Papaya", "Quince", "Raspberry", "Strawberry", "Tomato", "Ugli", "Vanilla",
            "Watermelon", "Xigua", "Yam", "Zucchini", "is", "the", "best", "yellow",
            "something", "very", "delicious", "and", "tasty", "sweet", "fresh"
        };
    }

    /// <summary>
    /// Generates a file with random content.
    /// </summary>
    public async Task GenerateFile(string filePath, long targetSizeInBytes, CancellationToken token)
    {
        using IRandom rand = new CryptoRandom();
        await using var writer = _fileSystem.FileWriter.CreateText(filePath);

        var buffer = new StringBuilder(BufferCapacity);
        long totalBytesWritten = 0;

        while (totalBytesWritten < targetSizeInBytes)
        {
            buffer.Clear();
            int linesInBatch = 0;

            // Accumulate lines in the buffer up to the batch size or target size
            while (linesInBatch < BatchSize && totalBytesWritten < targetSizeInBytes)
            {
                string line = GenerateRandomString(_numberDistribution, _wordCountDistribution, rand);
                buffer.AppendLine(line);
                totalBytesWritten += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
                linesInBatch++;
            }

            // Write the buffered lines to the file
            await writer.WriteAsync(buffer.ToString().AsMemory(), token).ConfigureAwait(false);
        }

        await writer.FlushAsync(token);
    }

    /// <summary>
    /// Generates a random string with a number and a sequence of words.
    /// </summary>
    private string GenerateRandomString(IDistribution numberDistribution, IDistribution wordCountDistribution, IRandom rand)
    {
        // Generate random number
        var number = numberDistribution.Generate(rand);

        // Generate random word count
        var wordCount = wordCountDistribution.Generate(rand);

        // Build the string using StringBuilder for efficiency
        var sb = new StringBuilder();
        sb.Append(number);
        sb.Append(". ");

        for (int i = 0; i < wordCount; i++)
        {
            if (i > 0)
                sb.Append(' ');
            var index = rand.Next(0, _words.Length);
            sb.Append(_words[index]);
        }

        return sb.ToString();
    }
}