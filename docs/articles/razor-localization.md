# Localization of MVC and Razor Pages

## Overview

Tlumach localizes ASP.NET Core MVC (controllers and views) and Razor Pages applications. The texts of views and pages come from Tlumach translations, and the language of every request is the language of the user, so one process serves many users, each in their own language. The support covers:

* `IViewLocalizer`, `IHtmlLocalizer`, and `IHtmlLocalizer<T>`, which return HTML and take the texts of a view from the keys that start with the path of the view;
* the `tlumach-key` and `tlumach-unit` attributes, the `<tlumach-text>` element, and the `Html.Tlumach` helper, which show translation keys and generated translation units without code in the view;
* the `<tlumach-culture-selector>` tag helper, which lets users choose the language;
* the messages of model binding (e.g. "The value 'abc' is not valid for Age.") and the display names of model properties, which come from Tlumach translations, too.

This article is the reference. For a step-by-step introduction, read [Getting Started for integration with ASP.NET Core MVC](getting-started-aspnetcore-mvc.md) or [Getting Started for integration with Razor Pages](getting-started-razor-pages.md). The samples are `samples/Tlumach.Sample.Mvc` and `samples/Tlumach.Sample.RazorPages` in the [repository](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples); the code below is taken from them.

## 1. Packages and Assemblies

All assemblies are in the `AlliedBits.Tlumach` package (.NET 9 and .NET 10) and ship no JavaScript or CSS files:

| Assembly | Contents |
|---|---|
| `Tlumach.Web` | <xref:Tlumach.Web.TlumachCultureOptions> (the supported cultures, the default culture, and the culture endpoint) and `AddTlumachCultures`. It does not depend on ASP.NET Core, so Blazor WebAssembly clients can use it. |
| `Tlumach.AspNetCore` | The hosting helpers: `UseTlumachRequestLocalization`, which sets the culture of each request, and `MapTlumachCultureEndpoint`, which stores the chosen culture in a cookie. |
| `Tlumach.AspNetCore.Mvc` | Everything of this article: the HTML and view localizers, the tag helpers, `Html.Tlumach`, the culture selector, the model binding messages, and the display names. |

None of them depends on Blazor. An application that uses both MVC and Blazor registers the cultures with either `AddTlumachCultures` or `AddTlumachBlazor`, or calls both, in any order; see [Getting Started for integration with Blazor](getting-started-blazor.md). The translations are normally kept in a project that is processed by [Generator](generator.md), as in the other getting-started articles, but a translation manager that is created in any other way works just as well.

## 2. Registration

An MVC application (`samples/Tlumach.Sample.Mvc/Program.cs`):

```csharp
using System.Globalization;

using Tlumach.AspNetCore;
using Tlumach.AspNetCore.Mvc;
using Tlumach.Extensions.Localization;
using Tlumach.Sample.Mvc.Translation;
using Tlumach.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachCultures(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de"), CultureInfo.GetCultureInfo("uk")];
    options.DefaultCulture = CultureInfo.GetCultureInfo("en");
});

builder.Services.AddControllersWithViews()
    .AddTlumachViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddTlumachModelBindingMessages()
    .AddTlumachDisplayNames();

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseStaticFiles();
app.MapTlumachCultureEndpoint();
app.MapDefaultControllerRoute();

await app.RunAsync().ConfigureAwait(false);
```

A Razor Pages application (`samples/Tlumach.Sample.RazorPages/Program.cs`) differs in two lines only; it has no controllers and does not call `AddControllersWithViews()`:

```csharp
builder.Services.AddRazorPages()
    .AddTlumachViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddTlumachModelBindingMessages()
    .AddTlumachDisplayNames();

app.MapRazorPages();   // instead of MapDefaultControllerRoute()
```

What each call does:

