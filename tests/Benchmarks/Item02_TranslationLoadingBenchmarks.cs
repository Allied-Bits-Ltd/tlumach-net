using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 2 — <c>lock(Translations)</c> is held across file I/O and parsing.
/// <para>
/// <c>TryGetEntryFromCulture</c> holds one process-wide monitor while <c>InternalLoadTranslation</c>
/// probes the disk and parses the file. Threads asking for <em>different</em> cultures therefore
/// serialize behind each other even though they share no state. The
/// <c>ColdLoad_ParallelDistinctCultures</c> benchmark is the one that exposes this: after the fix it
/// should scale with core count, while <c>ColdLoad_ParallelSameCulture</c> should stay roughly flat
/// (one thread loads, the rest wait for that single load either way).
/// </para>
/// <para>
/// Each benchmark builds a fresh manager so the load path is genuinely cold, without needing
/// per-iteration setup.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item02")]
public class Item02_TranslationLoadingBenchmarks
{
    private CultureInfo[] _distinctCultures = null!;
    private CultureInfo _singleCulture = null!;

    /// <summary>Gets or sets the number of threads that race on the cold load.</summary>
    [Params(1, 4, 8)]
    public int Threads { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkData.EnsureParsersRegistered();

        // Touch the data directory once so directory creation is not attributed to the first iteration.
        _ = BenchmarkData.Directory;

        _distinctCultures = Array.ConvertAll(BenchmarkData.ConcurrentCultureNames, static name => new CultureInfo(name));
        _singleCulture = new CultureInfo(BenchmarkData.ConcurrentCultureNames[0]);
    }

    /// <summary>One thread, one cold load. The floor that the parallel cases are compared against.</summary>
    [Benchmark(Baseline = true, Description = "Cold load: single thread, one culture")]
    public int ColdLoadSingleThread()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        return manager.GetValue(config, BenchmarkData.PlainKey, _singleCulture, out _).Text?.Length ?? 0;
    }

    /// <summary>N threads all asking for the same, not-yet-loaded culture.</summary>
    [Benchmark(Description = "Cold load: N threads, same culture")]
    public int ColdLoadParallelSameCulture()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        int total = 0;
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, index =>
        {
            int length = manager.GetValue(config, BenchmarkData.PlainKey, _singleCulture, out _).Text?.Length ?? 0;
            Interlocked.Add(ref total, length);
        });

        return total;
    }

    /// <summary>
    /// N threads each asking for a different, not-yet-loaded culture. These loads are independent, so
    /// this is the case that should parallelize once the lock no longer covers the load.
    /// </summary>
    [Benchmark(Description = "Cold load: N threads, distinct cultures")]
    public int ColdLoadParallelDistinctCultures()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        int total = 0;
        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, i =>
        {
            CultureInfo culture = _distinctCultures[i % _distinctCultures.Length];
            int length = manager.GetValue(config, BenchmarkData.PlainKey, culture, out _).Text?.Length ?? 0;
            Interlocked.Add(ref total, length);
        });

        return total;
    }

    /// <summary>
    /// A single thread loading every culture in sequence — the total parse-and-load work that the
    /// parallel case above spreads across threads.
    /// </summary>
    [Benchmark(Description = "Cold load: single thread, all cultures sequentially")]
    public int ColdLoadAllCulturesSequential()
    {
        using TranslationManager manager = BenchmarkData.CreateManager();
        TranslationConfiguration config = manager.DefaultConfiguration!;

        int total = 0;
        for (int i = 0; i < Threads; i++)
        {
            CultureInfo culture = _distinctCultures[i % _distinctCultures.Length];
            total += manager.GetValue(config, BenchmarkData.PlainKey, culture, out _).Text?.Length ?? 0;
        }

        return total;
    }
}
