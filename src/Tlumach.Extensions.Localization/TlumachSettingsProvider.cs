using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tlumach.Extensions.Localization
{

    public sealed class TlumachSettingsProvider : ITlumachSettingsProvider
    {
        private readonly Dictionary<string, TlumachLocalizationOptions> _perContext =
            new(StringComparer.Ordinal);

        private readonly TlumachLocalizationOptions _defaultOptions;

        public TlumachSettingsProvider(TlumachLocalizationOptions defaultOptions)
        {
            _defaultOptions = defaultOptions;
        }

        public void AddContext(string context, TlumachLocalizationOptions options)
            => _perContext[context] = options;

        public TlumachLocalizationOptions GetOptionsFor(string context)
            => _perContext.TryGetValue(context, out var opt) ? opt : _defaultOptions;
    }
}
