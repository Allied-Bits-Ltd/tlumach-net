# Getting Started

## Integration with ASP.NET Core MVC

Tlumach localizes ASP.NET Core MVC applications (controllers and views): the texts of views come from Tlumach translations, in the language of each request; the messages of model binding and the display names of model properties are localized, too; and a tag helper lets users switch the language. This article walks through the setup step by step, using the sample `samples/Tlumach.Sample.Mvc` (with `samples/Tlumach.Sample.Mvc.Translation`) as the example. For an application that uses Razor Pages only, see [Getting Started for integration with Razor Pages](getting-started-razor-pages.md). The details of every feature are in [Localization of MVC and Razor Pages](razor-localization.md).

The integration is in the `Tlumach.AspNetCore.Mvc` assembly (.NET 9 and .NET 10), together with `Tlumach.AspNetCore`; both are in the `AlliedBits.Tlumach.AspNetCore` package, which brings `AlliedBits.Tlumach.Web`, `AlliedBits.Tlumach.Extensions.Localization`, and the core `AlliedBits.Tlumach` package with it. It does not depend on Blazor.

### 1. Translations

Create a translation project with the generator as described in [Generator](generator.md), e.g. a .NET Standard 2.0 class library with ARB files as embedded resources and a configuration file. The configuration of the sample:

```ini
defaultFile=sample.arb
generatedNamespace=Tlumach.Sample.Mvc.Translation
generatedClass=Strings
textProcessingMode=ArbNoEscaping
delayedUnitsCreation=true

[translations]
de=sample_de.arb
uk=sample_uk.arb
```

`ArbNoEscaping` is the ARB format in which an apostrophe is plain text, so a text such as `The value '{value}' is not valid` keeps its placeholder. Use `Arb` when you need the quoting of ICU. In both modes, the placeholders are named: `{name}`, `{count, plural, ...}`.

The keys of the translations are the keys that views use. The texts of a view are in a group that is named after the path of the view (`/Views/Home/Index.cshtml` has the keys that start with `Views.Home.Index.`), and the texts that views share are at the top level. In ARB, the groups are nested objects:

```json
{
    "@@locale": "en",
    "AppName": "Tlumach MVC sample",
    "Welcome": "Welcome to the <b>Tlumach</b> MVC sample",
    "Footer": "Translated with Tlumach · {year}",
    "Views": {
        "Home": {
            "Index": {
                "Title": "Home",
                "Intro": "Hello, <b>{name}</b>! This page is translated with IViewLocalizer.",
                "Items": "{count, plural, =0{Your cart is empty} =1{You have # item} other{You have # items}}",
                "TagHelper": "This paragraph uses the <code>tlumach-key</code> tag helper."
            }
        },
        "Account": {
            "Register": { "Title": "Register", "Submit": "Create account" }
        }
    },
    "ModelBinding": {
        "AttemptedValueIsInvalid": "The value '{value}' is not valid for {field}.",
        "ValueMustBeANumber": "The field {field} must be a number."
    },
    "DisplayNames": {
        "Email": "E-mail address",
        "Models": { "RegisterViewModel": { "Name": "Your name", "Age": "Your age" } }
    },
    "Validation": {
        "Required": "{field} is required.",
        "Email": "{field} is not valid.",
        "Range": "{field} must be between {min} and {max}."
    }
}
```

In the messages of validation attributes, the names of the placeholders are free, but their order in the text is not: under the ARB formats, the values are filled in the order of the arguments of the attribute (`{field}`, then `{min}` and `{max}` of `[Range]`).

Translations are trusted HTML: `<b>` in `Intro` reaches the page as markup, while the values of the placeholders (`{name}`) are encoded.

Reference the translation project from the web project, which also references `Tlumach.AspNetCore.Mvc` (the project, or the `AlliedBits.Tlumach.AspNetCore` package).

### 2. Registration

`Program.cs`:

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

app.UseTlumachRequestLocalization();   // before the endpoints
app.UseStaticFiles();
app.MapTlumachCultureEndpoint();       // "/tlumach/culture" by default; the culture selector uses it
app.MapDefaultControllerRoute();

