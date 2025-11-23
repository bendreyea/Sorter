namespace App.FileSystem.Implementations;

using System.Text;

/// <summary>
/// Wraps a StreamWriter.
/// </summary>
public class StreamWriterAdapter : StreamWriterBase
{
    private new readonly Stream _stream;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamWriterAdapter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="newLineSeparator">The new line separator.</param>
    public StreamWriterAdapter(Stream stream, Encoding encoding, int bufferSize = 8912, string? newLineSeparator = null) : base(stream, encoding, bufferSize, newLineSeparator)
    {
        _stream = stream;
    }
    
    /// <summary>
    /// Gets the current stream.
    /// </summary>
    public override Stream CurrentStream => _stream;
    
    /// <summary>
    /// Disposes the stream writer.
    /// </summary>
    public override void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the stream writer.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes resources.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_writer != null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            await _writer.DisposeAsync().ConfigureAwait(false);
        }
        if (_stream != null)
        {
            await _stream.FlushAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _stream?.Flush();
                _stream?.Dispose();
            }

            _disposed = true;
        }
    }
}