* `AddTlumachLocalization` is **required**. It provides the translation managers to all the pieces below; without it, the first use of a localizer throws an `InvalidOperationException` that names this call. See [Dependency Injection](di.md) for its options.
* `AddTlumachCultures` registers the supported cultures. Set `SupportedCultures`: `UseTlumachRequestLocalization` honors only these cultures, and the culture selector offers them. `UseTlumachRequestLocalization`, `MapTlumachCultureEndpoint`, and the culture selector need it; the localizers, tag helpers, model binding messages, and display names do not.
* `AddTlumachViewLocalization()` registers `IHtmlLocalizerFactory`, `IHtmlLocalizer<T>`, and `IViewLocalizer`. It replaces the registrations of `AddViewLocalization()`, so it does not matter whether `AddViewLocalization()` is called before or after it. It also adds the `LanguageViewLocationExpander` that finds culture-specific files (`Index.de.cshtml`); see below. The tag helpers and `Html.Tlumach` need this call, too. A repeated call (for example, after both `AddControllersWithViews()` and `AddRazorPages()`) configures the same options.
* `AddDataAnnotationsLocalization()` is the call of ASP.NET Core that localizes the messages of validation attributes through `IStringLocalizer`; see [Localization of Data Annotations](data-annotations.md).
* `AddTlumachModelBindingMessages()` and `AddTlumachDisplayNames()` are optional (sections 8 and 9).

The order of the calls on the builder does not matter. `UseTlumachRequestLocalization()` must run before the endpoints, and `MapTlumachCultureEndpoint()` is needed only when the culture selector is used.

To enable the tag helpers, add them to `_ViewImports.cshtml` (`Views/_ViewImports.cshtml` or `Pages/_ViewImports.cshtml`):

```razor
@using Tlumach.AspNetCore.Mvc
@using Microsoft.AspNetCore.Mvc.Localization
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Tlumach.AspNetCore.Mvc
```

## 3. View Keys

`IViewLocalizer` (and the tag helpers) take the texts of a view from the keys that start with the path of the view. The prefix of a view is its path without the leading slash and the extension, with slashes replaced by dots, followed by a dot:

| File | Prefix of its keys |
|---|---|
| `/Views/Home/Index.cshtml` | `Views.Home.Index.` |
| `/Views/Account/Register.cshtml` | `Views.Account.Register.` |
| `/Pages/Contact.cshtml` | `Pages.Contact.` |
| `/Pages/Movies/Create.cshtml` | `Pages.Movies.Create.` |
| `/Areas/Admin/Views/Users/List.cshtml` | `Areas.Admin.Views.Users.List.` |
| `/Areas/Admin/Pages/Users/List.cshtml` | `Areas.Admin.Pages.Users.List.` |
| `/Views/Shared/_Status.cshtml` (a partial view) | `Views.Shared._Status.` |
| `/Views/Shared/_Layout.cshtml` (a layout) | `Views.Shared._Layout.` |
| `/Pages/Shared/_Layout.cshtml` (a layout) | `Pages.Shared._Layout.` |

A partial view and a layout have their own prefix, not that of the view that uses them. No application name is added to the prefix.

A culture-specific view file has a prefix of its own, too, as the culture is a part of its file name: `/Views/Home/Index.de.cshtml` (found by `LanguageViewLocationExpander` for a German request) has the prefix `Views.Home.Index.de.`, so its `IViewLocalizer` looks up `Views.Home.Index.de.*` and then the shared keys, not `Views.Home.Index.*`. This mirrors stock MVC.

The lookup of `@L["Title"]` takes two steps: the key with the prefix of the file (`Views.Home.Index.Title`), then the shared key without a prefix (`Title`), which lets views share texts such as `AppName`. When neither exists, the result is the encoded key (section 4). The tag helpers add a middle step; see section 5.

The translation file of the sample (ARB, `ArbNoEscaping`), where the nested objects produce the dotted keys:

```json
{
    "@@locale": "en",
    "AppName": "Tlumach MVC sample",
    "Footer": "Translated with Tlumach · {year}",
    "Views": {
        "Home": {
            "Index": {
                "Title": "Home",
                "Intro": "Hello, <b>{name}</b>! This page is translated with IViewLocalizer.",
                "Items": "{count, plural, =0{Your cart is empty} =1{You have # item} other{You have # items}}"
            }
        }
    }
}
```

and the same in INI, where a section is a group:

```ini
AppName=Tlumach MVC sample

[Views.Home.Index]
Title=Home

[Views.Account.Register]
Title=Register
```

A view that uses the keys:

```razor
@inject IViewLocalizer L
@{ ViewData["Title"] = L.GetString("Title").Value; }
<h1>@L["Title"]</h1>
<p>@L["Intro", "Ada"]</p>
<p>@L["Items", 3]</p>
```

