namespace App.ExternalSorter.Merge.PolyPhase;

/// <summary>
/// Provides Fibonacci-related extension methods for polyphase merge.
/// </summary>
internal static class FibonacciExtension
{
    /// <summary>
    /// Splits a count into Fibonacci-based distribution for polyphase merge.
    /// </summary>
    /// <param name="n">The number of runs to distribute.</param>
    /// <returns>A tuple containing (tape1 count, tape2 count, dummy runs).</returns>
    public static (int, int, int) FibonacciSplit(this int n)
    {
        var fib = new List<int> { 1, 1 };
        while (fib[^1] < n)
            fib.Add(fib[^1] + fib[^2]);

        int fk = fib[^1]; // ≥ n
        int fk1 = fib[^2]; // F(k-1)
        int fk2 = fib.Count > 2 ? fib[^3] : 0;

        int b, dummy;
        var a = fk1;
        if (fk == n)
        {
            b = fk2;
            dummy = 0;
        }
        else
        {
            b = n - a;
            dummy = fk - n;
        }

        return (a, b, dummy);
    }
}