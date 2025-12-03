using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Adds the classes necessary for dependency injection.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Registers the classes so that Tlumach can be used via dependency injection.
        /// </summary>
        /// <param name="services">The services to which Tlumach is added.</param>
        /// <param name="configureDefault">A configuration callback for default/global options.</param>
        /// <param name="configurePerContext">A configuration callback for per-context options.</param>
        /// <returns>The value of <paramref name="services"/>.</returns>
        public static IServiceCollection AddTlumachLocalization(
            this IServiceCollection services,
            Action<TlumachLocalizationOptions> configureDefault,
            Action<TlumachSettingsProvider>? configurePerContext = null)
        {
            ArgumentNullException.ThrowIfNull(configureDefault);

            var defaultOptions = new TlumachLocalizationOptions();
            configureDefault(defaultOptions);

            var provider = new TlumachSettingsProvider(defaultOptions);
            configurePerContext?.Invoke(provider);

            services.AddSingleton<ITlumachSettingsProvider>(provider);
            services.AddSingleton<IStringLocalizerFactory, TlumachStringLocalizerFactory>();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(TlumachStringLocalizer<>));
            services.AddTransient<IStringLocalizer>(sp =>
            {
                var factory = sp.GetRequiredService<IStringLocalizerFactory>();
                return factory.Create(string.Empty, string.Empty);
            });

            return services;
        }
    }
}
