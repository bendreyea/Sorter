namespace App.FileSystem.InMemory;

using System.Text;
using App.FileSystem.Implementations;

/// <summary>
/// Provides an in-memory stream reader for reading text from memory.
/// </summary>
public class InMemoryStreamReader : StreamReaderBase
{
    private readonly Stream _memoryStream;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStreamReader"/> class.
    /// </summary>
    /// <param name="lines">The lines to read from memory.</param>
    /// <param name="newLineSeparator">The new line separator.</param>
    public InMemoryStreamReader(IEnumerable<string> lines, string newLineSeparator)
        : base(CreateMemoryStream(out var memoryStream, lines, newLineSeparator), Encoding.UTF8)
    {
        _memoryStream = memoryStream;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStreamReader"/> class.
    /// </summary>
    /// <param name="memoryStream">The memory stream to read from.</param>
    public InMemoryStreamReader(Stream memoryStream)
        : base(memoryStream, Encoding.UTF8)
    {
        _memoryStream = memoryStream;
    }
    
    private static Stream CreateMemoryStream(out Stream memoryStream, IEnumerable<string> lines, string separator)
    {
        // Combine the lines with the specified separator
        string combinedLines = string.Join(separator, lines);

        // Convert the combined string to a byte array using UTF-8 encoding
        byte[] byteArray = Encoding.UTF8.GetBytes(combinedLines);

        // Initialize the MemoryStream with the byte array
        memoryStream = new MemoryStream(byteArray);

        return memoryStream;
    }
    
    /// <summary>
    /// Gets the current memory stream.
    /// </summary>
    public override Stream CurrentStream => _memoryStream;
    
}