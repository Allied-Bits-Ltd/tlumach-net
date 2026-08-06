using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 1 — redundant <c>ToUpperInvariant()</c> allocations on every lookup.
/// <para>
/// <c>TranslationManager.GetValue</c> uppercases both the key and the culture name before probing
/// dictionaries that are already built with <see cref="StringComparer.OrdinalIgnoreCase"/>. The
/// <c>Library*</c> benchmarks track the end-to-end lookup cost; the <c>Reference*</c> pair isolates the
/// cost of the redundant conversion itself and stays valid across the redesign as a fixed yardstick.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item01")]
public class Item01_KeyNormalizationBenchmarks
{
    private TranslationManager _manager = null!;
    private TranslationConfiguration _config = null!;
    private CultureInfo _cultureWithOwnFile = null!;
    private CultureInfo _defaultCulture = null!;
    private Dictionary<string, TranslationEntry> _ignoreCaseDictionary = null!;

    /// <summary>Gets or sets the key length, which drives the cost of the uppercase conversion.</summary>
    [Params(8, 40)]
    public int KeyLength { get; set; }

    private string _key = null!;

    [GlobalSetup]
    public void Setup()
    {
        _manager = BenchmarkData.CreateManager();
        _config = _manager.DefaultConfiguration!;
        _cultureWithOwnFile = new CultureInfo("de-DE");
        _defaultCulture = new CultureInfo(BenchmarkData.DefaultLocale);

        // Warm both translations so the benchmarks measure the steady-state lookup, not file loading.
        _manager.GetValue(_config, BenchmarkData.PlainKey, _cultureWithOwnFile, out _);
        _manager.GetValue(_config, BenchmarkData.PlainKey, _defaultCulture, out _);

        // A standalone dictionary shaped exactly like Translation, for the reference pair.
        _key = BenchmarkData.PlainKey.PadRight(KeyLength, 'x');
        _ignoreCaseDictionary = new Dictionary<string, TranslationEntry>(StringComparer.OrdinalIgnoreCase)
        {
            [_key] = new TranslationEntry(_key, "value"),
        };

        for (int i = 0; i < 64; i++)
        {
            string filler = "filler" + i.ToString(CultureInfo.InvariantCulture);
            _ignoreCaseDictionary[filler] = new TranslationEntry(filler, "value");
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _manager.Dispose();

    /// <summary>End-to-end lookup that resolves from the culture-specific translation.</summary>
    [Benchmark(Description = "Library: GetValue, warm culture-local hit")]
    public string? LibraryWarmCultureHit()
        => _manager.GetValue(_config, BenchmarkData.PlainKey, _cultureWithOwnFile, out _).Text;

    /// <summary>End-to-end lookup for the default locale, which skips the culture branch entirely.</summary>
    [Benchmark(Description = "Library: GetValue, default-locale hit")]
    public string? LibraryDefaultLocaleHit()
        => _manager.GetValue(_config, BenchmarkData.PlainKey, _defaultCulture, out _).Text;

    /// <summary>The probe shape used today: uppercase the key, then probe a case-insensitive dictionary.</summary>
    [Benchmark(Description = "Reference: dictionary probe WITH ToUpperInvariant")]
    public TranslationEntry? ReferenceProbeWithToUpper()
    {
        _ignoreCaseDictionary.TryGetValue(_key.ToUpperInvariant(), out TranslationEntry? entry);
        return entry;
    }

    /// <summary>The probe shape the optimization moves to: probe the case-insensitive dictionary directly.</summary>
    [Benchmark(Description = "Reference: dictionary probe, no conversion")]
    public TranslationEntry? ReferenceProbeDirect()
    {
        _ignoreCaseDictionary.TryGetValue(_key, out TranslationEntry? entry);
        return entry;
    }
}
