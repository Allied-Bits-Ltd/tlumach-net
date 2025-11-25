# XAML integration

Tlumach shines when it comes to supporting UIs. You can bind XAML controls of WPF, UWP, WinUI, MAUI, or Avalonia to [generated translation units](glossary.md), which gives you automatic syntax checks and automatic UI updates on language switching.

The use of Tlumach with the mentioned frameworks is very similar in principles, but each framework requires slightly different syntax.

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
             <TextBlock Text="{tlumach:Translate {x:Static translations:ValueFromGeneratedClass.TranslationUnitKey}}"/>
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
