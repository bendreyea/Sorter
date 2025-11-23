namespace App.FileSystem.Interfaces;

using System.IO.Pipelines;

/// <summary>
/// Provides methods for reading files.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    IStreamReader OpenText(string path);

    /// <summary>
    /// Opens a file as a PipeReader for efficient streaming.
    /// </summary>
    PipeReader OpenAsPipeReader(string path);
}