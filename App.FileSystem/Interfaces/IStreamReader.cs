namespace App.FileSystem.Interfaces;

/// <summary>
/// Provides methods for reading files.
/// </summary>
public interface IStreamReader : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the current stream position is at the end of the stream.
    /// </summary>
    bool EndOfStream { get; }

    /// <summary>
    /// Reads a line of characters asynchronously from the current stream and returns the data as a string.
    /// </summary>
    ValueTask<string?> ReadLineAsync(CancellationToken token = default);

    /// <summary>
    /// Reads a block of characters asynchronously from the current stream.
    /// </summary>
    ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken token = default);

    /// <summary>
    /// Gets the current stream.
    /// </summary>
    Stream CurrentStream { get; }
}