`L["Title"]` returns a `LocalizedHtmlString`, which Razor writes as HTML. `L.GetString("Title")` returns a `LocalizedString`, which is plain text (use it for `ViewData["Title"]`, which Razor encodes when the layout writes it). Both look up the key in the same two steps.

### Options

<xref:Tlumach.AspNetCore.Mvc.TlumachViewLocalizationOptions> has two properties:

* <xref:Tlumach.AspNetCore.Mvc.TlumachViewLocalizationOptions.ViewKeyPrefix> is the function that returns the prefix of a view from its path. To add something to the default prefix, call <xref:Tlumach.AspNetCore.Mvc.TlumachViewLocalizationOptions.DefaultViewKeyPrefix(System.String)> from it; a function that returns `null` or an empty string disables the prefix, so that all keys are shared keys:

  ```csharp
  .AddTlumachViewLocalization(options => options.ViewKeyPrefix = path => "Site." + TlumachViewLocalizationOptions.DefaultViewKeyPrefix(path));
  ```

* `ViewLocationExpanderFormat` is the format of culture-specific view files found by `LanguageViewLocationExpander`: `Index.de.cshtml` with the default `Suffix`; `null` does not add the expander.

### A translation file per view

The prefix without the trailing dot (`Views.Home.Index`) is also the context, for which `AddTlumachLocalization` asks for options. An application can route a view to a translation manager of its own:

```csharp
builder.Services.AddTlumachLocalization(
    options => options.TranslationManager = Strings.TranslationManager,
    provider => provider.AddContext("Views.Home.Index", new TlumachLocalizationOptions { TranslationManager = HomeIndexStrings.TranslationManager }));
```

The view then looks up its keys (with the prefix first and without it second) in that manager only; the shared keys of the default manager are not available to it. A view without options of its own uses the default options. Managers are created once per options instance, not per view or per request.

A tag helper inside a `@section` of a view runs while the layout is the executing file, so it uses the manager chosen for the **layout's** context (`Views.Shared._Layout`); its keys are still looked up with the prefix of the view as the second tier (section 5), but in the manager of the layout. `@inject IViewLocalizer` in the same section is bound to the view and uses the **view's** manager. If a view has a manager of its own, use `IViewLocalizer` for the texts of its sections, or keep their keys in the manager of the layout.

## 4. Encoding Rules

The rules are the same for the HTML localizers, the view localizer, the tag helpers, and `Html.Tlumach`:

* **The translation is trusted HTML.** Markup in a translation (`<b>`, `<a>`) reaches the page as it is. Translation files must come from trusted sources (see the security notes below).
* **Placeholder values are HTML-encoded.** `@L["Intro", "<script>"]` shows `&lt;script&gt;` in the text of the translation `Hello, <b>{name}</b>!`.
* **`IHtmlContent` values are inserted as they are**, like the arguments of the stock `HtmlLocalizer`: `@L["Intro", new HtmlString("<i>Ada</i>")]`.
* **Numbers, dates, and times are formatted** for the culture of the request and are not encoded. The ICU placeholders (`plural`, `select`, `date`, ...) work, too; see [Templates and Placeholders](placeholders.md). All Tlumach placeholder syntaxes work (`{0}`, `{name}`), subject to the text processing mode of the translation (the `Arb` modes reject `{0}`).
* **Named values** are passed as a single `IReadOnlyDictionary<string, object?>` argument: `@L["Intro", new Dictionary<string, object?> { ["name"] = Model.Name }]`. Any other kind of argument is positional, and a dictionary of another type (e.g. `IDictionary<string, object>` only) is treated as **one** positional value. A named placeholder without a named value takes the positional value at its position.
* **`TranslationManager.WebEncodeValues` is ignored** for HTML: the localizers read the raw text of an entry, so the result is encoded exactly once whether the property is on or off. (`GetString` returns plain text and keeps the behavior of `IStringLocalizer`, including `WebEncodeValues`.)
* **Braces of `LocalizedHtmlString.Value` are doubled.** MVC treats `Value` as a composite format string and `WriteTo` formats it, so the localizer doubles `{` and `}` in the HTML that it produces; `WriteTo` writes the HTML exactly. Code that reads `Value` of a result should expect the doubled braces; to get the plain text, use `GetString`.
* **A missing key** gives the encoded key as the text and `IsResourceNotFound = true`.
* The culture is `CultureInfo.CurrentCulture` at the moment of the call, which `UseTlumachRequestLocalization` sets for each request, so a localizer that a container creates once follows the culture of every request.

