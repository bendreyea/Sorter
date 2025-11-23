namespace App.FileSystem.Implementations;

using System.IO.Pipelines;
using System.Text;
using App.FileSystem;
using Interfaces;

/// <summary>
/// Implements file reader operations.
/// </summary>
public class LocalFileReader : IFileReader
{
    private readonly int _bufferSize;
    private readonly Encoding _encoding;
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileReader"/> class.
    /// </summary>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="encoding">The encoding to use.</param>
    public LocalFileReader(int bufferSize, Encoding encoding)
    {
        _bufferSize = bufferSize;
        _encoding = encoding;
    }
    
    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream reader.</returns>
    public IStreamReader OpenText(string path)
    {
        return new FileStreamReader(path, _encoding, _bufferSize);
    }

    /// <summary>
    /// Opens a file as a PipeReader for efficient streaming.
    /// </summary>
    public PipeReader OpenAsPipeReader(string path)
    {
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _bufferSize, useAsync: true);
        return PipeReader.Create(fileStream);
    }
}