**Q: May key names be not just identifiers?**

A: While it is technically possible to permit additional characters in key names and convert them to some placeholder characters such as '_' (underscore) for generated classes, there's an open question of compatibility. Some formats (ARB) require key names to be identifiers, some translation tools may rely on this restriction too. You are welcome to post your thoughts to the [Discussions section](https://github.com/Allied-Bits-Ltd/tlumach/discussions) of the repository, and we will consider your case and figure out what solutions are possible.

A special note on the '*' character - a key in the "translations" section in configuration files may have this name - the '*' is used as a short and visible alias to the "other" key used to hold a reference to the "catch-all" translation file.

**Q: Can I have configuration files and translation files in different formats (say .ini and .json respectively)?**

A: Yes, this is automatically supported by the library _given that_ the application calls the Use() method of each parser class it intends to use (IniParser and JsonParser classes for the extensions specified in the question). TranslationManager does not initialize all parsers automatically, but generated classes do call Use() for the parsers they need.

**Q: Can I have support for {put_a_name_here} translation format?**

A: Yes, check how existing parsers are implemented and how they register themselves, then write your own. If you use Generator, remember to add a call to Use to the Generator.InitializeParsers() method or add the "TlumatchGeneratorExtraParsers" property to the project with translations (the generator will pick the value of this property). The property may contain one or more parser class names separated by a semicolon, comma, or space (or all of them together).

Feel free to offer your implementation for inclusion to the library - we will gladly accept the submission given that it meets minimal quality requirements such as commented code and the existence of unit tests.

**Q: TranslationManager does not load translation files from the disk**

A: Remember to set the LoadFromDisk property of TranslationManager. By default, the property is off to increase speed and, possibly, increase security (e.g., if some malformed translation file makes Tlumach crash or hang, loading files from the disk would introduce a new attack vector).