## 5. Tag Helpers

The tag helpers show a translation without code in the view. Enable them in `_ViewImports.cshtml` (see above).

```razor
<h1 tlumach-key="Title"></h1>                                         @* the content is replaced by the translation *@
<p tlumach-key="Intro" tlumach-arg-name="@Model.Name"></p>             @* a named value *@
<p tlumach-key="Greeting" tlumach-args="@(new object?[] { Model.Name, 3 })"></p>   @* positional values *@
<p tlumach-unit="Strings.Welcome"></p>                                @* a generated translation unit *@
<p tlumach-key="Title" tlumach-culture="de"></p>                      @* a fixed culture *@
<a asp-controller="Home" asp-action="Index" tlumach-key="Views.Home.Index.Title"></a>
<tlumach-text key="Intro" arg-name="@Model.Name" />                  @* no wrapper element *@
```

* `tlumach-key` takes a key; `tlumach-unit` takes a translation unit (`BaseTranslationUnit`), e.g. `Strings.Welcome` of a class created by [Generator](generator.md). Set one of them, not both and not none (otherwise an `InvalidOperationException` is thrown). The element, its other attributes, and any attribute tag helpers (`asp-controller`) are kept; the `tlumach-*` attributes are removed and the content of the element is replaced.
* `tlumach-arg-{name}` sets a named value and `tlumach-args` sets the positional values. **The values are C# expressions**: `@Model.Name`, `@("text")`, `@(3)`. A bare word such as `tlumach-arg-name="Ada"` is not an expression and does not compile; write `tlumach-arg-name="@("Ada")"`.
* `tlumach-culture` is the **name** of a culture (a string, `de` or `de-DE`). For a `CultureInfo`, use its name: `tlumach-culture="@culture.Name"`. An invalid name throws a `CultureNotFoundException`. Without it, the culture of the request is used. The name is passed to `CultureInfo.GetCultureInfo`, which keeps every culture it creates in a cache for the life of the process, so pass only values that the developer supplies (a literal or a name from the supported cultures), never raw request data (a query string value, a header, a form field), which would let a client grow that cache without limit. The same holds for the `culture` attribute of `<tlumach-text>`.
* `<tlumach-text>` is the element form; it takes the attributes `key`, `unit`, `args`, `arg-{name}`, and `culture` and renders no wrapper element.
* The values are encoded, the translation is trusted HTML, and `WebEncodeValues` is ignored, as in section 4.

### Where keys are looked up

A key of a tag helper is looked up in **three steps**: with the prefix of the current file, then with the prefix of the main view, then as a shared key. The middle step exists because ASP.NET Core runs the `@section` bodies of a view while the layout is the executing file, so a tag helper in a section of `Views/Home/Index.cshtml` would otherwise look for `Views.Shared._Layout.` keys and never find the keys of its own view. The side effect is that a tag helper in a layout also finds a key of the current view when the layout has no such key. `IViewLocalizer` does not do that: it looks up its own file and then the shared key, which is correct inside sections, too, because it is bound when the page starts.

## 6. Translation Units in Views

`Html.Tlumach` renders generated translation units. It replaces `@Html.Raw(...)`, which is what the advice for `WebEncodeValues` used to be:

```razor
<p>@Html.Tlumach(Strings.Welcome)</p>
<p>@Html.Tlumach(Strings.Greeting, Model.Name, 3)</p>
<p>@Html.Tlumach(Strings.Greeting, new Dictionary<string, object?> { ["name"] = Model.Name })</p>
```

`Html.Tlumach(unit)`, `Html.Tlumach(unit, params object?[] arguments)`, and `Html.Tlumach(unit, IReadOnlyDictionary<string, object?> values)` follow the encoding rules of section 4 and use the culture of the request. The unit renders once, as HTML, whether `WebEncodeValues` is on or off. Plain `@Strings.Welcome` is correct only when `WebEncodeValues` is off and the text contains no markup.

A placeholder without a value takes the value that is cached in the unit or is provided by its `OnPlaceholderValueNeeded` event. Generated units are static singletons, so these values are **process-wide**: they are shared by all requests. Pass per-user values as arguments.

