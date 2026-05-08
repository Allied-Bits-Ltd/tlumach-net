// <copyright file="BaseGenerator.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
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

using System.Globalization;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

/// <summary>
/// Includes core functions that generate the C# source code.
/// </summary>
public class BaseGenerator
{
#pragma warning disable CA1707 // Remove the underscores from member name ...
    protected const string OPTION_DELAYED_UNITS = "DelayedUnitCreation";
    protected const string OPTION_ONLY_DECLARE_KEYS = "OnlyDeclareKeys";
    protected const string OPTION_USING_NAMESPACE = "UsingNamespace";
    protected const string OPTION_EXTRA_PARSERS = "ExtraParsers";
    protected const string OPTION_FILLED_METHODS = "CreateFilledMethods";
#pragma warning restore CA1707 // Remove the underscores from member name ...

    private static string _indentStep = new string(' ', 4);

    private static string OwnName(string keyName)
    {
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
        int idx = keyName.IndexOf('.');
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

        if (idx == -1)
            return keyName;
        else
        if (idx == keyName.Length - 1)
            return string.Empty;
        else
            return keyName.Substring(idx + 1);
    }

    protected BaseGenerator()
    {
        // this constructor does nothing
    }

    protected static string? GenerateClass(string configFile, string projectDir, Dictionary<string, string> options)
    {
        if (configFile is null)
            throw new ArgumentNullException(nameof(configFile));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (projectDir is null)
            projectDir = string.Empty;

        string relativeDir = string.Empty;
        string? baseConfigFileDir = Path.GetDirectoryName(configFile);
        //string? baseConfigFileDir2 = baseConfigFileDir;

        BaseParser.PopulateKeyLocations = true;

        if (!string.IsNullOrEmpty(baseConfigFileDir))
        {
            baseConfigFileDir = Path.GetFullPath(baseConfigFileDir);
            char lastChar = baseConfigFileDir.Length > 0 ? baseConfigFileDir[baseConfigFileDir.Length - 1] : '\0';
            if (!((lastChar == Path.DirectorySeparatorChar) || (Path.AltDirectorySeparatorChar != '0' && lastChar == Path.AltDirectorySeparatorChar)))
            {
                baseConfigFileDir = baseConfigFileDir + Path.DirectorySeparatorChar;
            }

            lastChar = projectDir.Length > 0 ? projectDir[projectDir.Length - 1] : '\0';
            if (!((lastChar == Path.DirectorySeparatorChar) || (Path.AltDirectorySeparatorChar != '0' && lastChar == Path.AltDirectorySeparatorChar)))
            {
                projectDir = projectDir + Path.DirectorySeparatorChar;
            }

            if (baseConfigFileDir.Length > 0 && baseConfigFileDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).StartsWith(projectDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), StringComparison.InvariantCultureIgnoreCase))
            {
                relativeDir = Path.GetDirectoryName(baseConfigFileDir!.Substring(projectDir.Length)) ?? string.Empty;
            }
        }

        TranslationConfiguration? configuration;

        // The config parser will parse configuration and will find the correct parser for the files referenced by the configuration
        BaseParser? parser = FileFormats.GetConfigParser(Path.GetExtension(configFile));
        if (parser is null)
            return null;

        Translation? translation = null;

        TranslationTree? translationTree = parser.LoadTranslationStructure(configFile, projectDir, out configuration, out translation);

        if (configuration is null)
            throw new ParserLoadException(configFile, $"Failed to load the configuration from '{configFile}'");

        if (translationTree is null)
            throw new ParserLoadException(configFile, $"Failed to load the default language file referenced by '{configFile}'");

        // We have these checks here because a parser's ValidateConfiguration method accepts empty values (they are ok in runtime or when generators are not used).
        if (string.IsNullOrEmpty(configuration.Namespace))
            throw new ParserConfigException(configFile, $"The configuration file '{configFile}' does not contain a namespace for the class to be generated, which must be specified in the '{TranslationConfiguration.KEY_GENERATED_NAMESPACE}' setting");

        if (string.IsNullOrEmpty(configuration.ClassName))
            throw new ParserConfigException(configFile, $"The configuration file '{configFile}' does not contain a name of the class to be generated, which must be specified in the '{TranslationConfiguration.KEY_GENERATED_CLASS}' setting");

        /*BaseTranslationManager translationManager = new(configuration);
        translationManager.LoadFromDisk = true;
        if (!string.IsNullOrEmpty(baseConfigFileDir2))
            translationManager.TranslationsDirectory = baseConfigFileDir2;

        Translation? translation = translationManager.LoadTranslation(CultureInfo.InvariantCulture);*/

        if (translation is not null)
        {
            // Clear the stale entries before re-populating the index with the new ones from the translation file. This is necessary to maintain the accuracy of the index when files are updated or reprocessed.
            if (!string.IsNullOrEmpty(translation.OriginalFile))
                KeyIndex.ClearFile(translation.OriginalFile!);

            foreach (TranslationEntry entry in translation.Values)
            {
                if (entry.KeyLocated is not null)
                {
                    entry.KeyLocated.Namespace = configuration.Namespace;
                    entry.KeyLocated.ClassName = configuration.ClassName;
                    entry.KeyLocated.FilePath = translation.OriginalFile;
                    KeyIndex.Register(configuration.Namespace, configuration.ClassName, entry.Key, entry.KeyLocated);
                }
            }
        }

        TextFormat textFormat = configuration.TextProcessingMode ?? TextFormat.None;

        StringBuilder builder = new();

        EmitMainBody(builder, configuration, translationTree, relativeDir, translation, options, textFormat);

        return builder.ToString();
    }

    private static void EmitMainBody(StringBuilder builder, TranslationConfiguration configuration, TranslationTree translationTree, string relativeDir, Translation? translation, Dictionary<string, string> options, TextFormat textProcessingMode)
    {
        bool addLine;
        string? usingNamespace = null;
        bool delayedUnits = false;
        bool onlyDeclareKeys = false;
        bool createFilledMethods = false;

        if (!options.TryGetValue(OPTION_USING_NAMESPACE, out usingNamespace))
            usingNamespace = string.Empty;

        if (options.TryGetValue(OPTION_DELAYED_UNITS, out string? delayedUnitsStr))
            delayedUnits = "true".Equals(delayedUnitsStr, StringComparison.OrdinalIgnoreCase);

        if (options.TryGetValue(OPTION_FILLED_METHODS, out string? createFilledMethodsStr))
            createFilledMethods = "true".Equals(createFilledMethodsStr, StringComparison.OrdinalIgnoreCase);

        if (options.TryGetValue(OPTION_ONLY_DECLARE_KEYS, out string? onlyDeclareKeysStr))
            onlyDeclareKeys = "true".Equals(onlyDeclareKeysStr, StringComparison.OrdinalIgnoreCase);

        if (configuration.CreateFilledMethods)
            createFilledMethods = true;

        if (configuration.DelayedUnitsCreation)
            delayedUnits = true;

        if (configuration.OnlyDeclareKeys)
            onlyDeclareKeys = true;

        if (translation is null)
            createFilledMethods = false;

        // Collect the required parsers
        List<string> parserClassNames = CollectRequiredParsers(configuration);

        // Pick extra parsers from the project settings and add them to the list
        string? extraParsers = null;
        if (!options.TryGetValue(OPTION_EXTRA_PARSERS, out extraParsers))
            extraParsers = string.Empty;
        foreach (string extraParser in extraParsers.Split(',', ';', ' '))
        {
            if (extraParser.Length > 0 && !parserClassNames.Contains(extraParser, StringComparer.OrdinalIgnoreCase))
                parserClassNames.Add(extraParser);
        }

        builder.Append("// ").AppendLine(configuration.DefaultFile);
        builder.AppendLine("// <auto-generated/>").AppendLine();
        builder.AppendLine("#nullable enable").AppendLine();
        builder.AppendLine("using System;");
        if (createFilledMethods)
            builder.AppendLine("using System.Globalization;");

        builder.AppendLine("using System.Reflection;").AppendLine();
        builder.AppendLine("using Tlumach.Base;");
        if (!string.IsNullOrEmpty(usingNamespace) && !usingNamespace.Equals("Tlumach", StringComparison.Ordinal))
            builder.Append("using ").Append(usingNamespace).AppendLine(";");
        builder.AppendLine("using Tlumach;").AppendLine();
        builder.Append("namespace ").AppendLine(configuration.Namespace);
        builder.AppendLine("{");
        builder.AppendLine("    ///<summary>");
        builder.AppendLine("    ///An automatically generated class with translation units and string constants, using which you can access translated strings.");
        builder.AppendLine("    ///</summary>");

        builder.Append("    public sealed class ").AppendLine(configuration.ClassName);
        builder.AppendLine("    {");
        if (!string.IsNullOrEmpty(configuration.DefaultFileLocale))
            builder.Append("        private static string? _defaultFileLocale = \"").Append(configuration.DefaultFileLocale).AppendLine("\";");
        else
            builder.AppendLine("        private static string? _defaultFileLocale = null;");
        builder.AppendLine();
        builder.Append("        private static TranslationConfiguration _translationConfiguration = new TranslationConfiguration(typeof(").Append(configuration.ClassName).Append(").Assembly, @\"").Append(configuration.DefaultFile).Append("\", _defaultFileLocale, ").Append(configuration.GetEscapeModeFullName()).Append(')');
        if (!string.IsNullOrEmpty(relativeDir))
            builder.Append(" { DirectoryHint = @\"").Append(relativeDir).Append("\", }");
        builder.AppendLine(";").AppendLine();

        builder.AppendLine("        public static TranslationConfiguration Configuration => _translationConfiguration;");
        builder.AppendLine();
        builder.AppendLine("        ///<summary>");
        builder.AppendLine("        ///Use this instance to change the default culture or to access translations without using <seealso cref=\"TranslationUnit\"/> instances");
        builder.AppendLine("        ///</summary>");
        builder.AppendLine("        public static TranslationManager TranslationManager {get; } = new TranslationManager(_translationConfiguration);").AppendLine();

        builder.Append("        static ").Append(configuration.ClassName).AppendLine("()");
        builder.AppendLine("        {");

        addLine = false;
        foreach (var parserClassName in parserClassNames)
        {
            builder.Append("            ").Append(parserClassName).AppendLine(".Use();");
            addLine = true;
        }

        if (addLine)
            builder.AppendLine();

        addLine = false;
        foreach (var configTranslation in configuration.Translations)
        {
            builder.Append("            _translationConfiguration.Translations.Add(\"").Append(configTranslation.Key).Append("\", @\"").Append(configTranslation.Value).AppendLine("\");");
        }

        if (addLine)
            builder.AppendLine();

        if (!delayedUnits && !onlyDeclareKeys)
            EmitGroupUnitInitializers(builder, translationTree.RootNode, 1, usingNamespace, string.Empty, createFilledMethods);

        builder.AppendLine("        }").AppendLine();

        EmitGroupUnitDeclarations(builder, translationTree, translationTree.RootNode, 1, usingNamespace, delayedUnits, onlyDeclareKeys, createFilledMethods, string.Empty, textProcessingMode, translation);

        builder.AppendLine("    }").AppendLine().AppendLine("}");
    }

    private static void EmitGroupUnitInitializers(StringBuilder builder, TranslationTreeNode node, int level, string @namespace, string namePrefix, bool createFilledMethods)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (node is null)
            throw new ArgumentNullException(nameof(node));

        var indent = new string(' ', 8 + (level << 2));

        TranslationTreeLeaf value;
        string unitClassName;

        // The key here is a KeyValuePair, in which the key (and Value.Key) is the own name within the group.
        foreach (var key in node.Keys.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            value = key.Value;

            if (value.IsTemplated && createFilledMethods)
            {
                unitClassName = value.Key.Replace(".", string.Empty) + "TranslationUnit";
                if (char.IsLower(unitClassName[0]))
                    unitClassName = char.ToUpperInvariant(unitClassName[0]) + unitClassName.Substring(1);
            }
            else
            {
                unitClassName = "TranslationUnit";

                if (@namespace.Length > 0)
                {
                    unitClassName = @namespace + "." + unitClassName;
                }
            }

            builder.Append(indent).Append(OwnName(value.Key)).Append(" = new ").Append(unitClassName).Append("(TranslationManager, _translationConfiguration, \"").Append(namePrefix + value.Key).Append("\", ").Append(value.IsTemplated ? "true" : "false").AppendLine(");");
        }
    }

    private static void EmitGroupUnitDeclarations(StringBuilder builder, TranslationTree translationTree, TranslationTreeNode node, int level, string @namespace, bool delayedUnits, bool onlyDeclareKeys, bool createFilledMethods, string namePrefix, TextFormat textProcessingMode, Translation? translation)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (node is null)
            throw new ArgumentNullException(nameof(node));

        var indent = new string(' ', 4 + (level << 2));

        TranslationTreeLeaf value;
        string unitClassName, baseClassName = string.Empty;

        bool groupStart = false;

        TranslationEntry? entry = null;

        // The key here is a KeyValuePair, in which the key (and Value.Key) is the own name within the group.
        foreach (var key in node.Keys.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            value = key.Value;

            _ = translation?.TryGetValue(namePrefix + value.Key, out entry);

            unitClassName = "TranslationUnit";

            if (@namespace.Length > 0)
            {
                unitClassName = @namespace + "." + unitClassName;
            }

            if (value.IsTemplated && createFilledMethods)
            {

                baseClassName = unitClassName;
                unitClassName = value.Key.Replace(".", string.Empty) + "TranslationUnit";
                if (char.IsLower(unitClassName[0]))
                    unitClassName = char.ToUpperInvariant(unitClassName[0]) + unitClassName.Substring(1);
            }

            if (groupStart)
                builder.AppendLine();

            groupStart = true;

            string? keyDefaultValue = null;

            if (entry is not null && !string.IsNullOrEmpty(entry.Text))
            {
                keyDefaultValue = string.Concat(
                    "///\"",
                    entry.Text!
                        .Replace("&", "&amp;") // Must be first!
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&apos;")
                        .Replace("\n", "\n" + indent + "///"),
                    "\"");
            }

            string ownNameOfKey = OwnName(value.Key);
            builder.Append(indent).AppendLine("///<summary>");
            builder.Append(indent).AppendLine("///A constant which you can use instead of a string value of the key.");
            builder.Append(indent).AppendLine("///</summary>");
            builder.Append(indent).Append("public const string ").Append(ownNameOfKey).Append("Key = \"").Append(ownNameOfKey).AppendLine("\";");
            if (!onlyDeclareKeys)
            {
                builder.AppendLine();

                if (value.IsTemplated && createFilledMethods && entry is not null)
                {
                    EmitSubClass(entry, indent, unitClassName, baseClassName, textProcessingMode, builder);
                }

                if (delayedUnits)
                {
                    builder.Append(indent).Append("private static ").Append(unitClassName).Append("? _").Append(ownNameOfKey).AppendLine(";");

                    builder.Append(indent).AppendLine("///<summary>");
                    builder.Append(indent).AppendLine("///An instance of <see cref=\"TranslationUnit\"/> which you can use to access a translated string.");
                    if (!string.IsNullOrEmpty(keyDefaultValue))
                    {
                        builder.Append(indent).AppendLine("///<para>Original: ");
                        builder.Append(indent).Append(keyDefaultValue).AppendLine("</para>");
                    }

                    builder.Append(indent).AppendLine("///</summary>");
                    builder.Append(indent).Append("public static ").Append(unitClassName).Append(' ').AppendLine(ownNameOfKey);
                    builder.Append(indent).AppendLine("{");
                    builder.Append(indent).AppendLine("    get");
                    builder.Append(indent).AppendLine("    {");
                    builder.Append(indent).Append("        if (_").Append(ownNameOfKey).AppendLine(" is null)");
                    builder.Append(indent).Append("            _").Append(ownNameOfKey).Append(" = new ").Append(unitClassName).Append("(TranslationManager, _translationConfiguration, \"").Append(namePrefix + value.Key).Append("\", ").Append(value.IsTemplated ? "true" : "false").AppendLine(");");
                    builder.Append(indent).Append("        return _").Append(OwnName(value.Key)).AppendLine(";");
                    builder.Append(indent).AppendLine("    }");
                    builder.Append(indent).AppendLine("}");
                }
                else
                {
                    builder.Append(indent).AppendLine("///<summary>");
                    builder.Append(indent).AppendLine("///An instance of <see cref=\"TranslationUnit\"/> which you can use to access a translated string.");
                    if (!string.IsNullOrEmpty(keyDefaultValue))
                    {
                        builder.Append(indent).AppendLine("///<para>Original: ");
                        builder.Append(indent).Append(keyDefaultValue).AppendLine("</para>");
                    }

                    builder.Append(indent).AppendLine("///</summary>");
                    builder.Append(indent).Append("public static readonly ").Append(unitClassName).Append(' ').Append(ownNameOfKey).AppendLine(";");
                }
            }
        }

        string subKey;
        foreach (var child in node.ChildNodes.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            subKey = node.ChildNodes[child].Name;

            if (groupStart)
                builder.AppendLine();
            groupStart = true;
            builder.Append(indent).AppendLine("///<summary>");
            builder.Append(indent).AppendLine("///An automatically generated class with translation units and string constants, using which you can access translated strings.");
            builder.Append(indent).AppendLine("///</summary>");

            builder.Append(indent).Append("public static class ").AppendLine(subKey);
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).Append("    static ").Append(subKey).AppendLine("()");
            builder.Append(indent).AppendLine("    {");
            if (!delayedUnits)
                EmitGroupUnitInitializers(builder, node.ChildNodes[child], level + 1, @namespace, namePrefix + subKey + '.', createFilledMethods);
            builder.Append(indent).AppendLine("    }").AppendLine();

            EmitGroupUnitDeclarations(builder, translationTree, node.ChildNodes[child], level + 1, @namespace, delayedUnits, onlyDeclareKeys, createFilledMethods, namePrefix + subKey + '.', textProcessingMode, translation);
            builder.Append(indent).AppendLine("}");
        }
    }

    static void EmitSubClass(TranslationEntry entry, string indent, string unitClassName, string baseClassName, TextFormat textProcessingMode, StringBuilder builder)
    {
        builder.Append(indent).AppendLine("///<summary>");
        builder.Append(indent).AppendLine("///A subclass that offers the Filled method for handy passing of parameters");
        builder.Append(indent).AppendLine("///</summary>");
        builder.Append(indent).Append("public class ").Append(unitClassName).Append(": ").AppendLine(baseClassName);
        builder.Append(indent).AppendLine("{");

        string classIndent = indent + _indentStep;
        string methodIndent = classIndent + _indentStep;
        string methodIndent2 = methodIndent + _indentStep;
        string methodIndent3 = methodIndent2 + _indentStep;
        string methodIndent4 = methodIndent3 + _indentStep;
        string methodIndent5 = methodIndent4 + _indentStep;

        string inputText = (!string.IsNullOrEmpty(entry.EscapedText) ? entry.EscapedText : entry.Text) ?? string.Empty;

        var placeholders = entry.CollectPlaceholders(inputText!, textProcessingMode);

        builder.Append(classIndent).Append("public ").Append(unitClassName).AppendLine("(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)");
        builder.Append(classIndent).AppendLine("        : base(translationManager, translationConfiguration, key, containsPlaceholders)");
        builder.Append(classIndent).AppendLine("{");
        builder.Append(classIndent).AppendLine("}").AppendLine();

        builder.Append(classIndent).AppendLine(entry.BuildFilledMethodSignature(placeholders, textProcessingMode));
        builder.Append(classIndent).AppendLine("{");
        builder.Append(methodIndent).Append("return Filled(TranslationManager.CurrentCulture");
        foreach (var placeholderPair in placeholders)
        {
            builder.Append(", ").Append(placeholderPair.Name);
        }

        builder.AppendLine(");");

        builder.Append(classIndent).AppendLine("}").AppendLine();

        builder.Append(classIndent).AppendLine(entry.BuildFilledMethodSignature(placeholders, textProcessingMode, true));
        builder.Append(classIndent).AppendLine("{");

        builder.Append(methodIndent).Append("return InternalGetEntry(culture)?.ProcessTemplatedValue(culture, TextFormat.").Append(textProcessingMode.ToString()).AppendLine(", (name, index) => ");
        builder.Append(methodIndent).AppendLine("{");

        foreach (var placeholderPair in placeholders)
        {
            builder.Append(methodIndent2).Append("if (name == \"").Append(placeholderPair.Name).AppendLine("\")");
            builder.Append(methodIndent2).AppendLine("{");
            builder.Append(methodIndent3).Append("return ").Append(placeholderPair.Name).AppendLine(";");
            builder.Append(methodIndent2).AppendLine("}");
            builder.Append(methodIndent2).AppendLine("else");
        }

        builder.Append(methodIndent2).AppendLine("{");
        builder.Append(methodIndent3).AppendLine("int lIndex = -1;");
        if (placeholders.Any((p) => p.Name.StartsWith("arg") && int.TryParse(p.Name.Substring(4), out int lIndex) && lIndex >= 0 && lIndex < placeholders.Count))
        {
            builder.Append(methodIndent3).Append("if ((name.StartsWith(\"arg\") && name.Length >= 4 && int.TryParse(name.Substring(4), out lIndex) && lIndex >= 0 && lIndex <").Append(placeholders.Count).AppendLine("))");
            builder.Append(methodIndent3).AppendLine("{");
            builder.Append(methodIndent4).AppendLine("switch (lIndex)");
            builder.Append(methodIndent4).AppendLine("{");
            for (int i = 0; i < placeholders.Count; i++)
                builder.Append(methodIndent5).Append("case ").Append(i).Append(": return arg").Append(i).AppendLine(";");

            builder.Append(methodIndent4).AppendLine("}");
            builder.Append(methodIndent3).AppendLine("}");
        }

        builder.Append(methodIndent3).AppendLine("lIndex = index;");
        builder.Append(methodIndent3).Append("if (lIndex >= 0 && lIndex < ").Append(placeholders.Count).AppendLine(")");
        builder.Append(methodIndent3).AppendLine("{");
        builder.Append(methodIndent4).AppendLine("switch(lIndex)");
        builder.Append(methodIndent4).AppendLine("{");
        for (int i = 0; i < placeholders.Count; i++)
            builder.Append(methodIndent5).Append("case ").Append(i).Append(": return ").Append(placeholders[i].Name).AppendLine(";");

        builder.Append(methodIndent4).AppendLine("}");
        builder.Append(methodIndent3).AppendLine("}");
        builder.Append(methodIndent2).AppendLine("}");

        builder.Append(methodIndent2).AppendLine("return null;");

        builder.Append(methodIndent).AppendLine("}) ?? string.Empty;");

        builder.Append(classIndent).AppendLine("}");
        builder.Append(indent).AppendLine("}").AppendLine();
    }

    private static List<string> CollectRequiredParsers(TranslationConfiguration configuration)
    {
        List<string> result = [];
        BaseParser? parser = null;
        string parserType;
        parser = FileFormats.GetParser(Path.GetExtension(configuration.DefaultFile));
        if (parser is not null)
            result.Add(parser.GetType().Name);

        foreach (var translation in configuration.Translations)
        {
            parser = FileFormats.GetParser(Path.GetExtension(translation.Value));
            if (parser is not null)
            {
                parserType = parser.GetType().Name;
                if (!result.Contains(parserType, StringComparer.Ordinal))
                    result.Add(parserType);
            }
        }

        return result;
    }
}
