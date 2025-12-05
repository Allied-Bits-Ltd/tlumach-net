using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// This class keeps settings for various localizer instances bound to specific contents.
    /// </summary>
    public sealed class TlumachSettingsProvider : ITlumachSettingsProvider
    {
        private readonly Dictionary<string, TlumachLocalizationOptions> _perContext =
            new(StringComparer.Ordinal);

        private readonly TlumachLocalizationOptions _defaultOptions;

        internal TlumachSettingsProvider(TlumachLocalizationOptions defaultOptions)
        {
            _defaultOptions = defaultOptions;
        }

        /// <summary>
        /// Use this method to add localizer options for the specified context.
        /// </summary>
        /// <param name="context">The context to add the options for.</param>
        /// <param name="options">The options to add.</param>
        public void AddContext(string context, TlumachLocalizationOptions options)
            => _perContext[context] = options;

        /// <summary>
        /// Returns the options set for the given context.
        /// </summary>
        /// <param name="context">The context to retrieve the options for.</param>
        /// <returns>An instance of options or the default value if the options specific for the context were not found.</returns>
        public TlumachLocalizationOptions GetOptionsFor(string context)
            => _perContext.TryGetValue(context, out var opt) ? opt : _defaultOptions;
    }
}
