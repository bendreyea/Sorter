namespace App.FileSystem.Implementations;

using System.Text;
using App.FileSystem;
using Interfaces;

/// <summary>
/// Provides file system operations.
/// </summary>
public class LocalFileSystem : IFileSystem
{
    private readonly Encoding _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileSystem"/> class.
    /// </summary>
    /// <param name="encoding">The encoding to use for file operations.</param>
    public LocalFileSystem(Encoding encoding)
    {
        _encoding = encoding;
    }

    private const int BufferSize = 80912;
    /// <summary>
    /// Gets the file reader.
    /// </summary>
    public IFileReader FileReader => new LocalFileReader(BufferSize, _encoding);
    /// <summary>
    /// Gets the file writer.
    /// </summary>
    public IFileWriter FileWriter => new LocalFileWriter(BufferSize, _encoding);
    /// <summary>
    /// Gets the encoding used for file operations.
    /// </summary>
    public Encoding Encoding => _encoding;

    /// <summary>
    /// Gets a temporary file name.
    /// </summary>
    /// <returns>A temporary file name.</returns>
    public string GetTempFileName()
    {
        return Path.GetTempFileName();
    }

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    public void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    
    /// <summary>
    /// Asynchronously deletes the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the deletion operation.</returns>
    public Task DeleteFileAsync(string filePath, CancellationToken token)
    {
        return Task.Run(() => DeleteFile(filePath), token);
    }

    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    /// <param name="sourceFileName">The source file name.</param>
    /// <param name="destFileName">The destination file name.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
        File.Move(sourceFileName, destFileName, overwrite);
    }
}