## 7. Culture Selector

`<tlumach-culture-selector />` lets the user choose one of `TlumachCultureOptions.SupportedCultures`:

```razor
<tlumach-culture-selector class="lang" />
<tlumach-culture-selector button-text="✓" select-class="form-select" button-class="btn btn-primary" />
```

It renders a form that works without JavaScript:

```html
<form method="get" action="/tlumach/culture" class="lang">
  <input type="hidden" name="redirectUri" value="/Account/Register?returnUrl=1" />
  <select name="culture"><option value="de" selected="selected">Deutsch</option><option value="en">English</option>...</select>
  <button type="submit">OK</button>
</form>
```

* The action is the culture endpoint (`MapTlumachCultureEndpoint`, `/tlumach/culture` by default, preceded by the `PathBase`). The `GET` request stores the culture in the cookie that `UseTlumachRequestLocalization` reads and redirects to the `redirectUri`, which is the current path with the query string. The endpoint accepts only local paths and supported cultures; see [Getting Started for integration with Blazor](getting-started-blazor.md) for its security notes.
* The selected option is the culture of the request (`CurrentUICulture`, as in Blazor), mapped to a supported culture.
* Attributes: `button-text` (the default is "OK"; it is not localized, so pass a translated text or a neutral symbol), `display-name` (a `Func<CultureInfo, string>`; the default shows the `NativeName`), `select-class`, and `button-class`. All other attributes (`class`, `id`) are copied to the `form` element.
* Nothing is rendered when `SupportedCultures` is empty. The tag helper throws an `InvalidOperationException` when the cultures were not registered with `AddTlumachCultures` or `AddTlumachBlazor`.
* No inline script is rendered, so a strict Content-Security-Policy works. To submit the form when the selection changes, add a script file of your own, and hide the button with CSS if you want:

  ```javascript
  // wwwroot/culture-selector.js
  document.querySelectorAll('form select[name="culture"]').forEach(function (select) {
      select.addEventListener('change', function () { select.form.submit(); });
  });
  ```

  ```razor
  <script src="~/culture-selector.js"></script>
  ```

## 8. Model Binding Messages

`AddTlumachModelBindingMessages()` replaces the 11 messages of `DefaultModelBindingMessageProvider`, e.g. "The value 'abc' is not valid for Age." Their keys are `ModelBinding.` followed by the name of the message:

| Key | Named values | English text of MVC |
|---|---|---|
| `ModelBinding.MissingBindRequiredValue` | `field` | A value for the '{0}' parameter or property was not provided. |
| `ModelBinding.MissingKeyOrValue` | - | A value is required. |
| `ModelBinding.MissingRequestBodyRequiredValue` | - | A non-empty request body is required. |
| `ModelBinding.ValueMustNotBeNull` | `value` | The value '{0}' is invalid. |
| `ModelBinding.AttemptedValueIsInvalid` | `value`, `field` | The value '{0}' is not valid for {1}. |
| `ModelBinding.NonPropertyAttemptedValueIsInvalid` | `value` | The value '{0}' is not valid. |
| `ModelBinding.UnknownValueIsInvalid` | `field` | The supplied value is invalid for {0}. |
| `ModelBinding.NonPropertyUnknownValueIsInvalid` | - | The supplied value is invalid. |
| `ModelBinding.ValueIsInvalid` | `value` | The value '{0}' is invalid. |
| `ModelBinding.ValueMustBeANumber` | `field` | The field {0} must be a number. |
| `ModelBinding.NonPropertyValueMustBeANumber` | - | The field must be a number. |

The translations of the sample (ARB):

```json
"ModelBinding": {
    "AttemptedValueIsInvalid": "The value '{value}' is not valid for {field}.",
    "ValueMustBeANumber": "The field {field} must be a number.",
    "ValueMustNotBeNull": "A value is required."
}
```

