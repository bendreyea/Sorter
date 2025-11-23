using BenchmarkDotNet.Attributes;
using App.ExternalSorter.Core;
using App.ExternalSorter.Sorting;
using System;
using System.Linq;

namespace App.ExternalSorter.Benchmark
{
    [MemoryDiagnoser]
    public class InMemorySortBenchmark
    {
        // Increased sizes to satisfy BenchmarkDotNet's recommendation of >100ms iteration time
        [Params(200_000, 1_000_000, 2_000_000)]
        public int N;

        private string[] _data;
        private string[] _workArray;
        private readonly StringAndNumberComparer _comparer = new StringAndNumberComparer();

        [GlobalSetup]
        public void Setup()
        {
            _data = new string[N];
            var rng = new Random(42);
            var words = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew", "Ice Cream", "Jackfruit", "Kiwi", "Lemon", "Mango", "Nectarine", "Orange", "Papaya", "Quince", "Raspberry", "Strawberry", "Tangerine", "Ugli", "Vanilla", "Watermelon", "Xigua", "Yam", "Zucchini" };

            for (int i = 0; i < N; i++)
            {
                int number = rng.Next(1, 10000);
                string word = words[rng.Next(words.Length)];
                // Add some randomness to words to make them unique/varied
                if (rng.NextDouble() > 0.5) word += " " + words[rng.Next(words.Length)];
                
                _data[i] = $"{number}. {word}";
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _workArray = new string[N];
            Array.Copy(_data, _workArray, N);
        }

        [Benchmark(Baseline = true)]
        public void ArraySortWithComparer()
        {
            Array.Sort(_workArray, _comparer);
        }

        [Benchmark]
        public void ThreeWayRadixQuickSortBenchmark()
        {
            ThreeWayRadixQuickSort.Sort(_workArray);
        }
    }
}
