using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 4 — a delegate is allocated on every <c>BaseTranslationUnit.GetValue()</c> call.
/// <para>
/// The placeholder-resolving lambda in <c>GetValue(CultureInfo)</c> captures <c>this</c>, so Roslyn
/// cannot cache it statically and allocates a fresh <c>Func&lt;string, int, object?&gt;</c> per call.
/// The <c>Reference*</c> pair isolates that allocation from everything else the call does.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item04")]
public class Item04_UnitGetValueBenchmarks
{
    private TranslationManager _manager = null!;
    private TranslationConfiguration _config = null!;
    private TranslationUnit _templatedUnit = null!;
    private TranslationUnit _plainUnit = null!;
    private TranslationUnit _cachingUnit = null!;
    private CultureInfo _culture = null!;
    private bool _originalCacheValues;

    private Func<string, int, object?>? _cachedResolver;

    [GlobalSetup]
    public void Setup()
    {
        _originalCacheValues = TranslationUnit.CacheValues;

        _manager = BenchmarkData.CreateManager();
        _config = _manager.DefaultConfiguration!;
        _culture = new CultureInfo(BenchmarkData.DefaultLocale);
        _manager.CurrentCulture = _culture;

        // CacheValues is global and defaults to true, which would make every GetValue a field read.
        // The per-call cost is what item 4 is about, so caching is disabled for the units below.
        TranslationUnit.CacheValues = false;

        _templatedUnit = new TranslationUnit(_manager, _config, BenchmarkData.ArbOnePlaceholderKey, containsPlaceholders: true);
        _templatedUnit.CachePlaceholderValue("name", "Alice");

        _plainUnit = new TranslationUnit(_manager, _config, BenchmarkData.PlainKey, containsPlaceholders: false);

        // Warm the underlying translations.
        _ = _templatedUnit.GetValue(_culture);
        _ = _plainUnit.GetValue(_culture);

        TranslationUnit.CacheValues = true;
        _cachingUnit = new TranslationUnit(_manager, _config, BenchmarkData.PlainKey, containsPlaceholders: false);
        _ = _cachingUnit.CurrentValue;
        TranslationUnit.CacheValues = false;

        _cachedResolver = ResolvePlaceholder;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _templatedUnit.Dispose();
        _plainUnit.Dispose();
        _cachingUnit.Dispose();
        _manager.Dispose();
        TranslationUnit.CacheValues = _originalCacheValues;
    }

    /// <summary>
    /// The tracked number: a templated unit resolving through the per-call lambda, with caching off.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Library: templated unit GetValue(), value cache off")]
    public string TemplatedGetValue() => _templatedUnit.GetValue(_culture);

    /// <summary>A non-templated unit, which skips the lambda entirely — the cost floor for comparison.</summary>
    [Benchmark(Description = "Library: plain unit GetValue(), value cache off")]
    public string PlainGetValue() => _plainUnit.GetValue(_culture);

    /// <summary>
    /// A unit with the string cache enabled. Should be a field read; included so a regression in the
    /// caching path is visible.
    /// </summary>
    [Benchmark(Description = "Library: plain unit CurrentValue, value cache on")]
    public string CachedCurrentValue()
    {
        TranslationUnit.CacheValues = true;
        try
        {
            return _cachingUnit.CurrentValue;
        }
        finally
        {
            TranslationUnit.CacheValues = false;
        }
    }

    /// <summary>The allocation shape today: a new capturing delegate per call.</summary>
    [Benchmark(Description = "Reference: allocate resolver delegate per call")]
    public object? ReferenceNewDelegatePerCall()
    {
        Func<string, int, object?> resolver = ResolvePlaceholder;
        return resolver("name", 0);
    }

    /// <summary>The allocation shape after the fix: one delegate held in a field.</summary>
    [Benchmark(Description = "Reference: reuse cached resolver delegate")]
    public object? ReferenceCachedDelegate() => _cachedResolver!("name", 0);

    private object? ResolvePlaceholder(string name, int index) => name.Length > index ? name : null;
}