* The attempted value and the name of the field are available by name (`{value}`, `{field}`) and, as in the texts of MVC, by position (`{0}`, `{1}`: for `AttemptedValueIsInvalid`, the value first and the field second). **The ARB formats (`Arb`, `ArbNoEscaping`) reject placeholders that start with a digit**, so `{0}` works only with the .NET text format; the named placeholders work with both. A named placeholder without a named value is filled by position. Use `ArbNoEscaping` when the texts contain apostrophes (`'{value}'`).
* The text is plain text, not HTML; the validation tag helpers and `asp-validation-summary` encode it.
* A message that has no translation (a missing key) falls back to MVC's English text. A failed attempt to find the translation manager is a configuration error instead: when no manager can be resolved (`AddTlumachLocalization` is not called or sets no `TranslationManager`, `Configuration`, or `DefaultFile`, and `TlumachModelBindingOptions.TranslationManager` is not set), an `InvalidOperationException` that names `AddTlumachLocalization` is thrown. The failure is not cached; the next use tries again.
* The messages are looked up in the culture of the request, at the moment when MVC creates the message.
* <xref:Tlumach.AspNetCore.Mvc.TlumachModelBindingOptions> has `KeyPrefix` (the default is `ModelBinding.`) and `TranslationManager` (the default is the manager of `AddTlumachLocalization`).
* The messages apply to `[BindProperty]` properties, properties of model classes, and handler parameters alike.

The messages of validation attributes (`[Required]`, `[Range]`) are not model binding messages. They come from `AddDataAnnotationsLocalization`, where `ErrorMessage` is a translation key. Set `ErrorMessage` to a key, as the samples do (`[Required(ErrorMessage = "Validation.Required")]`): an attribute without `ErrorMessage` passes MVC's own English text to the localizer as if it were a key, and the text may come back with its `{0}` unfilled.

Under the ARB formats, the placeholders of a DataAnnotations message are filled **in the order of the arguments of the attribute**: `{field}` (the display name) first, then `{min}` and `{max}` for `[Range]`. The names of the placeholders are free (`{field}`, `{min}`, `{max}` are only a convention), but their order in the text is not: `"{field} must be between {min} and {max}."` works, while a text that puts `{max}` before `{min}` shows the values swapped. The .NET text format uses `{0}`, `{1}`, `{2}` in that order.

## 9. Display Names

The display name of a property appears in labels (`asp-for`), validation messages (`The {0} field is required.`), and the model binding messages. Without `[Display(Name = ...)]`, MVC shows the property name. `AddTlumachDisplayNames()` takes the names from Tlumach, in the culture of the request.

### The problem

MVC identifies the metadata of a property by the type that declares it plus the property name. The type name alone does not tell the properties apart in common code:

* Scaffolded and template pages nest `public class InputModel` in every page model: `ContactModel+InputModel`, `RegisterModel+InputModel`, `LoginModel+InputModel`, ...
* Scaffolded CRUD pages reuse the page model names in different folders: `Pages/Movies/Create.cshtml` has `MyApp.Pages.Movies.CreateModel`, and `Pages/Actors/Create.cshtml` has `MyApp.Pages.Actors.CreateModel`, each with its own `InputModel.Title`.

### Key styles

The key of a property is `DisplayNames.` + the key of its container type + `.` + the property name. <xref:Tlumach.AspNetCore.Mvc.TlumachDisplayNameKeyStyle> (the `KeyStyle` option) selects how the key of the type is formed:

