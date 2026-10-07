# Getting Started

## Integration with Razor Pages

Tlumach localizes Razor Pages applications that have no controllers (`AddRazorPages()` only): the texts of pages and partial views come from Tlumach translations, in the language of each request; the messages of model binding and the display names of model properties are localized, too; and a tag helper lets users switch the language. The integration uses the same assemblies as that of MVC, and the setup differs in a few details only. This article walks through the setup using the sample `samples/Tlumach.Sample.RazorPages` (with `samples/Tlumach.Sample.RazorPages.Translation`) and covers what is specific to pages: culture-specific files, the code of page models, handler parameters, and areas. The details of every feature are in [Localization of MVC and Razor Pages](razor-localization.md); the steps that are the same as in [Getting Started for integration with ASP.NET Core MVC](getting-started-aspnetcore-mvc.md) are described there.

The integration is in the `Tlumach.AspNetCore.Mvc` assembly (.NET 9 and .NET 10), together with `Tlumach.AspNetCore` and `Tlumach.Web`, all in the `AlliedBits.Tlumach` package. The name says MVC, but nothing in it needs controllers: the registration methods extend `IMvcBuilder`, which `AddRazorPages()` returns, too. It does not depend on Blazor.

### 1. Translations

Create a translation project with the generator as described in [Generator](generator.md), as in the MVC article. The keys of a page start with its path: `/Pages/Contact.cshtml` has the keys that start with `Pages.Contact.`, `/Pages/Movies/Create.cshtml` has `Pages.Movies.Create.`; the texts that pages share are at the top level. The sample (ARB, `ArbNoEscaping`):

```json
{
    "@@locale": "en",
    "AppName": "Tlumach Razor Pages sample",
    "Welcome": "Welcome to the <b>Tlumach</b> Razor Pages sample",
    "Footer": "Translated with Tlumach · {year}",
    "Save": "Save",
    "Pages": {
        "Index": {
            "Title": "Home",
            "Intro": "Hello, <b>{name}</b>! This page is translated with IViewLocalizer."
        },
        "Contact": {
            "Title": "Contact",
            "Submit": "Send",
            "Sent": "Thank you, {name}!"
        },
        "Movies": { "Create": { "Title": "Add a movie" } },
        "Actors": { "Create": { "Title": "Add an actor" } }
    },
    "ModelBinding": {
        "AttemptedValueIsInvalid": "The value '{value}' is not valid for {field}.",
        "ValueMustBeANumber": "The field {field} must be a number."
    },
    "DisplayNames": {
        "Email": "E-mail address",
        "Pages": {
            "ContactModel": { "InputModel": { "Name": "Your name", "Age": "Your age" } },
            "Movies": { "CreateModel": { "InputModel": { "Title": "Movie title" } } },
            "Actors": { "CreateModel": { "InputModel": { "Title": "Stage name" } } }
        }
    },
    "Validation": {
        "Required": "{field} is required.",
        "Email": "{field} is not valid.",
        "Range": "{field} must be between {min} and {max}."
    }
}
```

In the messages of validation attributes, the names of the placeholders are free, but their order in the text is not: under the ARB formats, the values are filled in the order of the arguments of the attribute (`{field}`, then `{min}` and `{max}` of `[Range]`).

### 2. Registration

`Program.cs`:

```csharp
using System.Globalization;

using Tlumach.AspNetCore;
using Tlumach.AspNetCore.Mvc;
using Tlumach.Extensions.Localization;
using Tlumach.Sample.RazorPages.Translation;
using Tlumach.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachCultures(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de"), CultureInfo.GetCultureInfo("uk")];
    options.DefaultCulture = CultureInfo.GetCultureInfo("en");
});

builder.Services.AddRazorPages()
    .AddTlumachViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddTlumachModelBindingMessages()
    .AddTlumachDisplayNames();

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseStaticFiles();
app.MapTlumachCultureEndpoint();
app.MapRazorPages();

await app.RunAsync().ConfigureAwait(false);
```

`AddTlumachLocalization` is required; `AddTlumachCultures` lists the languages; the calls on the builder may come in any order. The last two are optional.

Enable the tag helpers in `Pages/_ViewImports.cshtml`:

```razor
@namespace Tlumach.Sample.RazorPages.Pages
@using Tlumach.Sample.RazorPages
@using Tlumach.Sample.RazorPages.Translation
@using Tlumach.AspNetCore.Mvc
@using Microsoft.AspNetCore.Mvc.Localization
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Tlumach.AspNetCore.Mvc
```

### 3. Pages and the layout

`Pages/Index.cshtml` uses `IViewLocalizer`, `Html.Tlumach`, and the tag helper:

```razor
@page
@inject IViewLocalizer L
@{ ViewData["Title"] = L.GetString("Title").Value; }
<h1>@L["Title"]</h1>
<p>@Html.Tlumach(Strings.Welcome)</p>
<p>@L["Intro", "Ada"]</p>
<p tlumach-key="Welcome"></p>
```

