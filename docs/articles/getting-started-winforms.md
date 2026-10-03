# Getting Started

## Integration with Windows Forms

Tlumach supports Windows Forms applications on .NET Framework 4.7.2 and later, .NET 9, and .NET 10 through the _Tlumach.WinForms_ assembly. It offers two ways to localize the UI, which can be combined:

* the **TranslationProvider** component, which you place on a form in the Visual Studio Designer and which adds the translation key properties to controls, menu and toolbar items, and list view column headers;
* the **BindTranslation** extension methods, which bind properties of controls to [generated translation units](glossary.md#GeneratedUnit) in code.

In both cases, the texts are updated when the current language is switched, even if the language is switched in a background thread.

Windows Forms applications that target .NET 8 are not supported by _Tlumach.WinForms_: the package has no `net8.0-windows` assets, so target .NET 9, .NET 10, or .NET Framework 4.7.2 or later.

**1. Add Tlumach to your project**:

a) via NuGet

Add a package reference to "AlliedBits.Tlumach" to your project

* via NuGet package manager GUI in Visual Studio

* via the command line:

```cmd
dotnet add package AlliedBits.Tlumach
```

* using the text editor - add the following reference to your project:
```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach" Version="1.*" />
</ItemGroup>
```

b) with Source Code

- Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)
- Add _Tlumach.Base_, _Tlumach_, and _Tlumach.WinForms_ projects to your solution and reference them from your project(s).

**2. Create a configuration file, a default translation file, and a translation project**

These steps are the same as for other application types. Follow steps 2 to 7 of [Getting Started for integration with WPF](getting-started-wpf.md): they create "strings.cfg" with `generatedClass=Strings` and `generatedNamespace=Tlumach.Sample`, a "strings.toml" file with the `hello` key, and a translation project that you reference from your Windows Forms project.

After the translation project is built, the generated class `Tlumach.Sample.Strings` contains the `TranslationManager` property and a [translation unit](glossary.md#GeneratedUnit) for every key.

**3. Tell Tlumach.WinForms which translation manager to use**

Set the default translation manager once, before the first form is created:

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

`ApplicationConfiguration.Initialize()` is the call from the .NET 6+ project template. On .NET Framework, use `Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);` instead.

If different forms use different generated classes, set the `TranslationManager` property of the provider on a form instead (e.g., right after the call to `InitializeComponent()` in the constructor of the form). This property takes priority over `DefaultTranslationManager`.

**4. Localize forms in the Designer**

1. Build the solution, so that Visual Studio picks up the `TranslationProvider` component and shows it in the Toolbox.
2. Drop a `TranslationProvider` onto the form. It appears in the component tray as "translationProvider1".
3. Select a control, a menu item, or a list view column. In the Properties window, the "Localization" category now contains **TranslationKey on translationProvider1**. Enter the key of the string (e.g., `hello`). Use the full key with dots for keys in groups (e.g., `menu.file`).
4. To translate tooltips, drop a `ToolTip` component onto the form and select it in the **ToolTip** property of the provider. Then enter keys in **ToolTipKey on translationProvider1** of the controls. Menu and toolbar items do not need a `ToolTip` component: the provider sets their `ToolTipText` property.

The Designer keeps showing the texts that you typed. At run time, the provider replaces them with the translations when the form is initialized and every time the language changes. If a key is not found in the translation, the text from the Designer is kept.

For the form's caption, assign a key to the form itself.

The provider resolves keys as strings, so placeholders in such strings are not processed. For strings with placeholders, use the binding in code (see below).

**5. Localize forms in code**

The `BindTranslation` extension methods bind a property to a generated translation unit and keep it updated:

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

The method with a setter works with any component, including `DataGridViewColumn`, which is not a `Component` but implements `IComponent`. Properties that exist only on .NET (for example, `TextBox.PlaceholderText`, which is not available on .NET Framework) can be bound in the same way if your project targets .NET.

A binding is disposed of together with its control. To stop the updates earlier, dispose of the `TranslationBinding` object returned by the method.

A translation key assigned in the Designer and a code binding on the same property overwrite each other: whichever applies last wins. Use one or the other for a given property.

Bindings work with units that contain placeholders. Provide the placeholder values via the `OnPlaceholderValueNeeded` event of the unit or cache them, and call `NotifyPlaceholdersUpdated()` when the values change:

```c#
Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
helloNameLabel.BindTranslation(Strings.helloName);

nameTextBox.TextChanged += (sender, e) =>
{
    Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
    Strings.helloName.NotifyPlaceholdersUpdated();
};
```

**Lifetime**

The generated classes keep their `TranslationManager` in a static property, and both the provider and the bindings subscribe to its `OnCultureChanged` event. A form that is never disposed of therefore stays in memory and keeps being updated when the language changes.

* A form shown with `Show()` is disposed of when it is closed.
* A modal form shown with `ShowDialog()` is only hidden when it is closed, not disposed of, so dispose of it yourself: `using var dialog = new AboutForm(); dialog.ShowDialog(this);`.
* Dispose of `TranslationProvider` instances that you create in code without a container.
* Dispose of the bindings of controls that you remove from a form but do not dispose of.

**Switching languages**

To switch the current language, assign a new value to <xref:Tlumach.TranslationManager.CurrentCulture>:

```c#
using Tlumach.Sample;
...
CultureInfo deCulture = new CultureInfo("de-DE");
Strings.TranslationManager.CurrentCulture = deCulture;
```

The providers and the bindings update the controls on the UI thread, so the culture may be changed in any thread.

Remember that you need [locale-specific files](glossary.md#LocaleSpecificFile) for other languages. For this, read about [Translation Files and Formats](files-formats.md).

**Right-to-left languages**

Set the **ApplyRightToLeft** property of the provider to `true` to switch the form to the right-to-left layout when the current culture is written from right to left (e.g., Arabic or Hebrew). The provider then sets the `RightToLeft` property of the form (its **ContainerControl** property, which the Designer sets automatically) and its `RightToLeftLayout` property.

**Sample**

The [Windows Forms sample](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples/Tlumach.Sample.WinForms) shows both ways of localization and a language selector.
