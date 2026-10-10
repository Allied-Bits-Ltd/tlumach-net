namespace Tlumach.Writers;

using Tlumach.Base;
using Tlumach;

using System.Globalization;

/// <summary>
/// The basic writer class for specialized format writers. It defines the basic methods that should be implemented by all writers.
/// </summary>
public abstract class BaseWriter
{
    /// <summary>
    /// The message of the exception thrown by <see cref="WriteTranslations"/> of a format that holds one language per file. The format item is the <see cref="FormatName"/> of the writer.
    /// </summary>
    protected const string ErrSingleFileFormatS1 = "The {0} format does not support multiple languages in one file. Use the WriteTranslation() method instead.";

    /// <summary>
    /// The message of the exception thrown by <see cref="WriteConfiguration"/> when the translation manager has no default configuration.
    /// </summary>
    protected const string ErrNoConfigInTranslationManager = "The translation manager does not have a configuration to save.";

    /// <summary>
    /// The message of the exception thrown when the translation manager has no translation for a culture to be written. The format item is the name of the culture.
    /// </summary>
    protected const string ErrNoTranslationForCultureS1 = "No translation found for culture {0}.";

    /// <summary>
    /// Gets the name of the format (e.g., "JSON" or "RESX"), which is also used in error messages.
    /// </summary>
    public abstract string FormatName { get; }

    /// <summary>
    /// Gets the extension of the configuration files of the format, with the leading dot (e.g., ".jsoncfg"), or an empty string if the writer does not write configuration files.
    /// </summary>
    public abstract string ConfigExtension { get; }

    /// <summary>
    /// Gets the extension of the translation files of the format, with the leading dot (e.g., ".json").
    /// </summary>
    public abstract string TranslationExtension { get; }

    /// <summary>
    /// Writes the default configuration of the translation manager (<see cref="BaseTranslationManager.DefaultConfiguration"/>) as a configuration file of the format.
    /// </summary>
    /// <param name="translationManager">The translation manager whose configuration should be written.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    /// <exception cref="TlumachException">The translation manager has no default configuration, or the format does not support configuration files.</exception>
    public abstract void WriteConfiguration(TranslationManager translationManager, Stream stream);

    /// <summary>
    /// Writes translations in the format that supports multiple languages in one file.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public abstract void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream);

    /// <summary>
    /// Writes translations in the format that supports one language in one file.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="culture">The culture to write.<para>Set this parameter to <see cref="CultureInfo.InvariantCulture"/> to signal that the default translation should be written.</para></param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public abstract void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream);

    /// <summary>
    /// Writes translations.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    protected abstract void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream);

    /// <summary>
    /// Splits a hierarchical key at its last dot into the name of the section and the name of the key within the section.
    /// </summary>
    /// <param name="key">The key to split.</param>
    /// <returns>The part of <paramref name="key"/> before the last dot and the part after it, or an empty string and <paramref name="key"/> if the key contains no dot.</returns>
    protected static (string, string) GetSectionAndKeyName(string key)
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
    protected static string? GetImmediateChild(string parentKey, string descendantKey)
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

    /// <summary>
    /// Returns the entries of the translation in the order, in which they should be written.
    /// </summary>
    /// <param name="translation">The translation whose entries are needed.</param>
    /// <returns>The entries in the order of the source file if the translation keeps it (<see cref="Translation.OrderedEntries"/>); otherwise, the entries sorted by their hierarchical keys.</returns>
    protected internal static List<TranslationEntry> GetSortedEntries(Translation translation)
    {
        List<TranslationEntry> entryList;

        if (translation.OrderedEntries is not null)
        {
            entryList = translation.OrderedEntries;
        }
        else
        {
            entryList = translation.Values.ToList();
            entryList.Sort(TranslationEntry.CompareByHierarchicalKey);
        }

        return entryList;
    }

    /// <summary>
    /// Determines whether the entry should be written as a reference to another entry (<c>@</c> followed by the reference) rather than as text.
    /// </summary>
    /// <param name="entry">The entry to be written.</param>
    /// <returns><see langword="true"/> if the entry has no text but has a reference; otherwise, <see langword="false"/>.</returns>
    protected virtual bool ShouldWriteReference(TranslationEntry entry) => string.IsNullOrEmpty(entry.Text) && !string.IsNullOrEmpty(entry.Reference);
}
