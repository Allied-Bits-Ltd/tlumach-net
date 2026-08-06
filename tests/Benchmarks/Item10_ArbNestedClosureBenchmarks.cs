using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 10 — every Arb placeholder allocates a nested recursion closure.
/// <para>
/// <c>GetPlaceholderValue</c> constructs <c>internalGetPlaceholderValueFunc</c> unconditionally inside the
/// Arb branch, before it knows whether any of the <c>Utils.FormatArb*</c> calls will need it. The lambda
/// captures five values, so a display class and a delegate are allocated per placeholder, per call —
/// even for a plain <c>{name}</c> with no ICU tail.
/// </para>
/// <para>
/// <see cref="PlaceholderCount"/> scales the number of placeholders in one template. The allocation
/// column should scale linearly with it today, and become flat once the closure is hoisted to one
/// instance per template evaluation.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item10")]
public class Item10_ArbNestedClosureBenchmarks
{
    private static readonly string[] Names = ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"];

    private TranslationEntry _declaredEntry = null!;
    private TranslationEntry _icuPluralEntry = null!;
    private TranslationEntry _icuSelectEntry = null!;
    private CultureInfo _culture = null!;
    private Dictionary<string, object?> _values = null!;
    private Dictionary<string, object?> _pluralValues = null!;
    private Dictionary<string, object?> _selectValues = null!;

    /// <summary>Gets or sets how many declared placeholders the template carries.</summary>
    [Params(1, 4, 16)]
    public int PlaceholderCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkData.EnsureParsersRegistered();
        _culture = CultureInfo.InvariantCulture;

        string[] names = Names[..PlaceholderCount];
        string text = string.Join(" ", names.Select(static n => "{" + n + "}"));
        string declarations = string.Join(",\n", names.Select(static n => $"            \"{n}\": {{ \"type\": \"String\" }}"));

        string arb =
            "{\n" +
            "    \"@@locale\": \"en\",\n" +
            $"    \"tpl\": \"{text}\",\n" +
            "    \"@tpl\": {\n" +
            "        \"placeholders\": {\n" +
            declarations + "\n" +
            "        }\n" +
            "    },\n" +

            // The 'format' key matters: without it FormatArbNumber is never reached and the ICU tail is
            // silently ignored, so the ICU machinery would not run at all.
            "    \"plural\": \"{count, plural, =0{no items} one{one item} other{several items}}\",\n" +
            "    \"@plural\": {\n" +
            "        \"placeholders\": {\n" +
            "            \"count\": { \"type\": \"num\", \"format\": \"decimal\" }\n" +
            "        }\n" +
            "    },\n" +
            "    \"select\": \"{gender, select, male{He} female{She} other{They}}\",\n" +
            "    \"@select\": {\n" +
            "        \"placeholders\": {\n" +
            "            \"gender\": { \"type\": \"String\" }\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        Translation translation = TranslationManager.LoadTranslation(arb, ".arb", _culture, TextFormat.Arb)
            ?? throw new InvalidOperationException("Benchmark setup failed: the Arb sample did not parse.");

        _declaredEntry = translation["tpl"];
        _icuPluralEntry = translation["plural"];
        _icuSelectEntry = translation["select"];

        _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
            _values[name] = name.ToUpperInvariant();

        _pluralValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["count"] = 5 };
        _selectValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["gender"] = "female" };
    }

    /// <summary>
    /// N plain declared placeholders with no ICU tail. Every one of them allocates the recursion closure
    /// that is then never invoked — the tracked number.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Library: N declared placeholders, no ICU tail")]
    public string DeclaredPlaceholdersNoIcu()
        => _declaredEntry.ProcessTemplatedValue(_culture, TextFormat.Arb, _values);

    /// <summary>An ICU plural expression, where the closure genuinely is used.</summary>
    [Benchmark(Description = "Library: ICU plural expression")]
    public string IcuPlural()
        => _icuPluralEntry.ProcessTemplatedValue(_culture, TextFormat.Arb, _pluralValues);

    /// <summary>An ICU select expression, the other branch that consumes the closure.</summary>
    [Benchmark(Description = "Library: ICU select expression")]
    public string IcuSelect()
        => _icuSelectEntry.ProcessTemplatedValue(_culture, TextFormat.Arb, _selectValues);
}
