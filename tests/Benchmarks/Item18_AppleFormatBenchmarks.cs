using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 18 — the Apple specifier path allocates a single-char string and uses exceptions for
/// type dispatch.
/// <para>
/// <c>InternalProcessTemplatedText</c> does <c>string specKey = spec.ToString()</c> for every Apple
/// placeholder, and wraps each numeric conversion in <c>try { Convert.ToXxx(...) } catch { ... }</c>. The
/// <c>MismatchedTypes</c> benchmark is the important one: a caller passing a type the specifier cannot
/// convert makes the <c>catch</c> fire per placeholder per call, and a thrown-and-caught exception costs
/// microseconds rather than nanoseconds.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item18")]
public class Item18_AppleFormatBenchmarks
{
    private static readonly object?[] MatchingValues = [42, 1234.5678, 255, "text"];
    private static readonly object?[] MismatchedValues = ["not-a-number", "not-a-number", "not-a-number", "text"];

    private TranslationEntry _numericSpecifiers = null!;
    private TranslationEntry _stringSpecifiers = null!;
    private CultureInfo _culture = null!;

    private static readonly string[] SpecKeys = BuildSpecKeyTable();

    [GlobalSetup]
    public void Setup()
    {
        _culture = CultureInfo.InvariantCulture;

        // %d, %f and %x all take the Convert-inside-try path; %@ does not.
        _numericSpecifiers = BenchmarkData.CreateTemplatedEntry("apple", "d=%d f=%f x=%x s=%@");

        // Only %@ specifiers: no Convert calls at all, so this isolates the specKey allocation.
        _stringSpecifiers = BenchmarkData.CreateTemplatedEntry("appleStr", "%@ %@ %@ %@");
    }

    /// <summary>Four Apple specifiers whose values convert cleanly — the tracked number.</summary>
    [Benchmark(Baseline = true, Description = "Library: Apple specifiers, matching types")]
    public string AppleMatchingTypes()
        => _numericSpecifiers.ProcessTemplatedValue(_culture, TextFormat.Apple, MatchingValues);

    /// <summary>
    /// The same template with values the numeric specifiers cannot convert, so three exceptions are
    /// thrown and caught per call.
    /// </summary>
    [Benchmark(Description = "Library: Apple specifiers, mismatched types (exception path)")]
    public string AppleMismatchedTypes()
        => _numericSpecifiers.ProcessTemplatedValue(_culture, TextFormat.Apple, MismatchedValues);

    /// <summary>Four <c>%@</c> specifiers, which skip the conversion try/catch entirely.</summary>
    [Benchmark(Description = "Library: Apple %@ specifiers only")]
    public string AppleStringSpecifiers()
        => _stringSpecifiers.ProcessTemplatedValue(_culture, TextFormat.Apple, MatchingValues);

    /// <summary>The allocation shape today: a fresh one-character string per specifier.</summary>
    [Benchmark(Description = "Reference: char.ToString() per specifier")]
    public int ReferenceCharToString()
    {
        int total = 0;
        foreach (char spec in "dfx@")
            total += spec.ToString().Length;

        return total;
    }

    /// <summary>The shape the optimization moves to: a preallocated lookup table of interned strings.</summary>
    [Benchmark(Description = "Reference: cached specifier-key table")]
    public int ReferenceCachedSpecKey()
    {
        int total = 0;
        foreach (char spec in "dfx@")
            total += SpecKeys[spec].Length;

        return total;
    }

    private static string[] BuildSpecKeyTable()
    {
        string[] table = new string[128];
        for (int i = 0; i < table.Length; i++)
            table[i] = ((char)i).ToString(CultureInfo.InvariantCulture);

        return table;
    }
}
