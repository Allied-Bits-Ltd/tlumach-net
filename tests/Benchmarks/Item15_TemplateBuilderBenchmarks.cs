using System.Globalization;
using System.Text;

using BenchmarkDotNet.Attributes;

using Tlumach.Base;
using Tlumach.Benchmarks.Fixtures;

namespace Tlumach.Benchmarks;

/// <summary>
/// Optimization item 15 — a <see cref="StringBuilder"/> is allocated per template evaluation.
/// <para>
/// <c>InternalProcessTemplatedText</c> opens with <c>new StringBuilder(inputText.Length)</c>, and the ICU
/// rendering helpers allocate further builders of their own. Nested templates therefore allocate several
/// builders per single call.
/// </para>
/// <para>
/// <see cref="TextLength"/> scales the literal text around the placeholder so the builder's backing
/// array grows. The <c>Reference*</c> pair contrasts a fresh builder per call with a pooled one — note
/// that a naive pool is NOT correct for the real code, which is re-entrant through nested placeholders;
/// the reference is an upper bound on the achievable saving, not a drop-in design.
/// </para>
/// </summary>
[MemoryDiagnoser]
[BenchmarkCategory("Item15")]
public class Item15_TemplateBuilderBenchmarks
{
    private static readonly object?[] SingleValue = ["X"];

    private TranslationEntry _plainEntry = null!;
    private TranslationEntry _escapedEntry = null!;
    private CultureInfo _culture = null!;
    private string _sourceText = null!;

    [ThreadStatic]
    private static StringBuilder? _pooledBuilder;

    /// <summary>Gets or sets the length of the literal text surrounding the placeholder.</summary>
    [Params(32, 1024)]
    public int TextLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _culture = CultureInfo.InvariantCulture;

        string filler = new('w', Math.Max(1, TextLength / 2));
        _sourceText = filler + "{a}" + filler;

        _plainEntry = BenchmarkData.CreateTemplatedEntry("plain", _sourceText);

        // Escaped text forces the un-escaping pass to run alongside the placeholder scan.
        _escapedEntry = BenchmarkData.CreateEscapedTemplatedEntry("escaped", filler + "\\n{0}\\n" + filler);
    }

    /// <summary>A single template evaluation over text of the parameterised length — the tracked number.</summary>
    [Benchmark(Baseline = true, Description = "Library: Arb template evaluation")]
    public string ArbTemplate()
        => _plainEntry.ProcessTemplatedValue(_culture, TextFormat.Arb, SingleValue);

    /// <summary>The same, in .NET mode with escaped text, which also runs the un-escaping branch.</summary>
    [Benchmark(Description = "Library: .NET template evaluation with un-escaping")]
    public string DotNetEscapedTemplate()
        => _escapedEntry.ProcessTemplatedValue(_culture, TextFormat.DotNet, SingleValue);

    /// <summary>The allocation shape today: a new builder, sized to the input, per call.</summary>
    [Benchmark(Description = "Reference: new StringBuilder per call")]
    public int ReferenceNewBuilder()
    {
        StringBuilder builder = new(_sourceText.Length);
        builder.Append(_sourceText);
        return builder.ToString().Length;
    }

    /// <summary>
    /// The upper bound on the saving: reuse a thread-static builder. Not safe as-is for the real code,
    /// which re-enters the same method for nested placeholders.
    /// </summary>
    [Benchmark(Description = "Reference: pooled (thread-static) StringBuilder")]
    public int ReferencePooledBuilder()
    {
        StringBuilder builder = _pooledBuilder ??= new StringBuilder(256);
        builder.Clear();
        builder.Append(_sourceText);
        return builder.ToString().Length;
    }
}
