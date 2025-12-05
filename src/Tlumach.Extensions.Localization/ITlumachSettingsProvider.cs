namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// An interface that defines the GetOptionsFor method of the settings provider.
    /// </summary>
    public interface ITlumachSettingsProvider
    {
        /// <summary>
        /// Returns the options set for the given context.
        /// </summary>
        /// <param name="context">The context to retrieve the options for.</param>
        /// <returns>An instance of options or the default value if the options specific for the context were not found.</returns>
        TlumachLocalizationOptions GetOptionsFor(string context);
    }
}
