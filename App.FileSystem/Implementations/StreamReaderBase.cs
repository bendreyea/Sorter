namespace App.FileSystem.Implementations;

using System.Runtime.CompilerServices;
using System.Text;
using App.FileSystem;
using Interfaces;

/// <summary>
/// Base class for stream reader wrappers.
/// </summary>
public abstract class StreamReaderBase : IStreamReader
{
    private bool _disposed = false;
    /// <summary>
    /// The underlying stream reader.
    /// </summary>
    protected readonly StreamReader _reader;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamReaderBase"/> class.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size.</param>
    protected StreamReaderBase(Stream stream, Encoding encoding, int bufferSize = 1 * 1024 * 1024)
    {
        _reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize, leaveOpen: false);
    }

    /// <summary>
    /// Gets a value indicating whether the end of the stream has been reached.
    /// </summary>
    public bool EndOfStream => _reader.EndOfStream;

    /// <summary>
    /// Gets the current stream.
    /// </summary>
    public abstract Stream CurrentStream { get; }

    /// <summary>
    /// Disposes the stream reader.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Disposes the stream reader.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the stream reader.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public virtual async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _reader.Dispose(); // Synchronously dispose StreamReader
            await CurrentStream.DisposeAsync().ConfigureAwait(false); // Asynchronously dispose Stream
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public ValueTask<string?> ReadLineAsync(CancellationToken token = default)
    {
        return _reader.ReadLineAsync(token);
    }

    /// <inheritdoc />
    public ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken token = default)
    {
        return _reader.ReadAsync(buffer, token);
    }
}