using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 20 — <c>config.Translations</c> forces uppercase allocations because it uses the
/// default comparer.
/// <para>
/// <c>TranslationConfiguration.Translations</c> is a plain <c>Dictionary&lt;string, string&gt;</c>, so
/// <c>InternalLoadTranslation</c> must probe it with <c>culture.Name.ToUpperInvariant()</c> and
/// <c>culture.TwoLetterISOLanguageName.ToUpperInvariant()</c>, allocating two strings per load attempt.
/// This is a load-path cost only; it does not touch the steady-state lookup.
/// </para>
/// <para>
/// The <c>Library*</c> benchmarks build a fresh manager per invocation so the load path is genuinely
/// cold. That means they also include file I/O and parsing, which dominate; the <c>Reference*</c> pair
/// is what isolates the probe cost itself.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item20")]
public class Item20_ConfigTranslationsBenchmarks
{
    private CultureInfo _exactMatchCulture = null!;
    private CultureInfo _languageOnlyMatchCulture = null!;

    private Dictionary<string, string> _ordinalMap = null!;
    private Dictionary<string, string> _ignoreCaseMap = null!;
    private string _probeCultureName = null!;
    private string _probeLanguageName = null!;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkData.EnsureParsersRegistered();
        _ = BenchmarkData.Directory;

        // "de-DE" is mapped explicitly; "de-CH" is not, so it resolves through the two-letter language key.
        _exactMatchCulture = new CultureInfo("de-DE");
        _languageOnlyMatchCulture = new CultureInfo("de-CH");

        _ordinalMap = new Dictionary<string, string>(StringComparer.Ordinal);
        _ignoreCaseMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string culture in new[] { "DE", "DE-DE", "DE-AT", "FR", "ES", "IT", "NL", "PL", "SK", "HR", "CS" })
        {
            _ordinalMap[culture] = "file_" + culture + ".arb";
            _ignoreCaseMap[culture] = "file_" + culture + ".arb";
        }

        CultureInfo probe = new("de-CH");
        _probeCultureName = probe.Name;
        _probeLanguageName = probe.TwoLetterISOLanguageName;
    }

    /// <summary>Cold load of a culture that is mapped explicitly in the configuration.</summary>
    [Benchmark(Description = "Library: cold load, exact locale key")]
    public int ColdLoadExactKey()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        return manager.GetValue(config, BenchmarkData.PlainKey, _exactMatchCulture, out _).Text?.Length ?? 0;
    }

    /// <summary>
    /// Cold load of a culture that is not mapped, so the probe falls through to the two-letter language
    /// key — two uppercase conversions instead of one.
    /// </summary>
    [Benchmark(Description = "Library: cold load, two-letter language fallback")]
    public int ColdLoadLanguageFallback()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        return manager.GetValue(config, BenchmarkData.PlainKey, _languageOnlyMatchCulture, out _).Text?.Length ?? 0;
    }

    /// <summary>The probe shape used today: uppercase both names, then probe an ordinal dictionary.</summary>
    [Benchmark(Baseline = true, Description = "Reference: ordinal map + two ToUpperInvariant probes")]
    public bool ReferenceOrdinalWithUpperCasing()
    {
        bool found = _ordinalMap.TryGetValue(_probeCultureName.ToUpperInvariant(), out _);
        if (!found)
            found = _ordinalMap.TryGetValue(_probeLanguageName.ToUpperInvariant(), out _);

        return found;
    }

    /// <summary>The probe shape the optimization moves to: a case-insensitive map, probed directly.</summary>
    [Benchmark(Description = "Reference: OrdinalIgnoreCase map, no conversion")]
    public bool ReferenceIgnoreCaseDirect()
    {
        bool found = _ignoreCaseMap.TryGetValue(_probeCultureName, out _);
        if (!found)
            found = _ignoreCaseMap.TryGetValue(_probeLanguageName, out _);

        return found;
    }
}
