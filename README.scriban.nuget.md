AlliedBits.Tlumach.Scriban lets [Scriban](https://www.nuget.org/packages/Scriban) templates use translations of [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach), e.g. to render localized emails, reports, and documents.

* `scriptObject.ImportTlumach(Strings.TranslationManager)` adds the `t` and `t_html` functions.
* `{{ t "Email.Greeting" name: customer.name }}` returns a translation with its placeholders filled: named values, positional values for `{0}`, and ICU `plural` and `select`. A translation unit passed in the model can be used instead of a key.
* The culture comes from the `culture:` argument, then from `TemplateContext.PushCulture`, then from the translation manager, so emails for many users can be rendered concurrently, each in its own language.
* `t` returns text, which Scriban does not encode; set `HtmlEncode` to encode it. `t_html` treats the translation as trusted HTML and encodes only the values.
* A missing key returns the key by default, or an empty string, or throws, or is handled by a callback.

Requires Scriban 7.2.2 or later (7.x) and .NET 9 or later. Indexed `{0}` placeholders need the `DotNet` text processing mode of the configuration and ICU `plural` and `select` need `Arb`, so one configuration uses one of them. See the "Template Engines" topic in the [documentation](https://alliedbits.com/tlumach).
