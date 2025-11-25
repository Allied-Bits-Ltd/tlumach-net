# Strings and Translations

The main purpose of Tlumach is to provide localized / translated strings picked from [translation files](files-formats.md) in a handy way.

Each string is identified by a string key; translation files are sets of key-value pair in different [formats](files-formats.md). Each string together with its key makes a _translation unit_.

Keys are case-insensitive and should be valid identifiers in .NET format. A valid identifier must start with a letter or '_' (underscore) which may be followed by a zero or more letters, digits, and underscores:

These are the sample valid keys:
```ini
valid_key=text
_validKey=text
_1_key=text
```

When [Generator](generator.md) is used, keys become variable names, so if you use some analyzer that enforces the style of variable names, be sure that your keys follow that style at least in the [default file](glossary.md).

## Groups of Strings

If a [file format](files-formats.md) supports grouping, translation units can be grouped. When a translation unit belongs to a group, the name of the group becomes a prefix of the key, joined with the key using '.' (dot).

E.g., you can have the "greetings" group in the translation file, with "hello" and "goodbye" entries in it:

```ini
[greetings]
hello=Hello
goodbye=Bye
```

These units can be accessed by using the "greetings.hello" and "greetings.goodbye" keys respectively. When [Generator](generator.md) is used, groups become nested classes in the main class being generated, i.e., "greetings" will be a nested class, and "hello" and "goodbye" will be variables in this class.

Groups may be nested:

```ini
[greetings]
hello=How do you do!
goodbye=Goodbye!

[greetings.Informal]
hello=Hello
goodbye=Bye
```

This configuration will give you the "greetings" class and the "Informal" class inside the "greetings" class. The first two units will be "greetings.hello" and "greetings.goodbye", and the last two will be "greetings.Informal.hello" and "greetings.Informal.goodbye" respectively.

<a name="StringTypes"></a>
## Regular and Templated Strings

If a string is not templated, i.e., does not contain [placeholders](placeholders.md), Tlumach provides this string as is. If you use [generated translation units](glossary.md), you can access the text using the instance of <xref:Tlumach.TranslationUnit> that has the name equal to the key of the needed string. If you work directly with <xref:Tlumach.TranslationManager>, use one of the `GetValue()` methods (<xref:Tlumach.TranslationManager.GetValue(System.String)>, <xref:Tlumach.TranslationManager.GetValue(System.String,System.Globalization.CultureInfo)>, <xref:Tlumach.TranslationManager.GetValue(Tlumach.Base.TranslationConfiguration,System.String,System.Globalization.CultureInfo)>) to obtain an instance of <xref:Tlumach.Base.TranslationEntry> and read the value of the <xref:Tlumach.Base.TranslationEntry.Text> property.

If a string is templated, an application can retrieve it from the translation manager as a template and handle the placeholders as it likes,
or the application can use the capabilities of Tlumach to process [templates](placeholders.md) and fill the template with actual values.

To obtain the template when using [Generator](generator.md), the application can call the <xref:Tlumach.TemplatedTranslationUnit.GetValueAsTemplate(System.Globalization.CultureInfo)> method.

When working directly with <xref:Tlumach.TranslationManager>, the application can call one of the `GetValue()` methods (<xref:Tlumach.TranslationManager.GetValue(System.String)>, <xref:Tlumach.TranslationManager.GetValue(System.String,System.Globalization.CultureInfo)>, <xref:Tlumach.TranslationManager.GetValue(Tlumach.Base.TranslationConfiguration,System.String,System.Globalization.CultureInfo)>) that return an instance of <xref:Tlumach.Base.TranslationEntry> and read the value of the <xref:Tlumach.Base.TranslationEntry.Text> property. To let Tlumach process the template, you can inspect the <xref:Tlumach.Base.TranslationEntry.IsTemplated> property of this entry and if it is `true`, use one of the `ProcessTemplatedValue()` methods (<xref:Tlumach.Base.TranslationEntry.ProcessTemplatedValue(System.Globalization.CultureInfo,Tlumach.Base.TextFormat,System.Collections.Generic.IDictionary{System.String,System.Object})>, <xref:Tlumach.Base.TranslationEntry.ProcessTemplatedValue(System.Globalization.CultureInfo,Tlumach.Base.TextFormat,System.Collections.Specialized.OrderedDictionary)>, <xref:Tlumach.Base.TranslationEntry.ProcessTemplatedValue(System.Globalization.CultureInfo,Tlumach.Base.TextFormat,System.Object)>, <xref:Tlumach.Base.TranslationEntry.ProcessTemplatedValue(System.Globalization.CultureInfo,Tlumach.Base.TextFormat,System.Object[])>) to fill the template with the data.

<a name="locales"></a>
## Current and Other Locales

<xref:Tlumach.TranslationManager> has a <xref:Tlumach.TranslationManager.CurrentCulture> property which is used as a default culture, for which translation units are retrieved.

This <xref:Tlumach.TranslationManager.CurrentCulture> property is useful first of all in UI applications where XAML properties are bound to generated translation units: the text provided to the bound controls is defined by the value of this property.

If you are using one of the methods for retrieval of translation units mentioned above in their overloads that don't include a culture, the value of <xref:Tlumach.TranslationManager.CurrentCulture> is used there too.

To change the language of the UI in your XAML-based project, change the value of <xref:Tlumach.TranslationManager.CurrentCulture>. This will notify the bindings and also cause the <xref:Tlumach.TranslationManager.OnCultureChanged> event to fire.

## Overriding Translation Files

Sometimes, it is necessary to provide a specific phrase regardless of its presence in a translation file. Maybe, the string is completely missing from the translation, or, maybe, your application configuration allows administrators to define some messages for their users.

To address these needs, <xref:Tlumach.TranslationManager> includes several events:
* <xref:Tlumach.TranslationManager.OnTranslationValueNeeded> is fired when TranslationManager receives a request for a translation unit before it attempts to locate the unit in the translation files. You can provide a unit by assigning a value to <xref:Tlumach.TranslationValueEventArgs.Entry>, <xref:Tlumach.TranslationValueEventArgs.Text>, or <xref:Tlumach.TranslationValueEventArgs.EscapedText> properties of the event arguments class (<xref:Tlumach.TranslationValueEventArgs>).
* <xref:Tlumach.TranslationManager.OnTranslationValueFound> is fired when TranslationManager finds the needed translation unit. Your application has a chance to override the value. This can be done by replacing a value in <xref:Tlumach.TranslationValueEventArgs.Entry>, or by assigning a value to <xref:Tlumach.TranslationValueEventArgs.Text> or <xref:Tlumach.TranslationValueEventArgs.EscapedText> properties of the event arguments class (<xref:Tlumach.TranslationValueEventArgs>).
* <xref:Tlumach.TranslationManager.OnTranslationValueNotFound> is fired if TranslationManager does not find the needed translation unit. You can provide a unit by assigning a value to <xref:Tlumach.TranslationValueEventArgs.Entry>, <xref:Tlumach.TranslationValueEventArgs.Text>, or <xref:Tlumach.TranslationValueEventArgs.EscapedText> properties of the event arguments class (<xref:Tlumach.TranslationValueEventArgs>).
