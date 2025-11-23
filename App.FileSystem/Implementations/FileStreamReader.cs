namespace App.FileSystem.Implementations;

using System.Text;

/// <summary>
/// Provides a file-based stream reader implementation.
/// </summary>
public sealed class FileStreamReader : StreamReaderBase
{
    private readonly Stream _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStreamReader"/> class.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size.</param>
    public FileStreamReader(string path, Encoding encoding, int bufferSize)
        : base(
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                useAsync: true),
            encoding,
            bufferSize)
    {
        _stream = _reader.BaseStream;
    }

    /// <summary>
    /// Gets the current file stream.
    /// </summary>
    public override Stream CurrentStream => _stream;
}
