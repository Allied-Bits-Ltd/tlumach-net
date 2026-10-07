AlliedBits.Tlumach.Fluid lets [Fluid](https://www.nuget.org/packages/Fluid.Core) (Liquid) templates use translations of [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach), e.g. to render localized emails, reports, and documents.

* `templateOptions.AddTlumach(Strings.TranslationManager)` adds the `t` and `t_html` filters.
* `{{ "Email.Greeting" | t: name: customer.Name }}` returns a translation with its placeholders filled: named values, positional values for `{0}`, and ICU `plural` and `select`. A translation unit passed in the model can be used instead of a key.
* The culture comes from the `culture:` argument, then from `TemplateContext.CultureInfo`, then from the translation manager, so emails for many users can be rendered concurrently, each in its own language.
* `t` returns text that Fluid encodes when the template is rendered with an HTML encoder, so nothing is encoded twice. `t_html` treats the translation as trusted HTML and encodes only the values; values passed through `raw` stay raw.
* A missing key returns the key by default, or an empty string, or throws, or is handled by a callback.

Requires Fluid 2 (2.40 or later) and .NET 9 or later. Indexed `{0}` placeholders need the `DotNet` text processing mode of the configuration and ICU `plural` and `select` need `Arb`, so one configuration uses one of them. See the "Template Engines" topic in the [documentation](https://alliedbits.com/tlumach).
