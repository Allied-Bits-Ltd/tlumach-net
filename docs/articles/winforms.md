# Windows Forms integration

## Overview

`Tlumach.WinForms` localizes Windows Forms applications. It offers two ways to set the texts of the UI from translations, which can be combined on one form:

* the <xref:Tlumach.WinForms.TranslationProvider> component, which you place on a form in the Visual Studio Designer and which adds the **TranslationKey** and **ToolTipKey** properties to controls, tool strip items, and list view column headers;
* the `BindTranslation` extension methods of <xref:Tlumach.WinForms.TranslationBindingExtensions>, which bind properties of components to [generated translation units](glossary.md#GeneratedUnit) in code.

In both cases, the texts are updated when the current language is switched, also when it is switched in a background thread, without restarting the application or re-creating the forms.

This article is the reference. For a step-by-step introduction, read [Getting Started for integration with Windows Forms](getting-started-winforms.md). The sample is `samples/Tlumach.Sample.WinForms` in the [repository](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples/Tlumach.Sample.WinForms).

## Packages and target frameworks

The `Tlumach.WinForms` assembly is in the `AlliedBits.Tlumach.WinForms` package, which brings the core `AlliedBits.Tlumach` package with it. It is built for .NET Framework 4.7.2, .NET 9 (`net9.0-windows`), and .NET 10 (`net10.0-windows`), so it can be used by applications that target .NET Framework 4.7.2 or later, .NET 9, or .NET 10. The package has no assets for .NET 8, so Windows Forms applications that target .NET 8 are not supported. The package does not depend on a framework package, because Windows Forms is a part of .NET Framework and of the Windows desktop shared framework of .NET.

## Where the translations come from

The texts come from a <xref:Tlumach.TranslationManager>, normally the `TranslationManager` property of a class created by [Generator](generator.md) in a [project with translations](generator.md#TranslationProject). The same class contains a translation unit for every key, which the `BindTranslation` methods take. The generated class of Windows Forms applications uses the <xref:Tlumach.TranslationUnit> class of the core package, so the project with translations references `AlliedBits.Tlumach` and needs no Windows Forms-specific settings.

In .NET Framework applications, the project with translations needs nothing special either: since version 2.0, Generator produces code that compiles with C# 7.3, the default language version of .NET Framework and .NET Standard 2.0 projects, and works in projects in the old (non-SDK) format that are built by the C# compiler that runs on .NET Framework, such as Visual Studio builds of such projects. See [Packages](referencing.md) for the requirements of Generator.

## The TranslationProvider component

<xref:Tlumach.WinForms.TranslationProvider> is an extender provider: once it is placed on a form, the Properties window shows **TranslationKey on translationProvider1** and **ToolTipKey on translationProvider1** in the "Localization" category of every supported component. Enter the key of a string there, using the full key with dots for keys in groups (e.g., `menu.file`). To translate the caption of a form, assign a key to the form itself.

At run time, the provider replaces the texts with the translations when the form is initialized and every time the culture of the translation manager changes. When a key is not found in the translation, the text set in the Designer is kept.

### Supported components

| Component | TranslationKey sets | ToolTipKey sets |
|---|---|---|
| Controls, including forms and user controls | `Control.Text` | The tooltip that the `ToolTip` component of the provider shows for the control |
| Tool strip items (menu items, toolbar buttons, status labels, ...) | `ToolStripItem.Text` | `ToolStripItem.ToolTipText` |
| List view column headers (`ColumnHeader`) | `ColumnHeader.Text` | Nothing; column headers have no tooltips |

Other components, such as `DataGridViewColumn`, and other properties, such as `AccessibleDescription`, are bound in code (see [Binding in code](#binding-in-code)). List view items are not components; the application sets their texts itself, as the sample does.

### Translation managers

The provider resolves the keys with its <xref:Tlumach.WinForms.TranslationProvider.TranslationManager> property or, when that is `null`, with the static <xref:Tlumach.WinForms.TranslationProvider.DefaultTranslationManager> property. Set `DefaultTranslationManager` once, before the first form is created, because the provider applies the translations at the end of `InitializeComponent`:

```c#
using Tlumach.WinForms;
using Tlumach.Sample;

[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();

    TranslationProvider.DefaultTranslationManager = Strings.TranslationManager;

    Application.Run(new MainForm());
}
```

On .NET Framework, call `Application.EnableVisualStyles()` and `Application.SetCompatibleTextRenderingDefault(false)` instead of `ApplicationConfiguration.Initialize()`.

`TranslationManager` is not available in the Designer. Set it in code, for example right after `InitializeComponent()` in the constructor of the form, when different forms use different generated classes; setting it applies the translations at once. The provider listens to the culture changes of the effective translation manager only, and it picks up the current value of `DefaultTranslationManager` every time it applies the translations.

<xref:Tlumach.WinForms.TranslationProvider.ApplyTranslations> applies the translations for the current culture to all components that have keys. The provider calls it itself when it is initialized, when its translation manager is set, and when the culture changes. Call it when the translations change in some other way, for example after the translation files have been reloaded, or when `DefaultTranslationManager` was set only after the form had been created.

### Tooltips

Controls show tooltips through a `ToolTip` component. Drop one onto the form and select it in the <xref:Tlumach.WinForms.TranslationProvider.ToolTip> property of the provider; the **ToolTipKey** values of controls are then shown by that component. Tool strip items show their own tooltips and do not need it.

### Right-to-left layout

When <xref:Tlumach.WinForms.TranslationProvider.ApplyRightToLeft> is `true`, the provider sets the `RightToLeft` property of its <xref:Tlumach.WinForms.TranslationProvider.ContainerControl> to `Yes` when the current culture is written from right to left (e.g., Arabic or Hebrew) and to `No` otherwise, and, when the container is a form, its `RightToLeftLayout` property as well. This happens every time the translations are applied, so the value set in the Designer is overwritten; when `ApplyRightToLeft` is turned off, the current value is left as it is.

In the Designer, `ContainerControl` is set automatically to the form or user control being designed. A provider created in code needs `ContainerControl` to be set explicitly.

### Design-time behavior

In design mode, including the case when a form or user control with a provider is a part of another form being designed, the provider never changes any text. The Designer keeps showing the texts that you typed, and translated texts never get into the code of the form. The Designer serializes only the keys and the settings of the provider:

```c#
translationProvider1 = new Tlumach.WinForms.TranslationProvider(components);
toolTip1 = new System.Windows.Forms.ToolTip(components);
((System.ComponentModel.ISupportInitialize)translationProvider1).BeginInit();
...
translationProvider1.ApplyRightToLeft = true;
translationProvider1.ContainerControl = this;
translationProvider1.ToolTip = toolTip1;
...
translationProvider1.SetTranslationKey(helloLabel, "Hello");
translationProvider1.SetToolTipKey(languageComboBox, "LanguageHint");
translationProvider1.SetTranslationKey(this, "Welcome");
((System.ComponentModel.ISupportInitialize)translationProvider1).EndInit();
```

The provider implements `ISupportInitialize`: between `BeginInit` and `EndInit`, it collects the keys without applying them, and `EndInit` applies all of them at once. `SetTranslationKey` and `SetToolTipKey` can also be called in code; outside the initialization, they apply the translation at once. A `null` or empty key removes the assignment and leaves the current text as it is.

### Placeholders

The provider uses the text of a translation as it stands in the translation file, so placeholders in it, such as `{name}`, are not processed and appear literally. For strings with placeholders, use `BindTranslation`.

## Binding in code

The `BindTranslation` methods bind a property of a component to a translation unit, set the property at once, and keep it updated:

```c#
using Tlumach.WinForms;
using Tlumach.Sample;

helloLabel.BindTranslation(Strings.hello);                           // Control.Text
fileMenuItem.BindTranslation(Strings.menu.file);                     // ToolStripItem.Text
nameColumn.BindTranslation(Strings.columns.name);                    // ColumnHeader.Text of a ListView
toolTip1.BindTranslation(saveButton, Strings.hints.save);            // the tooltip of a control
searchBox.BindTranslation(Strings.hints.search, (box, text) => box.AccessibleDescription = text); // any property
nameGridColumn.BindTranslation(Strings.columns.name, (column, text) => column.HeaderText = text); // DataGridViewColumn.HeaderText
```

| Method | Binds |
|---|---|
| `BindTranslation(this Control, TranslationUnit)` | `Control.Text` |
| `BindTranslation(this ToolStripItem, TranslationUnit)` | `ToolStripItem.Text` |
| `BindTranslation(this ColumnHeader, TranslationUnit)` | `ColumnHeader.Text` |
| `BindTranslation(this ToolTip, Control, TranslationUnit)` | The tooltip that the `ToolTip` component shows for the control |
| `BindTranslation<T>(this T, TranslationUnit, Action<T, string>)` | Any property of any `IComponent`, set by the given method |

The method with a setter works with anything that implements `IComponent`, including `DataGridViewColumn`, which is not a `Component`. Properties that exist only on .NET, such as `TextBox.PlaceholderText`, can be bound in the same way when the project targets .NET.

A translation key assigned in the Designer and a binding on the same property overwrite each other, and whichever is applied last wins. Use one or the other for a given property.

### Units with placeholders

A binding sets the property to the `CurrentValue` of the unit, so placeholders are processed. Provide the values with the <xref:Tlumach.BaseTranslationUnit.OnPlaceholderValueNeeded> event of the unit or cache them with `CachePlaceholderValue`, and call `NotifyPlaceholdersUpdated()` when they change; the binding listens to this notification and re-reads the text:

```c#
Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
helloNameLabel.BindTranslation(Strings.helloName);

nameTextBox.TextChanged += (sender, e) =>
{
    Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
    Strings.helloName.NotifyPlaceholdersUpdated();
};
```

The values are kept in the unit, which is a static member of the generated class, so all bindings to the unit show the same values.

### The binding object

Each method returns a <xref:Tlumach.WinForms.TranslationBinding>. Its `Unit` property is the bound unit, `IsDisposed` tells whether the binding still updates the component, `Update()` applies the current text of the unit (on the UI thread), and `Dispose()` stops the updates and releases the event subscriptions. A binding is disposed of automatically when its component is disposed of; for a tooltip binding, that is the control. If the first update in the method fails, the binding is disposed of, and the exception propagates.

## Switching the language

To switch the language, assign a new culture to <xref:Tlumach.TranslationManager.CurrentCulture> of the translation manager:

```c#
Strings.TranslationManager.CurrentCulture = new CultureInfo("de-DE");
```

The translation manager raises its `OnCultureChanged` event, and the providers and the bindings update their components. Remember that other languages need [locale-specific files](glossary.md#LocaleSpecificFile), see [Translation Files and Formats](files-formats.md), and see [Managing Languages](language-management.md) for listing the available languages, e.g. for a language selector.

### Background threads

The culture may be changed in any thread. The providers and the bindings remember the thread that created them, normally the UI thread, and its synchronization context. A change made on that thread updates the components at once; a change made on another thread is posted to the UI thread, so the components are updated shortly afterwards, when the UI thread processes the posted update. Create the forms and the bindings on the UI thread, as usual in Windows Forms.

## Lifetime and disposal

The generated classes keep their `TranslationManager` in a static property, and both the providers and the bindings subscribe to its `OnCultureChanged` event. A form that is never disposed of therefore stays in memory and keeps being updated when the language changes.

* A provider placed in the Designer is added to the `components` container of the form and is disposed of together with the form. A form shown with `Show()` is disposed of when it is closed.
* A modal form shown with `ShowDialog()` is only hidden when it is closed, so dispose of it yourself, e.g. with a `using` statement.
* Dispose of `TranslationProvider` instances that you create in code without a container.
* A provider forgets a component when the component is disposed of, and a binding disposes of itself together with its component. Dispose of the bindings of components that you remove from a form but do not dispose of.

## Sample

The `samples/Tlumach.Sample.WinForms` application shows both ways of localization: a provider with keys on labels, menu items, list view columns, and the form, tooltips, and the right-to-left layout, and bindings in code for texts with placeholders, together with a language selector.

## See also

* [Getting Started for integration with Windows Forms](getting-started-winforms.md)
* [Generator](generator.md)
* [Strings and Translations](strings.md)
* [Templates and Placeholders](placeholders.md)
* [Managing Languages](language-management.md)
* [Packages](referencing.md)
