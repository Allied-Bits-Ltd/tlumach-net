using Tlumach.Base;
using System.Reflection;

namespace Tlumach.Extensions.Localization
{
    public sealed class TlumachLocalizationOptions
    {
        /// <summary>
        /// Gets or sets the manager that should be used to retrieve localized strings.
        /// <para>This value if set has the highest priority, and other properties are ignored.</para>
        /// </summary>
        public TranslationManager? TranslationManager { get; set; }

        /// <summary>
        /// Gets or sets the configuration that should be used to retrieve localized strings.
        /// <para>A new instance of <seealso cref="Tlumach.TranslationManager"/> is created using this configuration.</para>
        /// <para>This property has priority unless <seealso cref="TranslationManager"/> is set.</para>
        /// </summary>
        public TranslationConfiguration? Configuration { get; set; }

        // The properties below are the alternatives to the configuration

        /// <summary>
        /// Gets or sets the optional reference to the assemly that contains the configuration file and translation files.
        /// <para>The value of this property is used when both <seealso cref="TranslationManager"/> and <seealso cref="Configuration"/> are <see langword="null"/>.</para>
        /// </summary>
        public Assembly? Assembly { get; set; }

        /// <summary>
        /// Gets or sets the optional name of the default translation file.
        /// <para>The value of this property must be provided when both <seealso cref="TranslationManager"/> and <seealso cref="Configuration"/> are <see langword="null"/> because it is used to access localized strings.</para>
        /// </summary>
        public string? DefaultFile { get; set; }

        /// <summary>
        /// Gets or sets an optional indicator of the locale of the strings in the <see cref="DefaultFile"/> translation file.
        /// </summary>
        public string? DefaultFileLocale { get; set; }

        /// <summary>
        /// Gets or sets the text processing mode that should be used when processing the strings in the default file for the purpose of determining if they contain placeholders.
        /// <para>If this property is not set, the value is derived from the format of the file.</para>
        /// </summary>
        public TextFormat? TextProcessingMode { get; set; }
    }
}
