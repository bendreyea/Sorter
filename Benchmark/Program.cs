
using App.ExternalSorter.Benchmark;
using BenchmarkDotNet.Running;

// Run the in-memory sort benchmark
var summary = BenchmarkRunner.Run<InMemorySortBenchmark>();

// Run the Sorter benchmark with a 1GB file
// This will test the complete external sorting pipeline
// var summary = BenchmarkRunner.Run<SorterBenchmark>();

// Alternative benchmarks:
// BenchmarkRunner.Run<FileMergeBenchmark>();  // Tests different merge strategies
// BenchmarkRunner.Run<MergeBenchmark>();       // Tests merge operations
