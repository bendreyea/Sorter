namespace App.FileSystem.InMemory;

using System.Text;
using App.FileSystem;
using App.FileSystem.Implementations;
using Interfaces;

/// <summary>
/// Provides an in-memory stream writer for writing text to memory.
/// </summary>
public class InMemoryStreamWriter :  StreamWriterBase, IStreamWriter
{
    private string _cachedContent = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStreamWriter"/> class.
    /// </summary>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="newLineSeparator">The new line separator.</param>
    public InMemoryStreamWriter(Encoding? encoding = null, int bufferSize = 16777216, string? newLineSeparator = null) 
        : base(CreateMemoryStream(bufferSize, out var stream), encoding ?? Encoding.UTF8, bufferSize, newLineSeparator)
    {
        _stream = stream;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryStreamWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="newLineSeparator">The new line separator.</param>
    public InMemoryStreamWriter(Stream stream, int bufferSize = 16777216, string? newLineSeparator = null) : base(stream,  Encoding.UTF8, bufferSize, newLineSeparator)
    {
        _stream = stream;
    }
    
    /// <summary>
    /// Gets the current memory stream.
    /// </summary>
    public override Stream CurrentStream => _stream;

    
    private static Stream CreateMemoryStream(int bufferSize, out MemoryStream stream)
    {
        stream = new MemoryStream(bufferSize);
        return stream;
    }

    /// <summary>
    /// Gets the content written to the stream as a string.
    /// </summary>
    public string Content
    {
        get
        {
            if (_disposed)
            {
                return _cachedContent;
            }
            else
            {
                _writer.Flush(); // Ensure all data is written to the MemoryStream
                _stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(_stream, Encoding.UTF8, false, 1024, true))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    /// <summary>
    /// Disposes the stream writer.
    /// </summary>
    public override void Dispose()
    {
        if (!_disposed)
        {
            _writer.Flush(); // Ensure all data is written to the MemoryStream
            _cachedContent = Content; // Cache the content
            _writer.Dispose();
            _stream.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronously disposes the stream writer.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            _cachedContent = Content; // Cache the content
            await _writer.DisposeAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }
}