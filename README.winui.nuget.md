AlliedBits.Tlumach.WinUI integrates [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) with WinUI 3 (Windows App SDK) and [Uno Platform](https://platform.uno/).

* Its `TranslationUnit` class is used by the generated translation classes and is bound to in XAML. When the language is switched from any thread, the units post their change notifications to the UI thread, so the UI is updated.
* The package works for all targets of an Uno application (Android, iOS, macOS, Linux, WebAssembly, and Windows).
* The application chooses its own version of Windows App SDK or Uno Platform; the package does not depend on either.

Requires .NET 9 or later. See the Getting Started section of the [documentation](https://alliedbits.com/tlumach).
