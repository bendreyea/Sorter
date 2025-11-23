namespace App.ExternalSorter.Configuration;

/// <summary>
/// Settings for configuring the external sorter.
/// </summary>
public class ExternalSorterSettings
{
    /// <summary>
    /// Gets or sets the directory path for temporary files. Defaults to system temp directory.
    /// </summary>
    public string TempDirectory { get; set; } = Path.GetTempPath();
    
    /// <summary>
    /// Gets or sets the batch file size in bytes. Defaults to 100 MB.
    /// </summary>
    public int BatchFileSize { get; set; } = 150 * 1024 * 1024; // 100 MB
    
    /// <summary>
    /// Gets or sets the number of files to merge in a single batch.
    /// </summary>
    public int MergeBatch { get; set; } = 200;
}