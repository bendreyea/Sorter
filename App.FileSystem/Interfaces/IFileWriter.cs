namespace App.FileSystem.Interfaces;

/// <summary>
/// Provides methods for writing files.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Creates a new file for writing.
    /// </summary>
    IStreamWriter CreateText(string path);
    
    /// <summary>
    /// Creates a new file for writing, or appends to an existing file if it already exists.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IStreamWriter AppendText(string path);
}