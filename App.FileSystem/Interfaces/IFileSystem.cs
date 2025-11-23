namespace App.FileSystem.Interfaces;

using System.Text;

/// <summary>
/// Provides methods for working with the file system.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Gets the file reader.
    /// </summary>
    IFileReader FileReader { get; }

    /// <summary>
    /// Gets the file writer.
    /// </summary>
    IFileWriter FileWriter { get; }

    /// <summary>
    /// Gets the encoding used for file operations.
    /// </summary>
    Encoding Encoding { get; }

    /// <summary>
    /// Gets a temporary file name.
    /// </summary>
    string GetTempFileName();

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Asynchronously deletes the specified file.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteFileAsync(string path, CancellationToken token);
    
    /// <summary>
    /// Moves the specified file to a new location.
    /// </summary>
    void MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
}