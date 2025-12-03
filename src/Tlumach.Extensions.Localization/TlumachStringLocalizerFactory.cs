using Microsoft.Extensions.Localization;

using System.Globalization;
using System.Reflection;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Creates instances of <see cref="TlumachStringLocalizer"/>.
    /// </summary>
    public sealed class TlumachStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ITlumachSettingsProvider _settingsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLocalizerFactory"/> class using the given configuration provider.
        /// </summary>
        /// <param name="settingsProvider">The options to use when creating <see cref="TlumachStringLocalizer"/> instances.</param>
        public TlumachStringLocalizerFactory(ITlumachSettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;
        }

        /// <summary>
        /// Creates an instance of <see cref="TlumachStringLocalizer"/> from the options or from the reference to the generated class.
        /// </summary>
        /// <param name="resourceSource">The type of the class created by Tlumach Generator.</param>
        /// <returns>An instance of <see cref="TlumachStringLocalizer"/>.</returns>
        /// <exception cref="TlumachException">Thrown if the TranslationManager instance cannot be obtained from the class provided in <paramref name="resourceSource"/>.</exception>
        public IStringLocalizer Create(Type resourceSource)
        {
            ArgumentNullException.ThrowIfNull(resourceSource);

            var context = resourceSource.FullName ?? resourceSource.Name;
            var options = _settingsProvider.GetOptionsFor(context);

            if (options.TranslationManager is not null || options.Configuration is not null || !string.IsNullOrEmpty(options.DefaultFile))
                return new TlumachStringLocalizer(options);

            const BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy;

            var prop = resourceSource.GetProperty("TranslationManager", flags);
            if (prop == null)
                throw new TlumachException("Could not obtain the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            object? manager = prop.GetValue(null);
            if (manager is null)
                throw new TlumachException("Could not obtain the value of the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            return new TlumachStringLocalizer((TranslationManager)manager);
        }

        /// <summary>
        /// Creates an instance of <see cref="TlumachStringLocalizer"/> from the default file, embedded into resources of the assembly that is calling this method.
        /// <para>The created localizer uses <seealso cref="CultureInfo.CurrentCulture"/> for a culture and <seealso cref="TextFormat.DotNet"/> text processing mode for texts with placeholders.
        /// An application can change either of these settings later by calling <see cref="TlumachStringLocalizer.WithCulture(CultureInfo)"/> or <see cref="TlumachStringLocalizer.WithTextProcessingMode(TextFormat)"/> method respectively.</para>
        /// </summary>
        /// <param name="baseName">The name of the default file.</param>
        /// <param name="location">Not used.</param>
        /// <returns>An instance of <see cref="TlumachStringLocalizer"/>.</returns>
        /// <exception cref="TlumachException">Thrown if the default file provided in <paramref name="baseName"/> was not found.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="baseName"/> is null or empty.</exception>
        public IStringLocalizer Create(string baseName, string location)
        {
            ArgumentNullException.ThrowIfNull(baseName);

            var context = string.IsNullOrEmpty(location) ? baseName : location + "." + baseName;
            var options = _settingsProvider.GetOptionsFor(context);

            if (options.TranslationManager is not null || options.Configuration is not null || !string.IsNullOrEmpty(options.DefaultFile))
                return new TlumachStringLocalizer(options);

            TranslationManager manager = new TranslationManager(new TranslationConfiguration(Assembly.GetCallingAssembly(), baseName, defaultFileLocale: null, TextFormat.DotNet));
            return new TlumachStringLocalizer(manager);
        }
    }
}
