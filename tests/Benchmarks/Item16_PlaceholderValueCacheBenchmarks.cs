using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 16 — the placeholder value cache uses a linguistic comparer.
/// <para>
/// <c>BaseTranslationUnit.CachePlaceholderValue</c> creates the cache with
/// <see cref="StringComparer.InvariantCulture"/>, a collation-based comparer whose
/// <c>GetHashCode</c> and <c>Equals</c> route through ICU. The cache is probed once per placeholder per
/// <c>GetValue()</c> call.
/// </para>
/// <para>
/// The observed behaviour is case-SENSITIVE (a value cached as <c>"NAME"</c> does not satisfy a
/// <c>{name}</c> placeholder), so <see cref="StringComparer.Ordinal"/> is the behaviour-preserving
/// replacement. <see cref="StringComparer.OrdinalIgnoreCase"/> is included in the reference set to show
/// its cost, but adopting it would be a behaviour change.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item16")]
public class Item16_PlaceholderValueCacheBenchmarks
{
    private static readonly string[] Names = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel"];

    private TranslationManager _manager = null!;
    private TranslationConfiguration _config = null!;
    private TranslationUnit _unit = null!;
    private CultureInfo _culture = null!;
    private bool _originalCacheValues;

    private Dictionary<string, object?> _invariantCultureCache = null!;
    private Dictionary<string, object?> _ordinalCache = null!;
    private Dictionary<string, object?> _ordinalIgnoreCaseCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _originalCacheValues = TranslationUnit.CacheValues;
        TranslationUnit.CacheValues = false;

        _manager = BenchmarkData.CreateManager();
        _config = _manager.DefaultConfiguration!;
        _culture = new CultureInfo(BenchmarkData.DefaultLocale);
        _manager.CurrentCulture = _culture;

        _unit = new TranslationUnit(_manager, _config, BenchmarkData.ArbEightPlaceholderKey, containsPlaceholders: true);
        for (int i = 0; i < 8; i++)
            _unit.CachePlaceholderValue(((char)('a' + i)).ToString(CultureInfo.InvariantCulture), i);

        _ = _unit.GetValue(_culture);

        _invariantCultureCache = BuildCache(StringComparer.InvariantCulture);
        _ordinalCache = BuildCache(StringComparer.Ordinal);
        _ordinalIgnoreCaseCache = BuildCache(StringComparer.OrdinalIgnoreCase);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _unit.Dispose();
        _manager.Dispose();
        TranslationUnit.CacheValues = _originalCacheValues;
    }

    /// <summary>
    /// The tracked number: a unit with eight placeholders, all resolved from the cache, so the comparer is
    /// exercised eight times per call.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Library: unit GetValue() resolving 8 cached placeholders")]
    public string UnitGetValueFromCache() => _unit.GetValue(_culture);

    /// <summary>Eight probes against the comparer used today.</summary>
    [Benchmark(Description = "Reference: 8 probes, StringComparer.InvariantCulture")]
    public int ReferenceInvariantCulture() => ProbeAll(_invariantCultureCache);

    /// <summary>Eight probes against the behaviour-preserving replacement.</summary>
    [Benchmark(Description = "Reference: 8 probes, StringComparer.Ordinal")]
    public int ReferenceOrdinal() => ProbeAll(_ordinalCache);

    /// <summary>Eight probes against the case-insensitive variant, which would change behaviour.</summary>
    [Benchmark(Description = "Reference: 8 probes, StringComparer.OrdinalIgnoreCase")]
    public int ReferenceOrdinalIgnoreCase() => ProbeAll(_ordinalIgnoreCaseCache);

    private static Dictionary<string, object?> BuildCache(StringComparer comparer)
    {
        Dictionary<string, object?> cache = new(comparer);
        for (int i = 0; i < Names.Length; i++)
            cache[Names[i]] = i;

        return cache;
    }

    private static int ProbeAll(Dictionary<string, object?> cache)
    {
        int found = 0;
        for (int i = 0; i < Names.Length; i++)
        {
            if (cache.TryGetValue(Names[i], out object? value) && value is not null)
                found++;
        }

        return found;
    }
}
