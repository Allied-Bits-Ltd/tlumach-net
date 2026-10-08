// <copyright file="TlumachStringLocalizerFactory.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using Microsoft.Extensions.Localization;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Creates instances of <see cref="TlumachStringLocalizer"/>.
    /// <para>The localizers are created anew on every call (<c>IStringLocalizer&lt;T&gt;</c> is transient), but the translation managers that the factory creates are cached:
    /// those created from options by what the options describe (the <c>Configuration</c> instance, or the <c>DefaultFile</c> with its assembly and locale, together with the text processing mode),
    /// not by the options instance, and those created for a base name without options by the base name and the assembly (the one that the location names, or the calling one). So the localizers do not load translations again even when
    /// the settings provider returns new options objects. The options are still requested for every localizer, so a provider can switch a context to another manager at runtime.</para>
    /// </summary>
    public sealed class TlumachStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ITlumachSettingsProvider _settingsProvider;
        private readonly ConcurrentDictionary<ManagerSourceKey, Lazy<TranslationManager>> _bySource = new();
        private readonly ConcurrentDictionary<(Assembly Assembly, string BaseName), Lazy<TranslationManager>> _byBaseName = new();
        private readonly LocationAssemblies _locationAssemblies = new();

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
        /// <remarks>When the options do not carry a translation manager, this method falls back to reading the static
        /// <c>TranslationManager</c> property of <paramref name="resourceSource"/> through reflection. In a trimmed or
        /// NativeAOT application that property may have been removed. Supply the manager through
        /// <see cref="TlumachLocalizationOptions.TranslationManager"/>, <see cref="TlumachLocalizationOptions.Configuration"/>,
        /// or <see cref="TlumachLocalizationOptions.DefaultFile"/> instead — those paths are checked first and use no reflection at all.</remarks>
        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2067:UnrecognizedReflectionPattern",
            Justification = "IStringLocalizerFactory.Create(Type) and IHtmlLocalizerFactory.Create(Type) carry no DynamicallyAccessedMembers annotation, so an " +
                            "implementation cannot add one either (IL2092/IL2046). The reflection fallback is therefore " +
                            "documented as unsupported under trimming; trimmed applications are directed to the " +
                            "TlumachLocalizationOptions properties, which are checked before this code path is reached.")]
        public IStringLocalizer Create(Type resourceSource)
        {
            ArgumentNullException.ThrowIfNull(resourceSource);

            var context = resourceSource.FullName ?? resourceSource.Name;
            var options = _settingsProvider.GetOptionsFor(context);

            if (TranslationManagerResolver.HasManagerSource(options))
                return CreateFromOptions(options);

            return new TlumachStringLocalizer(TranslationManagerResolver.FromGeneratedClass(resourceSource));
        }

        /// <summary>
        /// Creates an instance of <see cref="TlumachStringLocalizer"/> from the options for the context or, when they set none of <c>TranslationManager</c>, <c>Configuration</c>, and <c>DefaultFile</c>,
        /// from the default file named <paramref name="baseName"/>, embedded into resources of the assembly that <paramref name="location"/> names or, when <paramref name="location"/> is empty or is not
        /// the name of a loadable assembly, of the assembly that is calling this method.
        /// <para>As for <c>ResourceManagerStringLocalizerFactory</c>, <paramref name="location"/> is the name of the assembly that holds the resources, so the file is found also when framework code
        /// (such as <c>HtmlLocalizerFactory</c> of MVC) calls this method on behalf of the application. A location that is not an assembly name does not cause an error, because it may be meant
        /// only as a part of the options context.</para>
        /// <para>A <see cref="TlumachLocalizationOptions.DefaultFile"/> of the options is loaded from <see cref="TlumachLocalizationOptions.Assembly"/> or, when that is not set, from the entry assembly (see <see cref="TranslationManagerResolver.GetDefaultFileAssembly"/>);
        /// <paramref name="location"/> does not change that.</para>
        /// <para>The localizer created from the default file named <paramref name="baseName"/> uses <seealso cref="CultureInfo.CurrentCulture"/> for a culture and <seealso cref="TextFormat.DotNet"/> text processing mode for texts with placeholders.
        /// An application can change either of these settings later by calling <see cref="TlumachStringLocalizer.WithCulture(CultureInfo)"/> or <see cref="TlumachStringLocalizer.WithTextProcessingMode(TextFormat)"/> method respectively.</para>
        /// </summary>
        /// <param name="baseName">The name of the default file.</param>
        /// <param name="location">The name (simple or full) of the assembly whose resources contain the default file named <paramref name="baseName"/>, or an empty string to use the calling assembly.
        /// When not empty, it is also a part of the options context, <c>location.baseName</c>.</param>
        /// <returns>An instance of <see cref="TlumachStringLocalizer"/>.</returns>
        /// <exception cref="TlumachException">Thrown if the default file provided in <paramref name="baseName"/> was not found.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="baseName"/> is null or empty.</exception>
        [MethodImpl(MethodImplOptions.NoInlining)] // Assembly.GetCallingAssembly() must see the caller of this method.
        public IStringLocalizer Create(string baseName, string location)
        {
            ArgumentNullException.ThrowIfNull(baseName);

            var context = string.IsNullOrEmpty(location) ? baseName : location + "." + baseName;
            var options = _settingsProvider.GetOptionsFor(context);

            if (TranslationManagerResolver.HasManagerSource(options))
                return CreateFromOptions(options);

            TranslationManager manager = GetOrCreate(
                _byBaseName,
                (Assembly: _locationAssemblies.Find(location) ?? Assembly.GetCallingAssembly(), BaseName: baseName),
                static key => new Lazy<TranslationManager>(() => new TranslationManager(new TranslationConfiguration(key.Assembly, key.BaseName, defaultFileLocale: null, TextFormat.DotNet))));
            return new TlumachStringLocalizer(manager);
        }

        /// <summary>
        /// Returns the cached manager. The <see cref="Lazy{T}"/> makes sure that only one manager is created per key; if its creation throws, the failed
        /// <see cref="Lazy{T}"/> is removed, so that a later call retries instead of rethrowing the cached exception forever.
        /// </summary>
        private static TranslationManager GetOrCreate<TKey>(ConcurrentDictionary<TKey, Lazy<TranslationManager>> cache, TKey key, Func<TKey, Lazy<TranslationManager>> create)
            where TKey : notnull
        {
            Lazy<TranslationManager> lazy = cache.GetOrAdd(key, create);
            try
            {
                return lazy.Value;
            }
            catch
            {
                cache.TryRemove(new KeyValuePair<TKey, Lazy<TranslationManager>>(key, lazy));
                throw;
            }
        }

        // A manager set in the options is used as it is. Equal keys describe the same manager, so the options of the first caller create it for all of them.
        private TlumachStringLocalizer CreateFromOptions(TlumachLocalizationOptions options)
        {
            if (options.TranslationManager is not null)
                return new TlumachStringLocalizer(options.TranslationManager, options.TextProcessingMode);

            TranslationManager manager = GetOrCreate(
                _bySource,
                ManagerSourceKey.From(options),
                _ => new Lazy<TranslationManager>(() => TranslationManagerResolver.CreateFromOptions(options)));
            return new TlumachStringLocalizer(manager, options.TextProcessingMode);
        }
    }
}
