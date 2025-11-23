namespace App.FileSystem.Implementations;

using System.Text;
using System.Buffers;
using App.FileSystem;
using Interfaces;

/// <summary>
/// Base class for stream writer wrappers.
/// </summary>
public abstract class StreamWriterBase : IStreamWriter
{
    /// <summary>
    /// The underlying stream writer.
    /// </summary>
    protected readonly StreamWriter _writer;
    /// <summary>
    /// The underlying stream.
    /// </summary>
    protected Stream _stream;
    /// <summary>
    /// Indicates whether the writer has been disposed.
    /// </summary>
    protected bool _disposed = false;
    
    /// <summary>
    /// The new line separator.
    /// </summary>
    protected readonly ReadOnlyMemory<char> NewLine;

    /// <summary>
    /// Gets the current stream.
    /// </summary>
    public abstract Stream CurrentStream { get; }

    
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamWriterBase"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="newLineSeparator">The new line separator.</param>
    protected StreamWriterBase(Stream stream, Encoding encoding, int bufferSize, string? newLineSeparator = null)
    {
        if (stream == null) 
            throw new ArgumentNullException(nameof(stream));
        
        _writer = new StreamWriter(stream, encoding, bufferSize, leaveOpen: true);
        _stream = stream;
        _writer.AutoFlush = true;
        NewLine = (newLineSeparator ?? Environment.NewLine).AsMemory();
        if (newLineSeparator != null)
        {
            _writer.NewLine = newLineSeparator;
        }
    }
    
    /// <summary>
    /// Asynchronously writes a line to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="bufferIndex">The current buffer index.</param>
    /// <param name="line">The line to write.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The new buffer index.</returns>
    public async ValueTask<int> WriteLineToBufferAsync(
        char[] buffer,
        int bufferIndex,
        ReadOnlyMemory<char> line,
        CancellationToken token)
    {
        int totalLength = line.Length + NewLine.Length;

        if (totalLength > buffer.Length)
        {
            await FlushBufferAsync(buffer, bufferIndex, token).ConfigureAwait(false);
            bufferIndex = 0;

            await _writer.WriteAsync(line, token).ConfigureAwait(false);
            await _writer.WriteAsync(NewLine, token).ConfigureAwait(false);

            return bufferIndex;
        }

        if (bufferIndex + totalLength > buffer.Length)
        {
            await FlushBufferAsync(buffer, bufferIndex, token).ConfigureAwait(false);
            bufferIndex = 0;
        }

        line.CopyTo(buffer.AsMemory(bufferIndex));
        bufferIndex += line.Length;

        NewLine.CopyTo(buffer.AsMemory(bufferIndex));
        bufferIndex += NewLine.Length;

        return bufferIndex;
    }

    /// <summary>
    /// Asynchronously flushes the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to flush.</param>
    /// <param name="bufferIndex">The current buffer index.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Zero after flushing.</returns>
    public async ValueTask<int> FlushBufferAsync(char[] buffer, int bufferIndex,
        CancellationToken token)
    {
        if (bufferIndex > 0)
        {
            await _writer.WriteAsync(buffer.AsMemory(0, bufferIndex), token).ConfigureAwait(false);
        }

        await _writer.FlushAsync(token);

        return 0;
    }
    
    /// <summary>
    /// Asynchronously writes a line to the stream.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task WriteLineAsync(string value)
    {
        EnsureNotDisposed();
        return _writer.WriteLineAsync(value);
    }

    /// <summary>
    /// Asynchronously writes characters to the stream.
    /// </summary>
    /// <param name="value">The characters to write.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the write operation.</returns>
    public Task WriteAsync(ReadOnlyMemory<char> value, CancellationToken token = default)
    {
        EnsureNotDisposed();
        return _writer.WriteAsync(value, token);
    }
    
    /// <summary>
    /// Writes a byte sequence to the stream.
    /// </summary>
    public async ValueTask WriteSequenceAsync(ReadOnlySequence<byte> sequence, CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        foreach (var segment in sequence)
        {
            await _stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously flushes the stream.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the flush operation.</returns>
    public Task FlushAsync(CancellationToken token = default)
    {
        EnsureNotDisposed();
        return _writer.FlushAsync(token);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StreamWriterBase));
    }

    /// <summary>
    /// Disposes the stream writer.
    /// </summary>
    public abstract void Dispose();
    /// <summary>
    /// Asynchronously disposes the stream writer.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public abstract ValueTask DisposeAsync();
}