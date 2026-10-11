# XAML integration

Tlumach shines when it comes to supporting UIs. You can bind XAML controls of WPF, UWP, WinUI, MAUI, or Avalonia to [generated translation units](glossary.md), which gives you automatic syntax checks and automatic UI updates on language switching.

The use of Tlumach with the mentioned frameworks is very similar in principles, but each framework requires slightly different syntax described below.

**Note** on templated translation units: since version 1.1, you can bind XAML controls to these units, but you need to take extra steps to provide actual data for the placeholders in such units. Read the details in the [corresponding section of this documentation](strings.md#TemplatesInXAML).

<a name="wpf"></a>
## WPF (Windows Presentation Foundation)

In WPF, translations are referenced in the XAML code as follows:

```xml
<Window x:Class="YourNamespace.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:tlumach="clr-namespace:Tlumach.WPF;assembly=Tlumach.WPF"
        xmlns:translations="clr-namespace:ValueFromGeneratedNamespaceSetting;assembly=YourProjectWithTranslations"
...
        <TextBlock Text="{tlumach:Translate {x:Static translations:ValueFromGeneratedClass.TranslationUnitKey}}"/>
```
where
* **ValueFromGeneratedNamespaceSetting** is the namespace which you specified in the `generatedNamespace` configuration setting in your [configuration file](config-file.md),
* **YourProjectWithTranslations** is the name of your [project with translations](generator.md#TranslationProject),
* **ValueFromGeneratedClass** is the class name which you specified in the `generatedClass` configuration setting in your [configuration file](config-file.md),
* **TranslationUnitKey** is the key of the translation unit to bind the property to.

In the WPF sample project, this code looks like this:
```xml
<Window x:Class="Tlumach.Sample.WPF.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:tlumach="clr-namespace:Tlumach.WPF;assembly=Tlumach.WPF"
        xmlns:translations="clr-namespace:Tlumach.Sample;assembly=Tlumach.Sample.Translation"
...
        <TextBlock Text="{tlumach:Translate {x:Static translations:Strings.Hello}}" />
```

<a name="wpf-binding"></a>
### Taking the translation unit from a binding

The translation unit can also come from a binding, for example, when a view model decides which text to show:

```xml
<TextBlock Text="{tlumach:Translate {Binding StatusText}}" />
<TextBlock Text="{tlumach:Translate Unit={Binding StatusText}}" />
```

where `StatusText` is a property of the data context whose value is a `TranslationUnit`. Both forms are equivalent. The binding is resolved for the element that displays the text, just as if it were set on that element directly, so it can use the `DataContext`, `ElementName`, `RelativeSource`, or an explicit `Source`, and it works in data templates and in style setters, where each element gets the unit of its own data item. The text follows the changes of the bound property, the language changes, and the calls to <xref:Tlumach.BaseTranslationUnit.NotifyPlaceholdersUpdated>.

If the value of the binding is not a `TranslationUnit` (or the binding cannot be resolved), the property receives an empty string.

A `MultiBinding` or a `PriorityBinding` that produces the unit is evaluated outside of the element tree, so only the bindings with an explicit `Source` work in them; use a plain `Binding` whenever possible.

**Note** on versions before 2.0: there, the binding was evaluated outside of the element tree, so only the bindings with an explicit `Source` worked (the others produced an empty text), and the `Unit={Binding ...}` form failed when the XAML was loaded.

<a name="wpf-lifetime"></a>
### Lifetime of the bound elements

The generated classes keep the translation manager and the translation units in static properties, so they live as long as the application. `Translate` subscribes to their events weakly: closed windows, removed elements, and elements of data templates are garbage-collected as usual, and the subscriptions that they leave behind are removed the next time the language changes. Before version 2.0, each use of `Translate` stayed subscribed to the translation manager (and, with it, stayed in memory and was updated on every language change) for the lifetime of the application, even after its element was gone. The `Translate` markup extension of MAUI received the same fix.

<a name="uwp"></a>
## UWP

In UWP, translations are referenced in the XAML code as follows:

```xml
<Page x:Class="YourNamespace.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Tlumach.Sample.UWP"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:tru="using:ValueFromGeneratedNamespaceSetting"
...
      <TextBlock Text="{x:Bind tru:ValueFromGeneratedClass.TranslationUnitKey.CurrentValue, Mode=OneWay}"/>
```
where
* **ValueFromGeneratedNamespaceSetting** is the namespace which you specified in the `generatedNamespace` configuration setting in your [configuration file](config-file.md),
* **ValueFromGeneratedClass** is the class name which you specified in the `generatedClass` configuration setting in your [configuration file](config-file.md),
* **TranslationUnitKey** is the key of the translation unit to bind the property to.

In the WinUI sample project, this code looks like this:
```xml
<Page x:Class="Tlumach.Sample.UWP.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Tlumach.Sample.UWP"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:tru="using:Tlumach.Sample"
...
      <TextBlock Text="{x:Bind tru:Strings.Hello.CurrentValue, Mode=OneWay}" />
```

<a name="winui"></a>
## WinUI

This section also applies to [Uno Platform](https://platform.uno/) applications, which use the same XAML and the same _Tlumach.WinUI_ assembly.

In WinUI, translations are referenced in the XAML code as follows:

```xml
<Window x:Class="YourNamespace.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:Tlumach.Sample.WinUI"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:tru="using:ValueFromGeneratedNamespaceSetting"
...
        <TextBlock Text="{x:Bind tru:ValueFromGeneratedClass.TranslationUnitKey.CurrentValue, Mode=OneWay}"/>
```
where
* **ValueFromGeneratedNamespaceSetting** is the namespace which you specified in the `generatedNamespace` configuration setting in your [configuration file](config-file.md),
* **ValueFromGeneratedClass** is the class name which you specified in the `generatedClass` configuration setting in your [configuration file](config-file.md),
* **TranslationUnitKey** is the key of the translation unit to bind the property to.

In the WinUI sample project, this code looks like this:
```xml
<Window x:Class="Tlumach.Sample.WinUI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:Tlumach.Sample.WinUI"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:tru="using:Tlumach.Sample"
...
        <TextBlock Text="{x:Bind tru:Strings.Hello.CurrentValue, Mode=OneWay}" />
```

<a name="uno"></a>
### Uno Platform

Uno Platform does not track changes in `x:Bind` paths that start with a static member: a binding like `{x:Bind tru:Strings.Hello.CurrentValue, Mode=OneWay}` shows the initial text but is not updated when the language changes. Expose the translation units through instance properties of the page (or of a view model) and bind to these properties instead:

```c#
public sealed partial class MainPage : Page
{
    public Tlumach.WinUI.TranslationUnit Hello => Strings.Hello;
    ...
}
```

```xml
<TextBlock Text="{x:Bind Hello.CurrentValue, Mode=OneWay}" />
```

This form works in WinUI too, so it can be used in the code shared between WinUI and Uno projects. See the Uno sample project (_samples/Tlumach.Sample.Uno_) for a complete example.

<a name="avalonia"></a>
## Avalonia

In Avalonia, translations are referenced in the XAML code as follows:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        xmlns:tlumach="clr-namespace:Tlumach.Avalonia;assembly=Tlumach.Avalonia"
        xmlns:translations="clr-namespace:ValueFromGeneratedNamespaceSetting;assembly=YourProjectWithTranslations"
...
        <TextBlock Text="{tlumach:Translate {x:Static translations:ValueFromGeneratedClass.TranslationUnitKey}}"/>
```

where
* **ValueFromGeneratedNamespaceSetting** is the namespace which you specified in the `generatedNamespace` configuration setting in your [configuration file](config-file.md),
* **YourProjectWithTranslations** is the name of your [project with translations](generator.md#TranslationProject),
* **ValueFromGeneratedClass** is the class name which you specified in the `generatedClass` configuration setting in your [configuration file](config-file.md),
* **TranslationUnitKey** is the key of the translation unit to bind the property to.

In the Avalonia sample project, this code looks like this:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d" d:DesignWidth="800" d:DesignHeight="450"
        xmlns:tlumach="clr-namespace:Tlumach.Avalonia;assembly=Tlumach.Avalonia"
        xmlns:translations="clr-namespace:Tlumach.Sample;assembly=Tlumach.Sample.Avalonia.Translation"
        x:Class="Tlumach.Sample.Avalonia.MainWindow"
...
        <TextBlock Text="{tlumach:Translate {x:Static translations:Strings.Hello}}"/>
```

<a name="maui"></a>
## MAUI

In MAUI, translations are referenced in the XAML code as follows:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:tlumach="clr-namespace:Tlumach.MAUI;assembly=Tlumach.MAUI"
             xmlns:translations="clr-namespace:ValueFromGeneratedNamespaceSetting;assembly=YourProjectWithTranslations"
...
            <Label Text="{tlumach:Translate {x:Static translations:ValueFromGeneratedClass.TranslationUnitKey}}"/>
```

where
* **ValueFromGeneratedNamespaceSetting** is the namespace which you specified in the `generatedNamespace` configuration setting in your [configuration file](config-file.md),
* **YourProjectWithTranslations** is the name of your [project with translations](generator.md#TranslationProject),
* **ValueFromGeneratedClass** is the class name which you specified in the `generatedClass` configuration setting in your [configuration file](config-file.md),
* **TranslationUnitKey** is the key of the translation unit to bind the property to.

In the MAUI sample project, this code looks like this:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:tlumach="clr-namespace:Tlumach.MAUI;assembly=Tlumach.MAUI"
             xmlns:translations="clr-namespace:Tlumach.Sample;assembly=Tlumach.Sample.Translation"
...
            <Label Text="{tlumach:Translate {x:Static translations:Strings.Hello}}"/>
```

<a name="maui-binding"></a>
### Taking the translation unit from a binding

As in WPF, the translation unit can come from a binding:

```xml
<Label Text="{tlumach:Translate {Binding StatusText}}" />
<Label Text="{tlumach:Translate Unit={Binding StatusText}}" />
```

where `StatusText` is a property of the binding context whose value is a `TranslationUnit`. Both forms are equivalent. The binding is resolved for the element that displays the text, just as if it were set on that element directly, so it can use the `BindingContext`, a `RelativeSource`, an element referenced with `x:Reference`, or an explicit `Source`, and it works in data templates and in style setters. Compiled bindings (`x:DataType`) work as well. The text follows the changes of the bound property, the language changes, and the calls to <xref:Tlumach.BaseTranslationUnit.NotifyPlaceholdersUpdated>. If the value of the binding is not a `TranslationUnit` (or the binding cannot be resolved), the property receives an empty string.

`Translate` subscribes to the events of the translation manager and of the units weakly, so pages and elements that are no longer used are garbage-collected as usual (see [Lifetime of the bound elements](#wpf-lifetime) in the WPF section). The texts are updated on the UI thread also when the language changes in a background thread.

**Note** on versions before 2.0: there, `Translate` was a `BindableObject` with a bindable `Unit` property. The binding was applied to `Translate` itself, which was not a part of the visual tree and had no binding context, so only the bindings with an explicit `Source` (or `x:Reference`) worked; a binding to the `BindingContext` produced an empty text, and a `RelativeSource` binding failed when the XAML was loaded. Since 2.0, `Unit` is a plain property that receives the binding itself, and `Translate.UnitProperty` no longer exists.
