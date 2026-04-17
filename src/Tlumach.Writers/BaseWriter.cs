namespace Tlumach.Writers;

using Tlumach.Base;
using Tlumach;

using System.Globalization;

/// <summary>
/// The basic writer class for specialized format writers. It defines the basic methods that should be implemented by all writers.
/// </summary>
public abstract class BaseWriter
{
    protected const string ErrSingleFileFormat = "This format does not support multiple languages in one file. Use WriteTranslation method instead.";
    protected const string ErrNoConfigInTranslationManager = "The translation manager does not have a configuration to save.";
    protected const string ErrNoTranslationForCulture = "No translation found for culture {0}.";

    public abstract string ConfigExtension { get; }

    public abstract string TranslationExtension { get; }

    public abstract void WriteConfiguration(TranslationManager translationManager, Stream stream);

    /// <summary>
    /// Writes translations in the format that supports multiple languages in one file.
    /// </summary>
    /// <param name="translationManager">The translation translationManager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public abstract void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream);

    /// <summary>
    /// Writes translations in the format that supports one language in one file.
    /// </summary>
    /// <param name="translationManager">The translation translationManager from which the translations should be picked.</param>
    /// <param name="culture">The culture to write.<para>Set this parameter to <see cref="CultureInfo.InvariantCulture"/> to signal that the default translation should be written.</para></param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public abstract void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream);

    /// <summary>
    /// Writes translations.
    /// </summary>
    /// <param name="translationManager">The translation translationManager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public abstract void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream);
}
