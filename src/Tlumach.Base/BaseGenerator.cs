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

using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
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
#pragma warning restore CA1707 // Remove the underscores from member name ...

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

            TranslationConfiguration? configuration;

            // The config parser will parse configuration and will find the correct parser for the files referenced by the configuration
            BaseParser? parser = FileFormats.GetConfigParser(Path.GetExtension(configFile));
            if (parser is null)
                return null;

            TranslationTree? translationTree = parser.LoadTranslationStructure(configFile, projectDir, out configuration);

            if (configuration is null)
                throw new ParserLoadException(configFile, $"Failed to load the configuration from '{configFile}'");

            if (translationTree is null)
                throw new ParserLoadException(configFile, $"Failed to load the default language file referenced by '{configFile}'");

            // We have these checks here because a parser's ValidateConfiguration method accepts empty values (they are ok in runtime or when generators are not used).
            if (string.IsNullOrEmpty(configuration.Namespace))
                throw new ParserConfigException(configFile, $"The configuration file '{configFile}' does not contain a namespace for the class to be generated, which must be specified in the '{TranslationConfiguration.KEY_GENERATED_NAMESPACE}' setting");

            if (string.IsNullOrEmpty(configuration.ClassName))
                throw new ParserConfigException(configFile, $"The configuration file '{configFile}' does not contain a name of the class to be generated, which must be specified in the '{TranslationConfiguration.KEY_GENERATED_CLASS}' setting");

            StringBuilder builder = new();

            EmitMainBody(builder, configuration, translationTree, options);

            return builder.ToString();
        }

        private static void EmitMainBody(StringBuilder builder, TranslationConfiguration configuration, TranslationTree translationTree, Dictionary<string, string> options)
        {
            bool addLine;
            string? usingNamespace = null;
            bool delayedUnits = false;
            bool onlyDeclareKeys = false;

            if (!options.TryGetValue(OPTION_USING_NAMESPACE, out usingNamespace))
                usingNamespace = string.Empty;
            if (options.TryGetValue(OPTION_DELAYED_UNITS, out string? delayedUnitsStr))
                delayedUnits = "true".Equals(delayedUnitsStr, StringComparison.OrdinalIgnoreCase);
            if (options.TryGetValue(OPTION_ONLY_DECLARE_KEYS, out string? onlyDeclareKeysStr))
                onlyDeclareKeys = "true".Equals(onlyDeclareKeysStr, StringComparison.OrdinalIgnoreCase);

            if (configuration.DelayedUnitsCreation)
                delayedUnits = true;

            if (configuration.OnlyDeclareKeys)
                onlyDeclareKeys = true;

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
            builder.AppendLine("// <auto-generated/>\n");
            builder.AppendLine("#nullable enable\n");
            builder.AppendLine("using System;\nusing System.Reflection;\n");
            builder.AppendLine("using Tlumach.Base;");
            if (!string.IsNullOrEmpty(usingNamespace) && !usingNamespace.Equals("Tlumach", StringComparison.Ordinal))
                builder.Append("using ").Append(usingNamespace).AppendLine(";");
            builder.AppendLine("using Tlumach;\n");
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
            builder.Append("        private static TranslationConfiguration _translationConfiguration = new TranslationConfiguration(typeof(").Append(configuration.ClassName).Append(").Assembly, \"").Append(configuration.DefaultFile).Append("\", _defaultFileLocale, ").Append(configuration.GetEscapeModeFullName()).AppendLine(");\n");
            builder.AppendLine("        public static TranslationConfiguration Configuration => _translationConfiguration;");
            builder.AppendLine();
            builder.AppendLine("        ///<summary>");
            builder.AppendLine("        ///Use this instance to change the default culture or to access translations without using <seealso cref=\"TranslationUnit\"/> instances");
            builder.AppendLine("        ///</summary>");
            builder.AppendLine("        public static TranslationManager TranslationManager {get; } = new TranslationManager(_translationConfiguration);\n");

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
            foreach (var translation in configuration.Translations)
            {
                builder.Append("            _translationConfiguration.Translations.Add(\"").Append(translation.Key).Append("\", @\"").Append(translation.Value).AppendLine("\");");
            }

            if (addLine)
                builder.AppendLine();

            if (!delayedUnits && !onlyDeclareKeys)
                EmitGroupUnitInitializers(builder, translationTree.RootNode, 1, usingNamespace, string.Empty);

            builder.AppendLine("        }\n");

            EmitGroupUnitDeclarations(builder, translationTree, translationTree.RootNode, 1, usingNamespace, delayedUnits, onlyDeclareKeys, string.Empty);

            builder.AppendLine("    }\n}");
        }

        private static void EmitGroupUnitInitializers(StringBuilder builder, TranslationTreeNode node, int level, string @namespace, string namePrefix)
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

                unitClassName = "TranslationUnit";

                if (@namespace.Length > 0)
                {
                    unitClassName = @namespace + "." + unitClassName;
                }

                builder.Append(indent).Append(OwnName(value.Key)).Append(" = new ").Append(unitClassName).Append("(TranslationManager, _translationConfiguration, \"").Append(namePrefix + value.Key).Append("\", ").Append(value.IsTemplated ? "true" : "false").AppendLine(");");
            }
        }

        private static void EmitGroupUnitDeclarations(StringBuilder builder, TranslationTree translationTree, TranslationTreeNode node, int level, string @namespace, bool delayedUnits, bool onlyDeclareKeys, string namePrefix)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            if (node is null)
                throw new ArgumentNullException(nameof(node));

            var indent = new string(' ', 4 + (level << 2));

            TranslationTreeLeaf value;
            string unitClassName;

            bool groupStart = false;

            // The key here is a KeyValuePair, in which the key (and Value.Key) is the own name within the group.
            foreach (var key in node.Keys.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                value = key.Value;

                unitClassName = "TranslationUnit";

                if (@namespace.Length > 0)
                {
                    unitClassName = @namespace + "." + unitClassName;
                }

                if (groupStart)
                    builder.AppendLine();
                groupStart = true;

                string ownNameOfKey = OwnName(value.Key);
                builder.Append(indent).AppendLine("///<summary>");
                builder.Append(indent).AppendLine("///A constant which you can use instead of a string value of the key.");
                builder.Append(indent).AppendLine("///</summary>");
                builder.Append(indent).Append("public const string ").Append(ownNameOfKey).Append("Key = \"").Append(ownNameOfKey).AppendLine("\";");
                if (!onlyDeclareKeys)
                {
                    builder.AppendLine();
                    if (delayedUnits)
                    {
                        builder.Append(indent).Append("private static ").Append(unitClassName).Append("? _").Append(ownNameOfKey).AppendLine(";");

                        builder.Append(indent).AppendLine("///<summary>");
                        builder.Append(indent).AppendLine("///An instance of <see cref=\"TranslationUnit\"/> which you can use to access a translated string.");
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
                    EmitGroupUnitInitializers(builder, node.ChildNodes[child], level + 1, @namespace, namePrefix + subKey + '.');
                builder.Append(indent).AppendLine("    }\n");

                EmitGroupUnitDeclarations(builder, translationTree, node.ChildNodes[child], level + 1, @namespace, delayedUnits, onlyDeclareKeys, namePrefix + subKey + '.');
                builder.Append(indent).AppendLine("}");
            }
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
}
