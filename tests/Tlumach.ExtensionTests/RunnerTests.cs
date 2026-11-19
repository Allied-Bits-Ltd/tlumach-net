// <copyright file="RunnerTests.cs" company="Allied Bits Ltd.">
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

using System.Text.Json;

using Tlumach.Validation;
using AlliedBits.Tlumach.Extension.Runner;

using Tlumach.TestUtilities;

namespace Tlumach.ExtensionTests;

public class RunnerTests
{
    private const string DefaultJson = "{ \"Hello\": \"Hello\" }";

    // -------------------------------------------------------------------------
    // Command line
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData]
    [InlineData("--help")]
    [InlineData("check", "--project", "a.csproj")]
    [InlineData("verify")]
    [InlineData("verify", "--project")]
    [InlineData("verify", "--project", "a.csproj", "--config", "a.jsoncfg")]
    [InlineData("verify", "--project", "a.csproj", "--project-dir", "x")]
    [InlineData("verify", "--project", "a.csproj", "--verbose")]
    public void WrongCommandLineFailsWithUsage(params string[] args)
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = RunnerCommand.Run(args, output, error, new FakeEvaluator(), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitFailure, exitCode);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
        Assert.Empty(output.ToString());
    }

    [Fact]
    public void SeveralProjectsAndJsonAreAccepted()
    {
        Assert.True(RunnerCommand.TryParse(["verify", "--project", "a.csproj", "--json", "--project", "b.csproj"], out RunnerCommand.Arguments? arguments, out _));
        Assert.Equal(["a.csproj", "b.csproj"], arguments!.ProjectPaths);
        Assert.True(arguments.Json);
    }

    [Fact]
    public void MissingProjectFails()
    {
        StringWriter error = new();

        int exitCode = RunnerCommand.Run(["verify", "--project", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csproj")], new StringWriter(), error, new FakeEvaluator(), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitFailure, exitCode);
        Assert.Contains("does not exist", error.ToString(), StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------
    // Reports
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidProjectSucceedsWithATextReport()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Strings_de.json", "{ \"Hello\": \"Hallo\" }");

        StringWriter output = new();
        int exitCode = RunnerCommand.Run(["verify", "--project", project.ProjectPath], output, new StringWriter(), new FakeEvaluator(project.Items), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitSuccess, exitCode);
        string text = output.ToString();
        Assert.Contains("Strings_de.json ('de', 1 entry)", text, StringComparison.Ordinal);
        Assert.Contains("Done: 2 files checked in 1 configuration, 0 errors, 0 warnings, 0 info messages.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorsGiveExitCodeOneAndAJsonReport()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        string german = project.Add("Strings_de.json", "{\n  \"Hello\": \n}\n");

        StringWriter output = new();
        StringWriter error = new();
        int exitCode = RunnerCommand.Run(["verify", "--project", project.ProjectPath, "--json"], output, error, new FakeEvaluator(project.Items, "a note"), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitErrorsFound, exitCode);

        // The text goes to the standard error when the JSON is requested
        Assert.Contains($"{german}:3:1: error {ValidationCodes.TranslationUnparsable}:", error.ToString(), StringComparison.Ordinal);

        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        Assert.Equal(ReportWriter.JsonVersion, root.GetProperty("version").GetInt32());
        Assert.Equal(1, root.GetProperty("errors").GetInt32());

        JsonElement projectElement = Assert.Single(root.GetProperty("projects").EnumerateArray());
        Assert.Equal(project.ProjectPath, projectElement.GetProperty("project").GetString());
        Assert.Equal("a note", projectElement.GetProperty("note").GetString());

        JsonElement configuration = Assert.Single(projectElement.GetProperty("configurations").EnumerateArray());
        Assert.Equal(config, configuration.GetProperty("configuration").GetString());
        Assert.True(configuration.GetProperty("generatorInput").GetBoolean());
        Assert.Equal(2, configuration.GetProperty("files").GetArrayLength());

        JsonElement germanFile = configuration.GetProperty("files")[1];
        Assert.Equal(german, germanFile.GetProperty("path").GetString());
        Assert.Equal("namingConvention", germanFile.GetProperty("origin").GetString());
        Assert.Equal("de", germanFile.GetProperty("locale").GetString());
        Assert.Equal(1, germanFile.GetProperty("errors").GetInt32());

        JsonElement message = Assert.Single(configuration.GetProperty("messages").EnumerateArray());
        Assert.Equal("error", message.GetProperty("severity").GetString());
        Assert.Equal(ValidationCodes.TranslationUnparsable, message.GetProperty("code").GetString());
        Assert.Equal(german, message.GetProperty("file").GetString());
        Assert.Equal(3, message.GetProperty("line").GetInt32());
        Assert.Equal(1, message.GetProperty("column").GetInt32());
    }

    [Fact]
    public void ConfigurationCanBeValidatedWithoutAProject()
    {
        using TempProject project = new();
        string config = project.Write("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Write("Strings.json", DefaultJson);
        project.Write("Strings_de.json", "{ \"Hello\": \"Hallo\", \"Bye\": \"Tschüss\" }");

        StringWriter output = new();
        int exitCode = RunnerCommand.Run(["verify", "--config", config], output, new StringWriter(), new FakeEvaluator(), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitSuccess, exitCode);
        Assert.Contains($"info {ValidationCodes.KeyNotInDefault}", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SeveralProjectsGetTotals()
    {
        using TempProject first = new("First");
        first.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        first.Add("Strings.json", DefaultJson);

        using TempProject second = new("Second");

        Dictionary<string, IReadOnlyList<ProjectFile>> items = new(StringComparer.OrdinalIgnoreCase)
        {
            [first.ProjectPath] = first.Items,
            [second.ProjectPath] = second.Items,
        };

        StringWriter output = new();
        int exitCode = RunnerCommand.Run(["verify", "--project", first.ProjectPath, "--project", second.ProjectPath, "--json"], output, new StringWriter(), new FakeEvaluator(items), CancellationToken.None);

        Assert.Equal(RunnerCommand.ExitSuccess, exitCode);
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        Assert.Equal(2, document.RootElement.GetProperty("projects").GetArrayLength());
        Assert.Equal(0, document.RootElement.GetProperty("projects")[1].GetProperty("configurations").GetArrayLength());
    }

    [Fact]
    public void JsonKeepsNonAsciiCharactersReadable()
    {
        using TempProject project = new("Projekt_Übersetzung");
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);

        StringWriter output = new();
        RunnerCommand.Run(["verify", "--project", project.ProjectPath, "--json"], output, new StringWriter(), new FakeEvaluator(project.Items), CancellationToken.None);

        Assert.Contains("Projekt_Übersetzung", output.ToString(), StringComparison.Ordinal);
    }

    // -------------------------------------------------------------------------
    // Project evaluation
    // -------------------------------------------------------------------------

    [Fact]
    public void MsBuildItemsAreParsed()
    {
        string projectPath = Path.Combine(Path.GetTempPath(), "App", "App.csproj");
        string json = $$"""
            {
              "Items": {
                "AdditionalFiles": [ { "Identity": "Strings.jsoncfg", "FullPath": {{JsonSerializer.Serialize(Path.Combine(Path.GetTempPath(), "App", "Strings.jsoncfg"))}} } ],
                "None": [ { "Identity": "Strings.json" } ],
                "EmbeddedResource": [ { "Identity": "..\\Shared\\French.json", "Link": "Strings_fr.json" } ],
                "Content": []
              }
            }
            """;

        List<ProjectFile> items = ProjectEvaluator.ParseItems(json, projectPath);

        Assert.Collection(
            items,
            i => Assert.True(i.IsGeneratorInput && i.FullPath == Path.Combine(Path.GetTempPath(), "App", "Strings.jsoncfg")),
            i => Assert.True(i.ItemType == "None" && i.FullPath == Path.Combine(Path.GetTempPath(), "App", "Strings.json") && i.Link is null),
            i => Assert.True(i.ItemType == "EmbeddedResource" && i.FullPath == Path.Combine(Path.GetTempPath(), "Shared", "French.json") && i.Link == "Strings_fr.json"));
    }

    [Fact]
    public void FilesAreFoundWithoutMsBuild()
    {
        using TempProject project = new();
        project.WriteProjectFile("""<AdditionalFiles Include="Translations\*.jsoncfg" /><EmbeddedResource Include="Translations\Strings.json" />""");
        string config = project.Write("Translations/Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        string strings = project.Write("Translations/Strings.json", DefaultJson);
        project.Write("obj/Strings_de.json", "{}");

        List<ProjectFile> items = ProjectEvaluator.EvaluateFromFiles(project.ProjectPath);

        Assert.Contains(items, i => i.FullPath == config && i.IsGeneratorInput);
        Assert.Contains(items, i => i.FullPath == strings && i.ItemType == "EmbeddedResource");
        Assert.DoesNotContain(items, i => i.FullPath.Contains("obj", StringComparison.Ordinal));
    }

    [Fact]
    public void MsBuildEvaluatesTheProject()
    {
        using TempProject project = new();
        project.WriteProjectFile("""<AdditionalFiles Include="Translations\*.jsoncfg" />""");
        string config = project.Write("Translations/Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        string strings = project.Write("Translations/Strings.json", DefaultJson);

        ProjectEvaluation evaluation = new ProjectEvaluator().Evaluate(project.ProjectPath, CancellationToken.None);

        Assert.Null(evaluation.Note);
        Assert.Contains(evaluation.Items, i => i.FullPath == config && i.IsGeneratorInput);

        // The SDK includes the other files of the directory as None items
        Assert.Contains(evaluation.Items, i => i.FullPath == strings && i.ItemType == "None");
    }

    private sealed class FakeEvaluator : IProjectEvaluator
    {
        private readonly Func<string, IReadOnlyList<ProjectFile>> _items;
        private readonly string? _note;

        public FakeEvaluator()
            : this(_ => [])
        {
        }

        public FakeEvaluator(IReadOnlyList<ProjectFile> items, string? note = null)
            : this(_ => items, note)
        {
        }

        public FakeEvaluator(Dictionary<string, IReadOnlyList<ProjectFile>> items)
            : this(path => items[path])
        {
        }

        private FakeEvaluator(Func<string, IReadOnlyList<ProjectFile>> items, string? note = null)
        {
            _items = items;
            _note = note;
        }

        public ProjectEvaluation Evaluate(string projectPath, CancellationToken cancellationToken) => new(_items(projectPath), _note);
    }
}
