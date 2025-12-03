using System.Globalization;
using System.Reflection;

using Microsoft.Extensions.Localization;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization
{
    public class TlumachStringLocalizer : IStringLocalizer
    {
        private readonly TranslationManager _manager;
        private TextFormat? _textProcessingMode;
        private CultureInfo _culture;

        public TlumachStringLocalizer(TranslationManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            _textProcessingMode = TextFormat.DotNet;
            _culture = CultureInfo.CurrentCulture;
        }

        public TlumachStringLocalizer(TlumachLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.TranslationManager is not null)
                _manager = options.TranslationManager;
            else
            if (options.Configuration is not null)
                _manager = new TranslationManager(options.Configuration);
            else
            if (!string.IsNullOrEmpty(options.DefaultFile))
                _manager = new TranslationManager(new TranslationConfiguration(options.Assembly ?? Assembly.GetCallingAssembly(), options.DefaultFile, options.DefaultFileLocale, options.TextProcessingMode ?? TextFormat.DotNet));
            else
                throw new ArgumentException("Options passed to TlumachStringLocalizer must have either TranslationMAnager, Configuration, or DefaultFile property set.", nameof(options));

            _textProcessingMode = options.TextProcessingMode;
            _culture = CultureInfo.CurrentCulture;
        }

        public LocalizedString this[string name]
        {
            get
            {
                string text;
                if (_manager.DefaultConfiguration is null)
                    return new LocalizedString(name, name, true);

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, _culture, out bool found);

                if (!found)
                    return new LocalizedString(name, name, true);

                if (!entry.ContainsPlaceholders)
                {
                    text = entry.Text ?? string.Empty;
                }
                else
                {
                    text = entry.ProcessTemplatedValue(_culture, _textProcessingMode ?? _manager.DefaultConfiguration.TextProcessingMode ?? TextFormat.DotNet, static (name, _) => name);
                }

                return new LocalizedString(name, text, false);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string text;
                if (_manager.DefaultConfiguration is null)
                    return new LocalizedString(name, name, true);

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, _culture, out bool found);

                if (!found)
                    return new LocalizedString(name, name, true);

                if (!entry.ContainsPlaceholders)
                {
                    text = entry.Text ?? string.Empty;
                }
                else
                {
                    text = entry.ProcessTemplatedValue(_culture, _textProcessingMode ?? _manager.DefaultConfiguration.TextProcessingMode ?? TextFormat.DotNet, arguments);
                }

                return new LocalizedString(name, text, false);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            Translation? translation = _manager.GetTranslation(_culture);

            List<LocalizedString> result = [];
            while (true)
            {
                // retrieve the keys of the translation if one exists.
                if (translation is not null)
                {
                    foreach (var key in translation.Keys)
                    {
                        result.Add(new LocalizedString(key, translation[key].Text ?? string.Empty));
                    }
                }

                if (includeParentCultures)
                {
                    var parentCulture = _culture.Parent;
                    translation = _manager.GetTranslation(parentCulture);

                    // If we got to the invariant culture, there will be no more parent and we may break;
                    if (parentCulture.Name.Length == 0)
                        break;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Switches the culture, used by the localizer object when retrieving localized text.
        /// </summary>
        /// <param name="culture">The new culture to use.</param>
        /// <returns>The object, whose method was called.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            _culture = culture;
            return this;
        }

        /// <summary>
        /// Switches the text processing mode, used by the localizer object when processing text which contains placeholders.
        /// </summary>
        /// <param name="textProcessingMode">The mode to use.</param>
        /// <returns>The object, whose method was called.</returns>
        public IStringLocalizer WithTextProcessingMode(TextFormat textProcessingMode)
        {
            _textProcessingMode = textProcessingMode;
            return this;
        }
    }

    public sealed class TlumachStringLocalizer<T> : IStringLocalizer<T>
    {
        private readonly IStringLocalizer _inner;

        public TlumachStringLocalizer(IStringLocalizerFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            _inner = factory.Create(typeof(T));
        }

        public LocalizedString this[string name]
            => _inner[name];

        public LocalizedString this[string name, params object[] arguments]
            => _inner[name, arguments];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => _inner.GetAllStrings(includeParentCultures);

        public IStringLocalizer WithCulture(CultureInfo culture)
            => ((TlumachStringLocalizer)_inner).WithCulture(culture);
    }
}
