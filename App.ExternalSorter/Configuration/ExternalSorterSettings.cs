namespace App.ExternalSorter.Configuration;

/// <summary>
/// Settings for configuring the external sorter.
/// </summary>
public class ExternalSorterSettings
{
    /// <summary>
    /// Gets or sets the batch file size in bytes (default: 128 MB for 3GB RAM target).
    /// Reduced from 512MB to lower memory pressure.
    /// </summary>
    public int BatchFileSize { get; set; } = 32 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the directory path for temporary files.
    /// </summary>
    public string TempDirectory { get; set; } = Path.GetTempPath();
    
    /// <summary>
    /// Gets or sets the number of files to merge in a single batch.
    /// </summary>
    public int MergeBatch { get; set; } = 64;
    
    /// <summary>
    /// Gets or sets the number of concurrent sort operations.
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;
    
    /// <summary>
    /// Gets the initial line capacity for array allocation.
    /// </summary>
    public int InitialLineCapacity => 2_000_000;
    
    /// <summary>
    /// Gets the pipeline segment size (256KB).
    /// </summary>
    public int PipelineSegmentSize => 256 * 1024;
    
    /// <summary>
    /// Gets the pipeline pause threshold (4MB).
    /// </summary>
    public int PipelinePauseThreshold => 4 * 1024 * 1024;
    
    /// <summary>
    /// Gets the pipeline resume threshold (2MB).
    /// </summary>
    public int PipelineResumeThreshold => 2 * 1024 * 1024;
    
    /// <summary>
    /// Gets the flush threshold for writing (256KB).
    /// </summary>
    public int FlushThreshold => 256 * 1024;
}