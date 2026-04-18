namespace Tlumach.Writers;

using Tlumach.Base;
using Tlumach;

using System.Globalization;

/// <summary>
/// The basic writer class for specialized format writers. It defines the basic methods that should be implemented by all writers.
/// </summary>
public abstract class BaseWriter
{
    protected const string ErrSingleFileFormatS1 = "The {0} format does not support multiple languages in one file. Use the WriteTranslation() method instead.";
    protected const string ErrNoConfigInTranslationManager = "The translation manager does not have a configuration to save.";
    protected const string ErrNoTranslationForCultureS1 = "No translation found for culture {0}.";

    public abstract string FormatName { get; }

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
    protected abstract void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream);

    protected (string, string) GetSectionAndKeyName(string key)
    {
        int idx = key.LastIndexOf('.');
        if (idx == -1)
            return (string.Empty, key);
        else
            return (key.Substring(0, idx), key.Substring(idx + 1));
    }

    /// <summary>
    /// Determines if the specified key is a parent of the given potential child key.
    /// </summary>
    /// <param name="parentKey">The potential parent key.</param>
    /// <param name="childKey">The potential child key.</param>
    /// <returns>True if parentKey is a parent of childKey; otherwise, false.</returns>
    protected static bool IsParentKey(string parentKey, string childKey)
    {
        if (string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(childKey))
            return false;

        if (parentKey.Length >= childKey.Length)
            return false;

        return childKey.StartsWith(parentKey + ".", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the immediate child key from a parent and its descendant key.
    /// </summary>
    /// <param name="parentKey">The parent key.</param>
    /// <param name="descendantKey">The descendant key (child, grandchild, etc.).</param>
    /// <returns>The immediate child key name, or <see langword="null"/> if parentKey is not a parent of descendantKey.</returns>
    protected static string GetImmediateChild(string parentKey, string descendantKey)
    {
        if (string.IsNullOrEmpty(parentKey) || string.IsNullOrEmpty(descendantKey))
            return null;

        if (parentKey.Length >= descendantKey.Length)
            return null;

        if (!descendantKey.StartsWith(parentKey + ".", StringComparison.Ordinal))
            return null;

        // Remove the parent key and the dot
        string remainder = descendantKey.Substring(parentKey.Length + 1);

        // Get the first segment (up to the next dot)
        int dotIndex = remainder.IndexOf('.');
        if (dotIndex == -1)
            return remainder; // No more dots, so the remainder is the immediate child
        else
            return remainder.Substring(0, dotIndex);
    }
}
