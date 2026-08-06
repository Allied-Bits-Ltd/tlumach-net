using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 7 — <c>.NET</c> format specifiers build a composite format string per placeholder.
/// <para>
/// <c>GetPlaceholderValue</c> does <c>string.Format(culture, "{0:" + tail + '}', value)</c>, allocating
/// a concatenated format string and re-parsing it on every placeholder evaluation.
/// </para>
/// <para>
/// NOTE: this path is also functionally broken today. <c>tail</c> retains the leading colon, so the
/// composite format becomes <c>"{0::N2}"</c> and the value is dropped in favour of the literal
/// <c>":N2"</c>. The <c>WithSpecifier</c> benchmark therefore measures a code path whose output is
/// wrong; keep that in mind when reading a post-fix comparison, because the corrected path will do
/// strictly more useful work than the current one. See the matching characterization test.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item07")]
public class Item07_DotNetFormatSpecifierBenchmarks
{
    private static readonly object?[] NumericValue = [1234.5678];

    private TranslationEntry _noSpecifier = null!;
    private TranslationEntry _withSpecifier = null!;
    private TranslationEntry _threeWithSpecifiers = null!;
    private CultureInfo _culture = null!;
    private string _tail = null!;
    private string _cachedCompositeFormat = null!;

    [GlobalSetup]
    public void Setup()
    {
        _culture = CultureInfo.InvariantCulture;
        _noSpecifier = BenchmarkData.CreateTemplatedEntry("plain", "value: {0}");
        _withSpecifier = BenchmarkData.CreateTemplatedEntry("spec", "value: {0:N2}");
        _threeWithSpecifiers = BenchmarkData.CreateTemplatedEntry("spec3", "{0:N2} {0:N3} {0:N4}");
        _tail = ":N2";
        _cachedCompositeFormat = "{0:" + _tail + "}";
    }

    /// <summary>A .NET-mode placeholder with no format specifier — the cheap path.</summary>
    [Benchmark(Baseline = true, Description = "Library: .NET placeholder, no specifier")]
    public string DotNetNoSpecifier()
        => _noSpecifier.ProcessTemplatedValue(_culture, TextFormat.DotNet, NumericValue);

    /// <summary>A .NET-mode placeholder carrying a format specifier — the tracked number.</summary>
    [Benchmark(Description = "Library: .NET placeholder with specifier")]
    public string DotNetWithSpecifier()
        => _withSpecifier.ProcessTemplatedValue(_culture, TextFormat.DotNet, NumericValue);

    /// <summary>Three specifiers in one template, so per-placeholder cost dominates.</summary>
    [Benchmark(Description = "Library: .NET, three placeholders with specifiers")]
    public string DotNetThreeSpecifiers()
        => _threeWithSpecifiers.ProcessTemplatedValue(_culture, TextFormat.DotNet, NumericValue);

    /// <summary>The allocation shape today: concatenate the composite format, then parse it.</summary>
    [Benchmark(Description = "Reference: concat composite format + string.Format")]
    public string ReferenceConcatCompositeFormat()
        => string.Format(_culture, "{0:" + _tail + "}", NumericValue[0]);

    /// <summary>Same output, but the composite format string is built once and reused.</summary>
    [Benchmark(Description = "Reference: cached composite format + string.Format")]
    public string ReferenceCachedCompositeFormat()
        => string.Format(_culture, _cachedCompositeFormat, NumericValue[0]);

    /// <summary>
    /// The cheapest shape: hand the specifier straight to <see cref="IFormattable"/>. Does not support
    /// alignment, which is why the cached-composite variant above is the safer target.
    /// </summary>
    [Benchmark(Description = "Reference: IFormattable.ToString(specifier, culture)")]
    public string ReferenceFormattableWithSpecifier()
        => NumericValue[0] is IFormattable formattable
            ? formattable.ToString("N2", _culture)
            : NumericValue[0]?.ToString() ?? string.Empty;
}
