namespace App.FileSystem.Interfaces;

/// <summary>
/// Provides methods for writing files.
/// </summary>
public interface IStreamWriter : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Writes a character to the stream.
    /// </summary>
    Task WriteLineAsync(string value);

    /// <summary>
    /// Writes a line to a buffer asynchronously.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="bufferIndex">The current buffer index.</param>
    /// <param name="line">The line to write.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The new buffer index.</returns>
    ValueTask<int> WriteLineToBufferAsync(
        char[] buffer,
        int bufferIndex,
        ReadOnlyMemory<char> line,
        CancellationToken token);
    
    /// <summary>
    /// Flushes the buffer asynchronously.
    /// </summary>
    /// <param name="buffer">The buffer to flush.</param>
    /// <param name="bufferIndex">The current buffer index.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The new buffer index.</returns>
    ValueTask<int> FlushBufferAsync(char[] buffer, int bufferIndex,
        CancellationToken token);

    /// <summary>
    /// Writes a character to the stream.
    /// </summary>
    Task WriteAsync(ReadOnlyMemory<char> value, CancellationToken token = default);

    /// <summary>
    /// Writes a byte sequence to the stream.
    /// </summary>
    ValueTask WriteSequenceAsync(System.Buffers.ReadOnlySequence<byte> sequence, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Writes a character to the stream.
    /// </summary>
    Task FlushAsync(CancellationToken token = default);
    
    /// <summary>
    /// Gets the current stream.
    /// </summary>
    Stream CurrentStream { get; }
}