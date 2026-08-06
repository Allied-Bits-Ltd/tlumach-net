using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 3 — <c>lock(translation)</c> and <c>Monitor.Enter(this)</c> on the warm read path.
/// <para>
/// A successful culture-local hit currently takes two monitors; a fall-through to the default
/// translation takes four or five. All of them are shared by every caller of one manager, so warm
/// reads do not scale across threads. <c>ParallelReads</c> holds the total work constant while varying
/// the thread count: today the wall-clock should barely improve past one thread, and after the fix it
/// should fall roughly in proportion to <c>Threads</c>.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item03")]
public class Item03_WarmLookupContentionBenchmarks
{
    private const int TotalLookups = 4096;

    private TranslationManager _manager = null!;
    private TranslationConfiguration _config = null!;
    private CultureInfo _cultureWithOwnFile = null!;
    private CultureInfo _defaultCulture = null!;

    /// <summary>Gets or sets the number of threads that share the fixed lookup budget.</summary>
    [Params(1, 2, 4, 8)]
    public int Threads { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _manager = BenchmarkData.CreateManager();
        _config = _manager.DefaultConfiguration!;
        _cultureWithOwnFile = new CultureInfo("de-DE");
        _defaultCulture = new CultureInfo(BenchmarkData.DefaultLocale);

        // Fully warm both paths.
        _manager.GetValue(_config, BenchmarkData.PlainKey, _cultureWithOwnFile, out _);
        _manager.GetValue(_config, BenchmarkData.PlainKey, _defaultCulture, out _);
    }

    [GlobalCleanup]
    public void Cleanup() => _manager.Dispose();

    /// <summary>
    /// A fixed number of warm lookups spread over <see cref="Threads"/> threads. Scaling here is the
    /// measurement of interest, not the absolute number.
    /// </summary>
    [Benchmark(Description = "Warm reads: fixed work spread over N threads")]
    public int ParallelReads()
    {
        int perThread = TotalLookups / Threads;
        int total = 0;

        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, index =>
        {
            int local = 0;
            for (int i = 0; i < perThread; i++)
                local += _manager.GetValue(_config, BenchmarkData.PlainKey, _cultureWithOwnFile, out _).Text?.Length ?? 0;

            Interlocked.Add(ref total, local);
        });

        return total;
    }

    /// <summary>
    /// The same fixed work, but all lookups fall through to the default translation, which takes the
    /// additional <c>Monitor.Enter(this)</c> pairs.
    /// </summary>
    [Benchmark(Description = "Warm reads via default translation: fixed work over N threads")]
    public int ParallelReadsThroughDefaultTranslation()
    {
        int perThread = TotalLookups / Threads;
        int total = 0;

        Parallel.For(0, Threads, new ParallelOptions { MaxDegreeOfParallelism = Threads }, index =>
        {
            int local = 0;
            for (int i = 0; i < perThread; i++)
                local += _manager.GetValue(_config, BenchmarkData.PlainKey, _defaultCulture, out _).Text?.Length ?? 0;

            Interlocked.Add(ref total, local);
        });

        return total;
    }

    /// <summary>
    /// Single-threaded cost of one warm lookup, isolated from the parallel harness overhead. This is the
    /// number to watch for a regression on the desktop/single-threaded case.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Warm read: single lookup, single thread")]
    public string? SingleWarmLookup()
        => _manager.GetValue(_config, BenchmarkData.PlainKey, _cultureWithOwnFile, out _).Text;
}
