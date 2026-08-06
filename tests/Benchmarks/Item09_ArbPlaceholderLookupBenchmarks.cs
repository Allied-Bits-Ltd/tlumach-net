using System.Globalization;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 9 — Arb placeholder metadata lookup uses <c>FirstOrDefault</c> with a capturing lambda.
/// <para>
/// For every placeholder in a templated Arb entry, <c>GetPlaceholderValue</c> runs
/// <c>Placeholders.FirstOrDefault(p =&gt; p.Name.Equals(placeholderName, ...))</c>. The lambda captures
/// <c>placeholderName</c>, so a display class and a delegate are allocated per placeholder, and the scan
/// itself is linear in the declared placeholder count.
/// </para>
/// <para>
/// <see cref="DeclaredPlaceholders"/> varies the declared count so the linear component is visible. The
/// <c>Reference*</c> pair contrasts <c>FirstOrDefault</c> with an allocation-free indexed loop.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item09")]
public class Item09_ArbPlaceholderLookupBenchmarks
{
    private static readonly string[] PlaceholderNames = ["a", "b", "c", "d", "e", "f", "g", "h"];

    private TranslationEntry _entry = null!;
    private CultureInfo _culture = null!;
    private Dictionary<string, object?> _values = null!;
    private List<Placeholder> _declared = null!;
    private string _lastName = null!;

    /// <summary>Gets or sets how many placeholders the entry declares.</summary>
    [Params(1, 8)]
    public int DeclaredPlaceholders { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkData.EnsureParsersRegistered();
        _culture = CultureInfo.InvariantCulture;

        int count = DeclaredPlaceholders;
        string[] names = PlaceholderNames[..count];

        string text = string.Join(" ", names.Select(static n => "{" + n + "}"));
        string declarations = string.Join(",\n", names.Select(static n => $"        \"{n}\": {{ \"type\": \"String\" }}"));

        string arb =
            "{\n" +
            "    \"@@locale\": \"en\",\n" +
            $"    \"tpl\": \"{text}\",\n" +
            "    \"@tpl\": {\n" +
            "        \"placeholders\": {\n" +
            declarations + "\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        Translation translation = TranslationManager.LoadTranslation(arb, ".arb", _culture, TextFormat.Arb)
            ?? throw new InvalidOperationException("Benchmark setup failed: the Arb sample did not parse.");

        _entry = translation["tpl"];

        if (_entry.Placeholders is null || _entry.Placeholders.Count != count)
            throw new InvalidOperationException($"Benchmark setup failed: expected {count} declared placeholders, got {_entry.Placeholders?.Count.ToString(CultureInfo.InvariantCulture) ?? "none"}.");

        _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
            _values[name] = name.ToUpperInvariant();

        _declared = _entry.Placeholders;

        // The worst case for a linear scan: the placeholder declared last.
        _lastName = names[^1];
    }

    /// <summary>
    /// The tracked number: resolving every placeholder in the entry, each one paying for a metadata scan.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Library: Arb entry with declared placeholders")]
    public string ArbDeclaredPlaceholders()
        => _entry.ProcessTemplatedValue(_culture, TextFormat.Arb, _values);

    /// <summary>The lookup shape used today, isolated: LINQ with a capturing predicate.</summary>
    [Benchmark(Description = "Reference: FirstOrDefault with capturing lambda")]
    public Placeholder? ReferenceFirstOrDefault()
    {
        string name = _lastName;
        return _declared.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The lookup shape the optimization moves to: an indexed loop, no allocations.</summary>
    [Benchmark(Description = "Reference: indexed for-loop")]
    public Placeholder? ReferenceIndexedLoop()
    {
        string name = _lastName;
        for (int i = 0; i < _declared.Count; i++)
        {
            if (_declared[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return _declared[i];
        }

        return null;
    }
}
