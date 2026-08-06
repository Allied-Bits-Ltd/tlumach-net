using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 17 — <c>GetLeadingNonNegativeNumber</c> allocates a substring per call.
/// <para>
/// The method scans leading digits, then allocates <c>text.Substring(0, i)</c> and hands it to
/// <c>int.TryParse</c>, which re-scans the very characters the loop just validated. It is called once per
/// placeholder in .NET-format mode, from four separate resolver lambdas.
/// </para>
/// <para>
/// The reference implementation accumulates the value in the existing scan loop. It reproduces the
/// current contract exactly, including <c>-1</c> / <c>charsUsed = 0</c> on overflow.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item17")]
public class Item17_LeadingNumberBenchmarks
{
    // A representative mix: pure numbers, numbers with a format tail, non-numeric names, and an
    // overflowing digit run (which currently costs a substring plus a failed parse).
    private static readonly string[] Inputs =
    [
        "0",
        "12",
        "007",
        "12abc",
        "name",
        "",
        "99999999999",
    ];

    /// <summary>Runs the current implementation across the whole input set.</summary>
    [Benchmark(Baseline = true, Description = "Library: Utils.GetLeadingNonNegativeNumber")]
    public int LibraryImplementation()
    {
        int total = 0;
        for (int i = 0; i < Inputs.Length; i++)
        {
            total += Utils.GetLeadingNonNegativeNumber(Inputs[i], out int used);
            total += used;
        }

        return total;
    }

    /// <summary>Runs the allocation-free reference across the same set.</summary>
    [Benchmark(Description = "Reference: accumulate digits in the scan loop")]
    public int ReferenceImplementation()
    {
        int total = 0;
        for (int i = 0; i < Inputs.Length; i++)
        {
            total += ParseLeadingDigits(Inputs[i], out int used);
            total += used;
        }

        return total;
    }

    /// <summary>Runs a span-based parse, which avoids the substring but keeps <c>int.TryParse</c>.</summary>
    [Benchmark(Description = "Reference: AsSpan + int.TryParse")]
    public int ReferenceSpanImplementation()
    {
        int total = 0;
        for (int i = 0; i < Inputs.Length; i++)
        {
            total += ParseLeadingDigitsWithSpan(Inputs[i], out int used);
            total += used;
        }

        return total;
    }

    /// <summary>
    /// Reproduces the current contract without allocating: returns the leading non-negative number and the
    /// number of characters it occupied, or <c>-1</c> and <c>0</c> when there are no leading digits or the
    /// value does not fit in an <see cref="int"/>.
    /// </summary>
    /// <param name="text">The text to scan.</param>
    /// <param name="charsUsed">Receives the number of digit characters consumed.</param>
    /// <returns>The parsed value, or <c>-1</c>.</returns>
    private static int ParseLeadingDigits(string text, out int charsUsed)
    {
        long value = 0;
        int i = 0;

        while (i < text.Length && char.IsDigit(text[i]))
        {
            value = (value * 10) + (text[i] - '0');
            if (value > int.MaxValue)
            {
                charsUsed = 0;
                return -1;
            }

            i++;
        }

        if (i == 0)
        {
            charsUsed = 0;
            return -1;
        }

        charsUsed = i;
        return (int)value;
    }

    private static int ParseLeadingDigitsWithSpan(string text, out int charsUsed)
    {
        int i = 0;
        while (i < text.Length && char.IsDigit(text[i]))
            i++;

        if (int.TryParse(text.AsSpan(0, i), NumberStyles.Number, CultureInfo.InvariantCulture, out int result))
        {
            charsUsed = i;
            return result;
        }

        charsUsed = 0;
        return -1;
    }
}
