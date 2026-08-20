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
    protected const string OPTION_STRING_ACCESSORS = "CreateStringAccessors";
    protected const string OPTION_STRING_ACCESSORS_CLASS = "StringAccessorsClass";
    protected const string OPTION_STRING_ACCESSORS_CULTURE = "StringAccessorsCulture";
    protected const string OPTION_USE_CONTEXT_CULTURE = "UseContextCulture";
#pragma warning restore CA1707 // Remove the underscores from member name ...

    /// <summary>
    /// The name of the class with string accessors that is generated when no other name is configured.
    /// </summary>
    private const string DefaultStringAccessorsClassName = "Texts";

    /// <summary>
    /// The value of the string accessor culture option that makes the accessors read the ambient culture instead of the culture of the translation manager.
    /// </summary>
    private const string StringAccessorsCultureAmbient = "ambient";

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

            baseConfigFileDir = baseConfigFileDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            projectDir = projectDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (projectDir.Length > 0 && baseConfigFileDir.Length > projectDir.Length && baseConfigFileDir.StartsWith(projectDir, StringComparison.InvariantCultureIgnoreCase))
            {
                relativeDir = Path.GetDirectoryName(baseConfigFileDir.Substring(projectDir.Length));
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
        bool useContextCulture = false;

        if (!options.TryGetValue(OPTION_USING_NAMESPACE, out usingNamespace))
            usingNamespace = string.Empty;

        if (options.TryGetValue(OPTION_DELAYED_UNITS, out string? delayedUnitsStr))
            delayedUnits = "true".Equals(delayedUnitsStr, StringComparison.OrdinalIgnoreCase);

        if (options.TryGetValue(OPTION_FILLED_METHODS, out string? createFilledMethodsStr))
            createFilledMethods = "true".Equals(createFilledMethodsStr, StringComparison.OrdinalIgnoreCase);

        if (options.TryGetValue(OPTION_USE_CONTEXT_CULTURE, out string? useContextCultureStr))
            useContextCulture = "true".Equals(useContextCultureStr, StringComparison.OrdinalIgnoreCase);

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

        StringAccessorOptions? stringAccessors = BuildStringAccessorOptions(configuration, options, onlyDeclareKeys);

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

        if (useContextCulture)
            builder.AppendLine("            TranslationManager.UseContextCulture = true;").AppendLine();

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

        EmitGroupUnitDeclarations(builder, translationTree, translationTree.RootNode, 1, usingNamespace, delayedUnits, onlyDeclareKeys, createFilledMethods, string.Empty, textProcessingMode, translation, stringAccessors);

        builder.AppendLine("    }").AppendLine().AppendLine("}");
    }

    /// <summary>
    /// Reads the options of the string accessors and returns them, or <see langword="null"/> when no string accessors are to be generated.
    /// </summary>
    /// <param name="configuration">The configuration read from the configuration file.</param>
    /// <param name="options">The options passed by the build.</param>
    /// <param name="onlyDeclareKeys">Whether only key constants are generated.</param>
    /// <returns>The options, or <see langword="null"/> when the accessors are switched off.</returns>
    private static StringAccessorOptions? BuildStringAccessorOptions(TranslationConfiguration configuration, Dictionary<string, string> options, bool onlyDeclareKeys)
    {
        bool createStringAccessors = false;

        if (options.TryGetValue(OPTION_STRING_ACCESSORS, out string? createStringAccessorsStr))
            createStringAccessors = "true".Equals(createStringAccessorsStr, StringComparison.OrdinalIgnoreCase);

        if (configuration.CreateStringAccessors)
            createStringAccessors = true;

        // With onlyDeclareKeys there are no translation units to read the text from, so the two options are mutually exclusive and this one gives way.
        if (onlyDeclareKeys || !createStringAccessors)
            return null;

        if (!options.TryGetValue(OPTION_STRING_ACCESSORS_CLASS, out string? className) || string.IsNullOrEmpty(className))
            className = DefaultStringAccessorsClassName;

        if (!string.IsNullOrEmpty(configuration.StringAccessorsClass))
            className = configuration.StringAccessorsClass!;

        if (!options.TryGetValue(OPTION_STRING_ACCESSORS_CULTURE, out string? culture) || string.IsNullOrEmpty(culture))
            culture = null;

        if (!string.IsNullOrEmpty(configuration.StringAccessorsCulture))
            culture = configuration.StringAccessorsCulture;

        return new StringAccessorOptions(
            className!,
            StringAccessorsCultureAmbient.Equals(culture, StringComparison.OrdinalIgnoreCase),
            "global::" + configuration.Namespace + "." + configuration.ClassName);
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

    private static void EmitGroupUnitDeclarations(StringBuilder builder, TranslationTree translationTree, TranslationTreeNode node, int level, string @namespace, bool delayedUnits, bool onlyDeclareKeys, bool createFilledMethods, string namePrefix, TextFormat textProcessingMode, Translation? translation, StringAccessorOptions? stringAccessors)
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

        List<KeyValuePair<string, string?>>? accessors = stringAccessors is not null ? new List<KeyValuePair<string, string?>>() : null;

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

            string? keyDefaultValue = BuildOriginalDocBlock(entry?.Text, indent);

            string ownNameOfKey = OwnName(value.Key);

            if (accessors is not null)
                accessors.Add(new KeyValuePair<string, string?>(ownNameOfKey, entry?.Text));

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

        if (stringAccessors is not null && accessors is not null && accessors.Count > 0)
        {
            if (groupStart)
                builder.AppendLine();

            groupStart = true;

            EmitStringAccessors(builder, node, accessors, indent, namePrefix, stringAccessors);
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
            // The onlyDeclareKeys check matters as much here as it does for the class that declares the groups: without it the static constructor of a group would assign units that were never declared,
            // and the generated code would not compile.
            if (!delayedUnits && !onlyDeclareKeys)
                EmitGroupUnitInitializers(builder, node.ChildNodes[child], level + 1, @namespace, namePrefix + subKey + '.', createFilledMethods);
            builder.Append(indent).AppendLine("    }").AppendLine();

            EmitGroupUnitDeclarations(builder, translationTree, node.ChildNodes[child], level + 1, @namespace, delayedUnits, onlyDeclareKeys, createFilledMethods, namePrefix + subKey + '.', textProcessingMode, translation, stringAccessors);
            builder.Append(indent).AppendLine("}");
        }
    }

    /// <summary>
    /// Builds the block of an XML documentation comment that shows the original text of a key, escaped for XML and indented for the given level.
    /// </summary>
    /// <param name="text">The original text, or <see langword="null"/> when there is none.</param>
    /// <param name="indent">The indentation of the member that the comment belongs to. It is baked into the replacement of the line breaks, so the block has to be built again for every level.</param>
    /// <returns>The block, or <see langword="null"/> when there is no text to show.</returns>
    private static string? BuildOriginalDocBlock(string? text, string indent)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        return string.Concat(
            "///\"",
            text!
                .Replace("&", "&amp;") // Must be first!
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("\n", "\n" + indent + "///"),
            "\"");
    }

    /// <summary>
    /// Emits the nested class with the string accessors of the keys of one node of the translation tree.
    /// <para>The accessors exist so that the attributes that localize through a resource type and the name of a property, such as DisplayAttribute and RequiredAttribute, can be pointed at a class
    /// generated by Tlumach. Those attributes require a public static property of the type <see cref="string"/> that is declared on the type named in the annotation, which neither the translation unit
    /// members nor a shared base class can provide.</para>
    /// </summary>
    /// <param name="builder">The builder that receives the generated code.</param>
    /// <param name="node">The node, whose keys are emitted. Used to detect a name conflict with the class being generated.</param>
    /// <param name="accessors">The own name of every key of the node together with its original text.</param>
    /// <param name="indent">The indentation of the class that declares the keys.</param>
    /// <param name="namePrefix">The dotted path of the group classes that lead to this node, empty for the root.</param>
    /// <param name="options">The options of the string accessors.</param>
    private static void EmitStringAccessors(StringBuilder builder, TranslationTreeNode node, List<KeyValuePair<string, string?>> accessors, string indent, string namePrefix, StringAccessorOptions options)
    {
        // A key or a group of the very name of the class being generated would produce two members with one name, which does not compile. Report it in a way that does not break the build and leave the
        // accessors of this node out; the StringAccessorsClass option is the way out of the conflict.
        if (NameIsTaken(node, options.ClassName))
        {
            builder.Append(indent).Append("#warning Tlumach did not generate the '").Append(options.ClassName)
                .Append("' class with string accessors here because a key or a group of that name exists in the translation. Set the stringAccessorsClass option, or the TlumachGeneratorStringAccessorsClass property, to another name.")
                .AppendLine();
            return;
        }

        string memberIndent = indent + _indentStep;

        builder.Append(indent).AppendLine("///<summary>");
        builder.Append(indent).AppendLine("///Static string properties that return the translated texts of the keys of this class.");
        builder.Append(indent).AppendLine("///<para>Use this class as the resource type of an attribute that localizes through a resource type and the name of a property, such as <c>DisplayAttribute</c> or <c>RequiredAttribute</c>.</para>");
        builder.Append(indent).AppendLine("///</summary>");
        builder.Append(indent).Append("public static class ").AppendLine(options.ClassName);
        builder.Append(indent).AppendLine("{");

        bool addLine = false;
        foreach (var accessor in accessors)
        {
            if (addLine)
                builder.AppendLine();

            addLine = true;

            string? original = BuildOriginalDocBlock(accessor.Value, memberIndent);

            builder.Append(memberIndent).AppendLine("///<summary>");
            builder.Append(memberIndent).Append("///The translated text of the '").Append(namePrefix).Append(accessor.Key).Append("' key for ").AppendLine(options.AmbientCulture ? "the current culture of the thread." : "the current culture of the translation manager.");
            if (!string.IsNullOrEmpty(original))
            {
                builder.Append(memberIndent).AppendLine("///<para>Original: ");
                builder.Append(memberIndent).Append(original).AppendLine("</para>");
            }

            builder.Append(memberIndent).AppendLine("///</summary>");

            // The path has to be qualified with global:: and with the whole namespace: inside the generated class the accessor has the very name of the translation unit it reads, so an unqualified
            // reference would bind to the accessor itself.
            builder.Append(memberIndent).Append("public static string ").Append(accessor.Key).Append(" => ").Append(options.OwnerPath).Append('.').Append(namePrefix).Append(accessor.Key);
            // The template is deliberately returned unprocessed. An accessor exists to be the resource type of an attribute, and such an attribute passes the text to String.Format together with its own
            // arguments, so the positional placeholders of a validation message have to survive. Reading the processed value would strip them whenever textProcessingMode is DotNet or Arb, which turns
            // "The {0} field is required." into "The  field is required.".
            builder.AppendLine(options.AmbientCulture ? ".GetValueAsTemplate(System.Globalization.CultureInfo.CurrentCulture);" : ".CurrentTemplate;");
        }

        builder.Append(indent).AppendLine("}");
    }

    /// <summary>
    /// Tells whether a key or a group of the given node already uses the given name.
    /// </summary>
    /// <param name="node">The node to inspect.</param>
    /// <param name="name">The name to look for.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    private static bool NameIsTaken(TranslationTreeNode node, string name)
    {
        foreach (var key in node.Keys)
        {
            if (string.Equals(OwnName(key.Value.Key), name, StringComparison.Ordinal))
                return true;
        }

        foreach (var child in node.ChildNodes.Keys)
        {
            if (string.Equals(node.ChildNodes[child].Name, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The options that control the generation of the nested class with string accessors.
    /// </summary>
    private sealed class StringAccessorOptions
    {
        internal StringAccessorOptions(string className, bool ambientCulture, string ownerPath)
        {
            ClassName = className;
            AmbientCulture = ambientCulture;
            OwnerPath = ownerPath;
        }

        /// <summary>
        /// Gets the name of the generated class.
        /// </summary>
        internal string ClassName { get; }

        /// <summary>
        /// Gets the indicator of whether the accessors read the ambient culture of the thread rather than the culture of the translation manager.
        /// </summary>
        internal bool AmbientCulture { get; }

        /// <summary>
        /// Gets the fully qualified name of the generated class that declares the translation units, used to refer to them from inside the accessors.
        /// </summary>
        internal string OwnerPath { get; }
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
