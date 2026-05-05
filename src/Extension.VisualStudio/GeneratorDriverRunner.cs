// <copyright file="GeneratorDriverRunner.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Tlumach.Generator;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Runs the Tlumach <see cref="Generator"/> through a <see cref="CSharpGeneratorDriver"/>,
/// producing generated sources via the Roslyn incremental pipeline.
/// </summary>
internal static class GeneratorDriverRunner
{
    /// <summary>
    /// Runs the Roslyn incremental generator against all supplied config files and returns
    /// the driver run result. Generated sources and diagnostics are available on the result.
    /// </summary>
    internal static GeneratorDriverRunResult Run(
        IEnumerable<string> configFilePaths,
        Dictionary<string, string> options)
    {
        var compilation = CSharpCompilation.Create("tlumach-gen-host");

        ImmutableArray<AdditionalText> additionalTexts = configFilePaths
            .Select(static p => (AdditionalText)new TlumachAdditionalText(p))
            .ToImmutableArray();

        var buildProperties = MapToBuildProperties(options);
        var optionsProvider = new BuildPropertyConfigOptionsProvider(buildProperties);

        var generator = new Generator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static IReadOnlyDictionary<string, string> MapToBuildProperties(
        Dictionary<string, string> options)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in options)
            result[MapKey(kv.Key)] = kv.Value;
        return result;
    }

    private static string MapKey(string key) => key switch
    {
        "projectdir"         => "build_property.projectdir",
        "UsingNamespace"     => "build_property.TlumachGeneratorUsingNamespace",
        "ExtraParsers"       => "build_property.TlumachGeneratorExtraParsers",
        "DelayedUnitCreation" => "build_property.TlumachGeneratorDelayedUnitCreation",
        _                    => key,
    };
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0048",
    Justification = "Supporting type co-located with GeneratorDriverRunner.")]
internal sealed class TlumachAdditionalText : AdditionalText
{
    private readonly string _path;

    internal TlumachAdditionalText(string path) => _path = path;

    public override string Path => _path;

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1031
        try
        {
            return SourceText.From(File.ReadAllText(_path, Encoding.UTF8), Encoding.UTF8);
        }
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0048",
    Justification = "Supporting type co-located with GeneratorDriverRunner.")]
internal sealed class BuildPropertyConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private static readonly AnalyzerConfigOptions s_empty =
        new BuildPropertyConfigOptions(new Dictionary<string, string>());

    private readonly AnalyzerConfigOptions _global;

    internal BuildPropertyConfigOptionsProvider(IReadOnlyDictionary<string, string> props)
        => _global = new BuildPropertyConfigOptions(props);

    public override AnalyzerConfigOptions GlobalOptions => _global;

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => s_empty;

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => s_empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0048",
    Justification = "Supporting type co-located with GeneratorDriverRunner.")]
internal sealed class BuildPropertyConfigOptions : AnalyzerConfigOptions
{
    private readonly IReadOnlyDictionary<string, string> _dict;

    internal BuildPropertyConfigOptions(IReadOnlyDictionary<string, string> dict)
        => _dict = dict;

    public override bool TryGetValue(string key, out string? value)
        => _dict.TryGetValue(key, out value!);
}