| `KeyStyle` | Rule | `MyApp.Pages.Movies.CreateModel+InputModel` | `MyApp.Models.RegisterViewModel` | `Contoso.Shared.Address` (another assembly) |
|---|---|---|---|---|
| `RelativeTypeName` (the default) | The namespace without the root namespace, then the declaring types; `+` becomes `.`; the arity of generic types is dropped (``PagedList`1`` becomes `PagedList`) | `Pages.Movies.CreateModel.InputModel` | `Models.RegisterViewModel` | `Contoso.Shared.Address` |
| `TypeName` | The declaring types only | `CreateModel.InputModel` | `RegisterViewModel` | `Address` |
| `FullTypeName` | The full namespace and the declaring types | `MyApp.Pages.Movies.CreateModel.InputModel` | `MyApp.Models.RegisterViewModel` | `Contoso.Shared.Address` |

`RelativeTypeName` is unique within an application, does not change when the project is renamed, and mirrors the folder layout that the view keys use (`Pages.Movies.Create.` for the page and `Pages.Movies.CreateModel.InputModel.` for its model), so related texts sit next to each other. `TypeName` is the shortest, but the types with one name share keys (both `CreateModel.InputModel` types above). The key computation is available as <xref:Tlumach.AspNetCore.Mvc.TlumachDisplayNameKeys> `.GetContainerKey(Type, TlumachDisplayNameKeyStyle, string?)`, so tools and tests can compute the same keys.

The root namespace cannot be read at run time. The <xref:Tlumach.AspNetCore.Mvc.TlumachDisplayNameOptions.RootNamespace> option returns it for the assembly of a type; the default is the simple name of the assembly with `-` replaced by `_`, which is the default of SDK projects. Set it when your project has another `RootNamespace`; returning `null` removes nothing. A type from an assembly with another root namespace keeps its full namespace in the key. For full control, set `ContainerKey`, a `Func<Type, string>` that overrides the style.

### Lookup

For a property without a display name from other sources, two keys are tried in the culture of the request:

1. `DisplayNames.{container key}.{property}`, e.g. `DisplayNames.Pages.Movies.CreateModel.InputModel.Title`;
2. `DisplayNames.{property}`, e.g. `DisplayNames.Title`, for names that are common to many models.

When neither exists, MVC shows the property name. There is deliberately no step in between (such as a bare type name), because that would bring back the ambiguity above.

`[Display(Name = ...)]` and `[DisplayName]` always take precedence, whatever the order of registration; they stay localized by `AddDataAnnotationsLocalization`. Only properties get names this way (parameters and types do not). The properties that a derived model inherits are keyed under the derived type; the shared key covers names that are common to all of them. A failed attempt to find the translation manager is not cached; it is repeated on the next use.

The names of the sample (INI, for comparison with the ARB of the samples):

```ini
[DisplayNames]
Email=E-mail address
Title=Title

[DisplayNames.Pages.Movies.CreateModel.InputModel]
Title=Movie title

[DisplayNames.Pages.Actors.CreateModel.InputModel]
Title=Stage name
```

and the ARB of the MVC sample, where the view model is `Tlumach.Sample.Mvc.Models.RegisterViewModel`:

```json
"DisplayNames": {
    "Email": "E-mail address",
    "Models": {
        "RegisterViewModel": { "Name": "Your name", "Age": "Your age" }
    }
}
```

The display name flows into the messages of validation attributes, so `"{field} is required."` shows "Your name is required." with the translated name. (Under the ARB formats, `{field}` is filled with the display name by position, as the annotation passes it first; the same holds for the other placeholders of a message of an annotation, which are filled in the order of the arguments of the attribute, e.g. `{min}` and `{max}` of `[Range]` follow `{field}`.)

### Diagnostics

When a property falls through to its name, the provider writes a `Debug` message to the log (once per key and culture) that lists the keys it tried, e.g. "No display name was found for the keys 'DisplayNames.Models.RegisterViewModel.Age' and 'DisplayNames.Age' in the culture 'uk'; the property name is used." Enable the category `Tlumach.AspNetCore.Mvc.TlumachDisplayMetadataProvider` at the `Debug` level to see them (`"Logging:LogLevel:Tlumach.AspNetCore.Mvc": "Debug"`), and copy the key from the message instead of deriving it from the rules.

## 10. Security Notes

* **Translations are trusted HTML** in all of these APIs. Anyone who can change a translation file can inject markup and script into the pages. Translation files must come from sources that you trust. (There is no mode that sanitizes translations.)
* Placeholder values are HTML-encoded, but encoding does not validate URL schemes. In a translation such as `<a href="{url}">`, a user-supplied `javascript:` URL is still dangerous. Validate such URLs in the application code (e.g. accept only `http` and `https`) or build the link in the view.
* The culture endpoint and the culture selector reuse the checks of the Blazor integration: the culture must be a supported one and the `redirectUri` must be a local path. The endpoint carries no antiforgery token by design; at worst, a request from another site switches the user to another supported language.
* A generated translation unit renders with the values cached on it, and they are shared by all requests; do not cache the values of one user on a unit (section 6).

## See Also

* [Getting Started for integration with ASP.NET Core MVC](getting-started-aspnetcore-mvc.md)
* [Getting Started for integration with Razor Pages](getting-started-razor-pages.md)
* [Dependency Injection](di.md)
* [Localization of Data Annotations](data-annotations.md)
* [Templates and Placeholders](placeholders.md)
