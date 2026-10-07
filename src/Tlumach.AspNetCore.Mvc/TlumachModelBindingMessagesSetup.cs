// <copyright file="TlumachModelBindingMessagesSetup.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Replaces the accessors of <see cref="DefaultModelBindingMessageProvider"/> with lookups in Tlumach. A message that no translation contains is produced by the accessor
/// that was set before (MVC's English message). The values are passed both by name ("value", "field") and by position ({0}, {1} as in MVC's texts).
/// </summary>
internal sealed class TlumachModelBindingMessagesSetup : IConfigureOptions<MvcOptions>
{
    private readonly TlumachModelBindingOptions _options;
    private readonly IServiceProvider _services;

    public TlumachModelBindingMessagesSetup(TlumachModelBindingOptions options, IServiceProvider services)
    {
        _options = options;
        _services = services;
    }

    public void Configure(MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        DefaultModelBindingMessageProvider provider = options.ModelBindingMessageProvider;
        Messages m = new(_options.KeyPrefix, new Lazy<TemplateTranslator>(() => new TemplateTranslator(DefaultManager.Resolve(_services, _options.TranslationManager))));

        Func<string, string> missingBindRequired = provider.MissingBindRequiredValueAccessor;
        provider.SetMissingBindRequiredValueAccessor(field => m.Get("MissingBindRequiredValue", () => missingBindRequired(field), ("field", field)));

        Func<string> missingKeyOrValue = provider.MissingKeyOrValueAccessor;
        provider.SetMissingKeyOrValueAccessor(() => m.Get("MissingKeyOrValue", missingKeyOrValue));

        Func<string> missingRequestBody = provider.MissingRequestBodyRequiredValueAccessor;
        provider.SetMissingRequestBodyRequiredValueAccessor(() => m.Get("MissingRequestBodyRequiredValue", missingRequestBody));

        Func<string, string> valueMustNotBeNull = provider.ValueMustNotBeNullAccessor;
        provider.SetValueMustNotBeNullAccessor(value => m.Get("ValueMustNotBeNull", () => valueMustNotBeNull(value), ("value", value)));

        Func<string, string, string> attemptedValueIsInvalid = provider.AttemptedValueIsInvalidAccessor;
        provider.SetAttemptedValueIsInvalidAccessor((value, field) => m.Get("AttemptedValueIsInvalid", () => attemptedValueIsInvalid(value, field), ("value", value), ("field", field)));

        Func<string, string> nonPropertyAttemptedValueIsInvalid = provider.NonPropertyAttemptedValueIsInvalidAccessor;
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => m.Get("NonPropertyAttemptedValueIsInvalid", () => nonPropertyAttemptedValueIsInvalid(value), ("value", value)));

        Func<string, string> unknownValueIsInvalid = provider.UnknownValueIsInvalidAccessor;
        provider.SetUnknownValueIsInvalidAccessor(field => m.Get("UnknownValueIsInvalid", () => unknownValueIsInvalid(field), ("field", field)));

        Func<string> nonPropertyUnknownValueIsInvalid = provider.NonPropertyUnknownValueIsInvalidAccessor;
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => m.Get("NonPropertyUnknownValueIsInvalid", nonPropertyUnknownValueIsInvalid));

        Func<string, string> valueIsInvalid = provider.ValueIsInvalidAccessor;
        provider.SetValueIsInvalidAccessor(value => m.Get("ValueIsInvalid", () => valueIsInvalid(value), ("value", value)));

        Func<string, string> valueMustBeANumber = provider.ValueMustBeANumberAccessor;
        provider.SetValueMustBeANumberAccessor(field => m.Get("ValueMustBeANumber", () => valueMustBeANumber(field), ("field", field)));

        Func<string> nonPropertyValueMustBeANumber = provider.NonPropertyValueMustBeANumberAccessor;
        provider.SetNonPropertyValueMustBeANumberAccessor(() => m.Get("NonPropertyValueMustBeANumber", nonPropertyValueMustBeANumber));
    }

    private sealed class Messages
    {
        private readonly string _prefix;
        private readonly Lazy<TemplateTranslator> _translator;

        public Messages(string prefix, Lazy<TemplateTranslator> translator)
        {
            _prefix = prefix;
            _translator = translator;
        }

        public string Get(string name, Func<string> fallback, params (string Name, string Value)[] values)
        {
            TemplateArguments arguments = new();
            foreach ((string valueName, string value) in values)
            {
                arguments.AddPositional(value);
                arguments.AddNamed(valueName, value);
            }

            return _translator.Value.TryTranslate(_prefix + name, arguments, CultureInfo.CurrentCulture, out string text) ? text : fallback();
        }
    }
}