`L["Title"]` is looked up as `Pages.Index.Title` and then as the shared key `Title`. Translations are trusted HTML and the values of placeholders are encoded; see [Localization of MVC and Razor Pages](razor-localization.md) for the rules. The layout `Pages/Shared/_Layout.cshtml` has the prefix `Pages.Shared._Layout.`; it uses the shared keys `AppName` and `Footer`, links with `tlumach-key` (the `asp-page` attributes keep working), and the culture selector:

```razor
<a asp-page="/Index" tlumach-key="Pages.Index.Title"></a>
<a asp-page="/Contact" tlumach-key="Pages.Contact.Title"></a>
<tlumach-culture-selector class="lang" />
```

The selector sends the choice to the culture endpoint that `Program.cs` maps, which stores it in a cookie and returns to the current page; no JavaScript is needed.

Two pages that submit a form use a shared key for the button: `@L["Save"]` finds no `Pages.Movies.Create.Save` and takes the shared `Save`.

### 4. Model binding messages and display names

`Pages/Contact.cshtml.cs` binds a nested `InputModel`, as scaffolded pages do:

```csharp
public sealed class ContactModel : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Validation.Required")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Validation.Required")]
        [EmailAddress(ErrorMessage = "Validation.Email")]
        public string? Email { get; set; }

        [Range(1, 150, ErrorMessage = "Validation.Range")]
        public int Age { get; set; }
    }
}
```

* `ErrorMessage` of an annotation is a translation key. Always set it: an attribute without `ErrorMessage` hands MVC's English text to the localizer as if it were a key.
* The display name of `Input.Name` comes from `DisplayNames.Pages.ContactModel.InputModel.Name` (the namespace of the page model without the root namespace of the application, then the nested types), and then from the shared `DisplayNames.Name`. The `CreateModel.InputModel` of `Pages/Movies/Create.cshtml` and that of `Pages/Actors/Create.cshtml` are different types with the same name, and they get different keys, `DisplayNames.Pages.Movies.CreateModel.InputModel.Title` ("Movie title") and `DisplayNames.Pages.Actors.CreateModel.InputModel.Title` ("Stage name"). If a name does not show up, enable the `Debug` log of `Tlumach.AspNetCore.Mvc`, which lists the keys that were tried; see the key styles in [Localization of MVC and Razor Pages](razor-localization.md).
* Enter `abc` as the age: the message `ModelBinding.AttemptedValueIsInvalid` is shown in the language of the request.

### 5. Specifics of Razor Pages

**Culture-specific files.** `LanguageViewLocationExpander`, which `AddTlumachViewLocalization()` adds, affects the lookup of **views**, i.e. the partial views and layouts that pages render: `Pages/Shared/_Header.de.cshtml` is used instead of `_Header.cshtml` in the German request. Pages are selected by route and not by view lookup, so a page file with a culture in its name is not a German variant of the page. `Pages/Variant.de.cshtml` is a separate page at the route `/Variant.de`, and `/Variant` does not find it. Localize the content of pages with keys instead of language-specific page files.

**Page model code.** `IViewLocalizer` needs the context of a view and is not available in a `PageModel`. A handler that produces texts (a status message, `ViewData["Title"]`, `TempData`) injects `IStringLocalizer<TPageModel>` or `IHtmlLocalizer<TPageModel>` and uses the **full key**, so the same group of translations serves the page and its model:

```csharp
public sealed class ContactModel : PageModel
{
    private readonly IStringLocalizer<ContactModel> _localizer;

    public ContactModel(IStringLocalizer<ContactModel> localizer) => _localizer = localizer;

    public string? Message { get; private set; }

    public void OnPost()
    {
        if (ModelState.IsValid)
            Message = _localizer["Pages.Contact.Sent", Input.Name ?? string.Empty].Value;
    }
}
```

`IHtmlLocalizer<ContactModel>` works the same way and returns HTML with encoded values (see section 4 of [Localization of MVC and Razor Pages](razor-localization.md)). The localizers of a type use the default options of `AddTlumachLocalization`, unless options are registered for the full name of the type.

**Handler parameters.** The model binding messages apply to `[BindProperty]` properties and to the parameters of handlers (`OnPost(int age)`) alike. The display names apply to properties only: a handler parameter keeps its name, unless it has `[Display]`.

**Areas.** A page in an area has the prefix of its whole path: `/Areas/Admin/Pages/Users/List.cshtml` has the keys that start with `Areas.Admin.Pages.Users.List.`. The page models in the namespaces of areas get the display name keys from their namespaces, e.g. `DisplayNames.Areas.Admin.Pages.Users.ListModel.InputModel.Name`.

### 6. Try it

Start the sample (`dotnet run --project samples/Tlumach.Sample.RazorPages`), switch the language with the selector, and open the contact page and the two "create" pages: the texts of the pages, the labels, and the validation messages change with the language.

### Where next

* [Localization of MVC and Razor Pages](razor-localization.md): the reference of keys, encoding, tag helpers, the selector, model binding messages, display names, and security notes.
* [Getting Started for integration with ASP.NET Core MVC](getting-started-aspnetcore-mvc.md) for the same features in an MVC application.
* [Localization of Data Annotations](data-annotations.md) and [Dependency Injection](di.md).