await app.RunAsync().ConfigureAwait(false);
```

`AddTlumachLocalization` is required: it tells Tlumach which translation manager to use. `AddTlumachCultures` lists the languages of the application; `UseTlumachRequestLocalization` sets the culture of every request from the cookie that the culture selector writes (and, without a cookie, from the `Accept-Language` header) among these languages. `AddTlumachViewLocalization()` registers `IViewLocalizer`, `IHtmlLocalizer`, and `IHtmlLocalizer<T>`; the order of its call relative to `AddViewLocalization()` does not matter. The last two calls are optional (steps 6 and 7).

### 3. Enable the tag helpers

`Views/_ViewImports.cshtml`:

```razor
@using Tlumach.Sample.Mvc
@using Tlumach.Sample.Mvc.Models
@using Tlumach.Sample.Mvc.Translation
@using Tlumach.AspNetCore.Mvc
@using Microsoft.AspNetCore.Mvc.Localization
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Tlumach.AspNetCore.Mvc
```

### 4. A view with `IViewLocalizer`

`Views/Home/Index.cshtml`:

```razor
@inject IViewLocalizer L
@{ ViewData["Title"] = L.GetString("Title").Value; }
<h1>@L["Title"]</h1>
<p>@Html.Tlumach(Strings.Welcome)</p>
<p>@L["Intro", "Ada"]</p>
<p>@L["Items", 3]</p>
<p tlumach-key="TagHelper"></p>
<p><tlumach-text key="Intro" arg-name="@("<script>")" /></p>
```

`L["Title"]` is looked up as `Views.Home.Index.Title` and, if there is no such key, as the shared key `Title`. Values (`"Ada"`, `3`) fill the placeholders by position: the first value takes the first placeholder of the text, whatever its name. The ICU `plural` of `Items` works in the language of the request. The values are HTML-encoded; the markup of the translation is not.

`L.GetString("Title")` returns plain text (use it for `ViewData["Title"]`); `L["Title"]` returns HTML. Layouts and partial views have their own prefix: `Views/Shared/_Layout.cshtml` is `Views.Shared._Layout.`, so the layout can use `@L["AppName"]` and `@L["Footer", DateTime.Now.Year]` with the shared keys, as the sample does.

### 5. Tag helpers and `Html.Tlumach`

The last three lines of the view above show the other ways to show a translation:

* `Html.Tlumach(Strings.Welcome)` renders a generated translation unit. A typo in the name of the unit is a compilation error. It also replaces `@Html.Raw(Strings.Welcome)`, which is no longer needed, whether `WebEncodeValues` is on or off.
* `<p tlumach-key="TagHelper"></p>` replaces the content of an element with the translation of the key. `tlumach-unit="Strings.Welcome"` does so for a unit. The values are C# expressions: `tlumach-arg-name="@Model.Name"` for a named value, `tlumach-args="@(new object?[] { Model.Name })"` for the positional ones.
* `<tlumach-text key="Intro" arg-name="@("<script>")" />` renders the translation without a wrapper element.

Because the values are encoded, the `<script>` of the example shows up as text on the page.

The header of the layout (`Views/Shared/_Layout.cshtml`) uses the tag helper on links, which keeps the `asp-*` attributes working:

```razor
<a asp-controller="Home" asp-action="Index" tlumach-key="Views.Home.Index.Title"></a>
<a asp-controller="Account" asp-action="Register" tlumach-key="Views.Account.Register.Title"></a>
```

### 6. The culture selector

Place the selector in the layout:

```razor
<tlumach-culture-selector class="lang" />
```

It renders a small form with a `select` of the supported cultures and an "OK" button, which sends the choice to the culture endpoint. The endpoint stores it in a cookie and returns to the current page. The form needs no JavaScript; set `button-text` to change the text of the button, and see [Localization of MVC and Razor Pages](razor-localization.md) for the attributes and for a snippet that submits the form when the selection changes.

### 7. Model binding messages and display names

The sample page `Views/Account/Register.cshtml` is bound to `RegisterViewModel`:

```csharp
public class RegisterViewModel
{
    [Required(ErrorMessage = "Validation.Required")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    public string? Email { get; set; }

    [Range(1, 150, ErrorMessage = "Validation.Range")]
    public int Age { get; set; }
}
```

* `ErrorMessage` of an annotation is a translation key (`AddDataAnnotationsLocalization` resolves it through Tlumach). Set it to have the message translated: an attribute without `ErrorMessage` hands MVC's English text to the localizer as if it were a key, and the message stays in English (with the display name filled in).
* The properties have no `[Display]`, and `AddTlumachDisplayNames()` takes their names from `DisplayNames.Models.RegisterViewModel.Name` (the namespace of the model without the root namespace of the application, then the type name), and then from the shared `DisplayNames.Name`. The labels (`<label asp-for="Name">`) and the messages show these names: `"{field} is required."` gives "Your name is required." or, in German, "Ihr Name ist erforderlich."
* `AddTlumachModelBindingMessages()` localizes the messages that appear before validation. Enter `abc` as the age and the page shows the message `ModelBinding.AttemptedValueIsInvalid` ("The value 'abc' is not valid for Your age.") in the language of the user. In ARB translations, the values are `{value}` and `{field}`.

### 8. Try it

Start the sample (`dotnet run --project samples/Tlumach.Sample.Mvc`), open the home page, and switch the language with the selector: the views, the labels, and the validation messages change. A request in another language is served by the same process at the same time, each in its own culture.

### Where next

* [Localization of MVC and Razor Pages](razor-localization.md): the reference of keys, encoding, tag helpers, the selector, model binding messages, display names, and security notes.
* [Localization of Data Annotations](data-annotations.md) and [Dependency Injection](di.md).
* [Getting Started for integration with Razor Pages](getting-started-razor-pages.md) for the specifics of Razor Pages (page models, culture-specific files, areas).
