AlliedBits.Tlumach.HandlebarsNet lets [Handlebars.Net](https://www.nuget.org/packages/Handlebars.Net) templates use translations of [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach), e.g. to render localized emails, reports, and documents.

* `handlebars.RegisterTlumach(Strings.TranslationManager)` registers the `t` and `t_html` helpers.
* `{{t "Email.Greeting" name=customer.Name}}` returns a translation with its placeholders filled: hash arguments for named placeholders, positional values for `{0}`, and ICU `plural` and `select`. A translation unit passed in the model can be used instead of a key.
* The culture comes from the `culture=` argument, then from the `@culture` data of the render (`template(model, new { culture })`), then from the translation manager, so emails for many users can be rendered concurrently, each in its own language.
* `t` is escaped in `{{ }}` and not in `{{{ }}}`, as any Handlebars value. `t_html` treats the translation as trusted HTML and always encodes the values it inserts, like a Handlebars helper that returns a `SafeString`: `{{t_html ...}}` and `{{{t_html ...}}}` render the same. Use it directly, not as a `(t_html ...)` subexpression, which an outer `{{ }}` would escape.
* A missing key returns the key by default, or an empty string, or throws, or is handled by a callback.

Requires Handlebars.Net 2 and .NET 9 or later. Indexed `{0}` placeholders need the `DotNet` text processing mode of the configuration and ICU `plural` and `select` need `Arb`, so one configuration uses one of them. Handlebars.Net compiles templates at run time and is not suitable for NativeAOT. See the "Template Engines" topic in the [documentation](https://alliedbits.com/tlumach).
