# External Sorter - Large File Sorting Application

## Overview

**External Sorter** is a high-performance console application designed to generate and sort extremely large files that exceed available system memory (RAM). It implements the **External Merge Sort** algorithm with support for multiple merge strategies, including **Polyphase Merge Sort**.

The application is ideal for scenarios where:
- Files are too large to fit into memory
- Efficient disk-based sorting is required
- Data follows the format: `<Number>. <String>` (e.g., `42. Apple Banana Cherry`)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Commands](#commands)
   - [Generate Command](#1-generate-command)
   - [Sort Command](#2-sort-command)
4. [How It Works](#how-it-works)
5. [Algorithm Details](#algorithm-details)
6. [Project Structure](#project-structure)
7. [Configuration](#configuration)
8. [Performance Considerations](#performance-considerations)

---

## Prerequisites

- **.NET 10** or later must be installed on your machine
- Download from: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- Sufficient disk space for temporary files (approximately 2x the size of the input file)

---

## Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/bendreyea/ExternalSorter.git
   cd ExternalSorter
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the application:**
   ```bash
   dotnet build -c Release
   ```

4. **Navigate to the build output:**
   ```bash
   cd App.Console/bin/Release/net10.0/
   ```

---

## Commands

### 1. Generate Command

Generates a test file with random data. Each line follows the format: `<Number>. <String>`, where:
- **Number**: A randomly generated integer (Poisson distribution, λ=5)
- **String**: A sequence of random words (word count follows Poisson distribution, λ=3)

**Syntax:**
```bash
App.Console.exe generate -f <file_name> -s <size_in_mb> [-o <output_directory>]
```

**Parameters:**
- **-f, --file-name** (Required): Name of the file to generate (e.g., `data.txt`)
- **-s, --file-size** (Required): Target file size in megabytes (e.g., `1000` for 1GB)
- **-o, --output-dir** (Optional): Output directory path. Defaults to current directory if not specified.

**Examples:**
```bash
# Generate a 100MB file in the current directory
App.Console.exe generate -f data.txt -s 100

# Generate a 5GB file in a specific directory
App.Console.exe generate -f large_data.txt -s 5000 -o /Users/andrew/RiderProjects/
```

**Sample Output:**
```
5. Apple Banana
12. Cherry Date Elderberry Fig
3. Grape
8. Honeydew Jackfruit Kiwi Lemon
```

---

### 2. Sort Command

Sorts a file using external merge sort algorithm. The sorting order is:
1. **Primary**: String part (case-insensitive, then case-sensitive with lowercase first)
2. **Secondary**: Numeric part (ascending)

**Syntax:**
```bash
App.Console.exe sort -i <input_file_path> -o <output_file_path>
```

**Parameters:**
- **-i, --input** (Required): Full path to the input file to be sorted
- **-o, --output** (Required): Full path where the sorted output will be written

**Examples:**
```bash
# Sort a file
App.Console.exe sort -i /Users/andrew/RiderProjects/test1.txt -o /Users/andrew/RiderProjects/test1_sorted.txt

# Sort a large file (the algorithm handles files larger than available RAM)
App.Console.exe sort -i large_data.txt -o large_data_sorted.txt
```

---

## How It Works

The External Sorter uses a **two-phase approach** to sort files that are larger than available memory:

### Phase 1: Split and Sort

1. **File Splitting**: The input file is read in streaming fashion and divided into manageable chunks
   - Default chunk size: **100 MB** (configurable)
   - Each chunk is sized to fit comfortably in memory
   - Files are split at line boundaries to maintain data integrity

2. **In-Memory Sorting**: Each chunk is independently sorted in memory
   - Uses the custom `StringAndNumberComparer` for sorting logic
   - Sorted chunks are written to temporary files with `.sorted` extension
   - Original unsorted chunks are deleted after sorting

3. **Parallel Processing**: Multiple chunks can be sorted concurrently
   - Uses producer-consumer pattern with channels
   - **Two producer threads** sort chunks in parallel (configured for demonstration purposes)
   - One consumer thread manages the merge process
   - Note: Adding more threads provides diminishing returns due to I/O bottlenecks (see [Parallelization Limits](#parallelization-limits) below)

### Phase 2: Merge

1. **Batch Merging**: Sorted chunks are merged in batches
   - Default batch size: **100 files** per merge operation
   - Multiple merge passes may be required for very large files
   - Uses the configured merge strategy (Polyphase, K-Way, or Tournament Tree)

2. **Iterative Reduction**: The merge process continues until a single sorted file remains
   - Each merge pass reduces the number of files
   - Intermediate merged files become inputs for the next pass
   - Temporary files are deleted after each successful merge

3. **Final Output**: The last remaining file is moved to the specified output location

### Workflow Diagram

```text
┌─────────────────────────────────────────────────────────────┐
│                     INPUT FILE                              │
│                   (Large unsorted file)                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│               PHASE 1: SPLIT & SORT                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Chunk 1  │  │ Chunk 2  │  │ Chunk 3  │  │ Chunk N  │   │
│  │ (100 MB) │  │ (100 MB) │  │ (100 MB) │  │ (100 MB) │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │             │             │           │
│       ▼             ▼             ▼             ▼           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Sort    │  │  Sort    │  │  Sort    │  │  Sort    │   │
│  │ in RAM   │  │ in RAM   │  │ in RAM   │  │ in RAM   │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │             │             │           │
│       ▼             ▼             ▼             ▼           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ 1.sorted │  │ 2.sorted │  │ 3.sorted │  │ N.sorted │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│               PHASE 2: MERGE                                │
│                                                             │
│  Pass 1: Merge 100 files → Intermediate files              │
│  Pass 2: Merge remaining files → Fewer files               │
│  Pass N: Merge final files → 1 sorted file                 │
│                                                             │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                   OUTPUT FILE                               │
│               (Fully sorted file)                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Algorithm Details

### Sorting Algorithm: Polyphase Merge Sort

The core of the merging strategy is the **Polyphase Merge Sort**. Unlike a standard balanced merge, Polyphase merge maximizes the efficiency of available "tapes" (or file streams) by distributing the initial runs unevenly based on **Fibonacci numbers**.

**How it works:**
1.  **Fibonacci Distribution**: The initial sorted runs are distributed across $N-1$ input tapes (where $N$ is the total number of tapes available) according to consecutive Fibonacci numbers.
2.  **Multi-Phase Merging**: In each phase, runs are merged from the input tapes onto the empty output tape until one of the input tapes becomes empty.
3.  **Tape Rotation**: The empty input tape becomes the new output tape, and the previous output tape becomes an input tape. This rotation continues until a single sorted file remains.

#### Illustration (3-Tape Polyphase Merge)

The following diagram illustrates the state of 3 tapes during a Polyphase merge of 13 initial runs (distributed as 8 and 5).

```text
[Initial State]
Tape 1: [8 runs]
Tape 2: [5 runs]
Tape 3: [Empty]

       | Merge 5 runs from T1 & T2 -> T3
       v

[Phase 1]
Tape 1: [3 runs]  (8 - 5)
Tape 2: [Empty]   (5 - 5)
Tape 3: [5 runs]  (Merged)

       | Merge 3 runs from T1 & T3 -> T2
       v

[Phase 2]
Tape 1: [Empty]   (3 - 3)
Tape 2: [3 runs]  (Merged)
Tape 3: [2 runs]  (5 - 3)

       | Merge 2 runs from T2 & T3 -> T1
       v

[Phase 3]
Tape 1: [2 runs]  (Merged)
Tape 2: [1 run]   (3 - 2)
Tape 3: [Empty]   (2 - 2)

       | ... Continue until 1 sorted file remains
       v

[Final Result]
One fully sorted file on a single tape.
```

### Custom Comparator: StringAndNumberComparer

The sorting logic uses a specialized comparator that handles the `<Number>. <String>` format:

**Sorting Rules:**
1. **Primary Sort**: String part (text after the period and space)
   - Case-insensitive comparison first
   - If strings match case-insensitively, lowercase comes before uppercase
2. **Secondary Sort**: Numeric part (number before the period)
   - Ascending numerical order

---

## Configuration

The External Sorter behavior can be customized through the `ExternalSorterSettings` class:

| Setting | Default Value | Description |
|---------|---------------|-------------|
| **TempDirectory** | System temp directory | Location for temporary files during sorting |
| **BatchFileSize** | 100 MB (104,857,600 bytes) | Size of each chunk for splitting |
| **MergeBatch** | 100 files | Number of files to merge in a single operation |

### Modifying Settings

Settings can be adjusted in the dependency injection configuration (typically in `Program.cs`):

```csharp
services.Configure<ExternalSorterSettings>(options =>
{
    options.BatchFileSize = 50 * 1024 * 1024;  // 50 MB chunks
    options.MergeBatch = 50;                    // Merge 50 files at a time
    options.TempDirectory = "/custom/temp/path";
});
```

---

## Performance Considerations

### Memory Usage
- **Chunk Size**: Smaller chunks reduce memory usage but increase merge passes
- **Batch Size**: Larger merge batches reduce I/O but require more file handles

### Disk Space
- Temporary files require approximately **2x** the input file size
- Temporary files are deleted progressively as merging completes

### CPU Utilization
- Parallel sorting: 2 producer threads + 1 consumer thread
- Merging is I/O-bound; sorting is CPU-bound

### Parallelization Limits

The application uses **two producers for demonstration purposes** to show parallel chunk sorting. However, adding more threads does not linearly increase performance due to **Amdahl's Law**.

#### Amdahl's Law

[Amdahl's Law](https://en.wikipedia.org/wiki/Amdahl%27s_law) describes the theoretical speedup limit when parallelizing a program:

$$\text{Speedup} = \frac{1}{(1 - P) + \frac{P}{N}}$$

Where:
- $P$ = Proportion of the program that can be parallelized
- $N$ = Number of parallel processors/threads
- $(1 - P)$ = Serial portion of the program

#### Analysis for External Sorter

In our sorting pipeline:
- **Parallelizable portion (P)**: In-memory sorting of chunks ≈ 60-70%
- **Serial portions**: File I/O (splitting, writing), merge coordination ≈ 30-40%

Assuming $P = 0.65$ (65% parallelizable):

| Threads (N) | Theoretical Speedup | Efficiency |
|-------------|---------------------|------------|
| 1           | 1.00×               | 100%       |
| 2           | 1.54×               | 77%        |
| 4           | 2.29×               | 57%        |
| 8           | 3.25×               | 41%        |
| 16          | 4.21×               | 26%        |

**Key Observations:**
- Doubling threads from 1→2 provides 1.54× speedup (not 2×)
- With 8 threads, maximum speedup is only 3.25× (not 8×)
- Beyond 4 threads, efficiency drops significantly
- I/O bottlenecks (disk read/write) dominate at higher thread counts

**Practical Recommendation**: 2-4 producer threads provide the best balance between speedup and resource utilization for typical disk-based workloads.

### Optimal Settings
For best performance:
- **Small RAM systems** (< 8 GB): Use 50 MB chunks
- **Medium RAM systems** (8-16 GB): Use 100 MB chunks (default)
- **Large RAM systems** (> 16 GB): Use 200+ MB chunks

*Actual performance depends on hardware, disk speed, and data characteristics.*

---

## Benchmark Results

### Why Polyphase Merge?

While the primary motivation for implementing **Polyphase Merge Sort** was **curiosity** and exploring an elegant algorithm from the classical literature, the benchmarks confirm it was also the right performance choice.

### Merge Strategy Comparison

Three merge strategies were implemented and benchmarked:

1. **K-Way Merge**: Standard multi-way merge using a priority queue
2. **Tournament Tree Merge**: Uses a tournament tree for efficient minimum selection
3. **Polyphase Merge**: Fibonacci-based distribution with tape rotation

#### Benchmark Environment
- **Test**: Merging multiple sorted file chunks
- **Data**: Realistic file sizes from the splitting phase
- **.NET**: BenchmarkDotNet for accurate measurements

## Benchmark

| Method      | Mean    | Error    | StdDev   | Gen0        | Gen1        | Gen2        | Allocated |
|------------ |--------:|---------:|---------:|------------:|------------:|------------:|----------:|
| Sort1GBFile | 1.668 m | 0.0988 m | 0.0054 m | 765000.0000 | 325000.0000 | 114000.0000 |   5.94 GB |


| Method                   | Mean     | Error   | StdDev   | Gen0       | Gen1       | Gen2       | Allocated |
|------------------------- |---------:|--------:|---------:|-----------:|-----------:|-----------:|----------:|
| BenchmarkKWayFiles       | 405.4 ms | 7.96 ms | 13.51 ms | 13000.0000 |  5000.0000 |  3000.0000 |  77.96 MB |
| BenchmarkPolyPhaseFiles  | 113.5 ms | 2.06 ms |  3.03 ms | 16000.0000 | 15000.0000 | 11000.0000 |  74.14 MB |
| BenchmarkTournamentFiles | 376.2 ms | 2.45 ms |  2.17 ms | 13000.0000 |  5000.0000 |  3000.0000 |  77.91 MB |

### Analysis

**Performance Winner: Polyphase Merge**
- **3.57× faster** than K-Way Merge (113.5ms vs 405.4ms)
- **3.31× faster** than Tournament Tree (113.5ms vs 376.2ms)

**Why Polyphase Wins:**
1. **Better Cache Locality**: Sequential tape reading patterns
2. **Reduced Comparisons**: Fibonacci distribution minimizes total merge operations
3. **Efficient I/O Patterns**: Natural alignment with file system operations
4. **Less Overhead**: No heap/tree maintenance like K-Way and Tournament strategies

**Conclusion**: While curiosity drove the initial implementation, Polyphase Merge Sort proved to be not just intellectually elegant but also the most performant strategy for file-based external sorting. The 3.5× speedup over conventional approaches validates both the historical algorithm design and its relevance for modern systems.


---

## License

This project is available under the terms specified in the `LICENSE` file.



