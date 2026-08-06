using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 6 — <c>string.Format(culture, "{0}", value)</c> used as a to-string helper.
/// <para>
/// An undeclared Arb placeholder resolves through <c>Utils.FormatArbUnknownPlaceholder</c>, which ends
/// in <c>string.Format(culture, "{0}", value)</c>. That parses a composite format string at runtime for
/// what is an identity conversion. The <c>Reference*</c> trio contrasts the three shapes over the same
/// mixed set of values.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item06")]
public class Item06_ValueToTextBenchmarks
{
    private static readonly object?[] MixedValues =
    [
        "some text",
        42,
        1234.5678,
        new DateTime(2024, 3, 7, 13, 45, 0, DateTimeKind.Unspecified),
    ];

    private TranslationEntry _fourPlaceholders = null!;
    private TranslationEntry _onePlaceholder = null!;
    private CultureInfo _invariant = null!;
    private CultureInfo _german = null!;

    [GlobalSetup]
    public void Setup()
    {
        _invariant = CultureInfo.InvariantCulture;
        _german = new CultureInfo("de-DE");
        _fourPlaceholders = BenchmarkData.CreateTemplatedEntry("many", "{a} {b} {c} {d}");
        _onePlaceholder = BenchmarkData.CreateTemplatedEntry("one", "value: {a}");
    }

    /// <summary>Four undeclared Arb placeholders of mixed types — the tracked number.</summary>
    [Benchmark(Baseline = true, Description = "Library: Arb, four undeclared placeholders")]
    public string ArbFourPlaceholders()
        => _fourPlaceholders.ProcessTemplatedValue(_invariant, TextFormat.Arb, MixedValues);

    /// <summary>The same, for a non-invariant culture, so culture-sensitive formatting is exercised.</summary>
    [Benchmark(Description = "Library: Arb, four undeclared placeholders, de-DE")]
    public string ArbFourPlaceholdersGerman()
        => _fourPlaceholders.ProcessTemplatedValue(_german, TextFormat.Arb, MixedValues);

    /// <summary>A single placeholder, to separate per-placeholder cost from fixed per-call cost.</summary>
    [Benchmark(Description = "Library: Arb, one undeclared placeholder")]
    public string ArbOnePlaceholder()
        => _onePlaceholder.ProcessTemplatedValue(_invariant, TextFormat.Arb, MixedValues);

    /// <summary>The conversion shape used today, over the whole mixed set.</summary>
    [Benchmark(Description = "Reference: string.Format(culture, \"{0}\", value)")]
    public int ReferenceStringFormatIdentity()
    {
        int total = 0;
        foreach (object? value in MixedValues)
            total += string.Format(_invariant, "{0}", value).Length;

        return total;
    }

    /// <summary>The conversion shape the optimization moves to.</summary>
    [Benchmark(Description = "Reference: IFormattable.ToString(null, culture)")]
    public int ReferenceFormattableToString()
    {
        int total = 0;
        foreach (object? value in MixedValues)
            total += ToText(value, _invariant).Length;

        return total;
    }

    private static string ToText(object? value, CultureInfo culture)
        => value is IFormattable formattable
            ? formattable.ToString(null, culture)
            : value?.ToString() ?? string.Empty;
}
