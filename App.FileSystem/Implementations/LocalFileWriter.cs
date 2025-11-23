namespace App.FileSystem.Implementations;

using System.Text;
using App.FileSystem;
using Interfaces;

/// <summary>
/// Implements file writer operations.
/// </summary>
public class LocalFileWriter : IFileWriter
{
    private readonly int _bufferSize;
    private readonly Encoding _encoding;
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileWriter"/> class.
    /// </summary>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="encoding">The encoding to use.</param>
    public LocalFileWriter(int bufferSize, Encoding encoding)
    {
        _bufferSize = bufferSize;
        _encoding = encoding;
    }
    
    /// <summary>
    /// Creates a new file for writing.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream writer.</returns>
    public IStreamWriter CreateText(string path)
    {
        var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: _bufferSize, useAsync: false);
        return new StreamWriterAdapter(fileStream, _encoding);
    }


    /// <summary>
    /// Opens a file for appending text.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>A stream writer.</returns>
    public IStreamWriter AppendText(string path)
    {
        var fileStream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: _bufferSize, useAsync: false);
        return new StreamWriterAdapter(fileStream, _encoding);
    }
}