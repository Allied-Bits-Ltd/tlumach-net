using System.Globalization;
using System.Reflection;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 8 — reflection property lookup re-enumerates <c>GetProperties()</c> per placeholder.
/// <para>
/// <c>Utils.TryGetPropertyValue</c> calls <c>Type.GetProperties(...)</c> on every call, which allocates a
/// fresh <c>PropertyInfo[]</c>, then scans it linearly, then reads the value through reflection. The
/// <c>PropertyCount</c> parameter shows the linear component; the <c>Reference*</c> pair contrasts the
/// current shape with a per-type cached dictionary.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item08")]
public class Item08_PropertyBagBenchmarks
{
    private TranslationEntry _twoPlaceholders = null!;
    private CultureInfo _culture = null!;

    private object _smallBag = null!;
    private object _largeBag = null!;
    private Type _largeBagType = null!;
    private Dictionary<string, PropertyInfo> _cachedProperties = null!;

    /// <summary>Gets or sets which property bag is used: a small one or a wide one.</summary>
    [Params(2, 12)]
    public int PropertyCount { get; set; }

    private object CurrentBag => PropertyCount <= 2 ? _smallBag : _largeBag;

    private Type CurrentBagType => PropertyCount <= 2 ? _smallBag.GetType() : _largeBagType;

    [GlobalSetup]
    public void Setup()
    {
        _culture = CultureInfo.InvariantCulture;
        _twoPlaceholders = BenchmarkData.CreateTemplatedEntry("bag", "{name} has {count}");

        _smallBag = new SmallBag();
        _largeBag = new LargeBag();
        _largeBagType = typeof(LargeBag);

        _cachedProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo property in CurrentBagType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            _cachedProperties[property.Name] = property;
    }

    /// <summary>The trimming-safe generic overload — the tracked number.</summary>
    [Benchmark(Baseline = true, Description = "Library: ProcessTemplatedValueFrom<T>")]
    public string ProcessTemplatedValueFromGeneric()
        => PropertyCount <= 2
            ? _twoPlaceholders.ProcessTemplatedValueFrom(_culture, TextFormat.Arb, (SmallBag)_smallBag)
            : _twoPlaceholders.ProcessTemplatedValueFrom(_culture, TextFormat.Arb, (LargeBag)_largeBag);

    /// <summary>The reflection-on-runtime-type overload.</summary>
#pragma warning disable IL2026
    [Benchmark(Description = "Library: ProcessTemplatedValue(object)")]
    public string ProcessTemplatedValueObject()
        => _twoPlaceholders.ProcessTemplatedValue(_culture, TextFormat.Arb, CurrentBag);
#pragma warning restore IL2026

    /// <summary>A single property read through the current implementation.</summary>
    [Benchmark(Description = "Reference: Utils.TryGetPropertyValue (GetProperties scan)")]
    public object? ReferenceUtilsLookup()
    {
        Utils.TryGetPropertyValue(CurrentBagType, CurrentBag, "count", out object? value);
        return value;
    }

    /// <summary>The same read against a per-type cached dictionary — the shape the optimization targets.</summary>
    [Benchmark(Description = "Reference: cached PropertyInfo dictionary")]
    public object? ReferenceCachedLookup()
        => _cachedProperties.TryGetValue("count", out PropertyInfo? property)
            ? property.GetValue(CurrentBag)
            : null;

    private sealed class SmallBag
    {
        public string Name => "Bob";

        public int Count => 3;
    }

    private sealed class LargeBag
    {
        public string Name => "Bob";

        public int Count => 3;

        public string Alpha => "a";

        public string Bravo => "b";

        public string Charlie => "c";

        public string Delta => "d";

        public string Echo => "e";

        public string Foxtrot => "f";

        public string Golf => "g";

        public string Hotel => "h";

        public string India => "i";

        public string Juliet => "j";
    }
}
