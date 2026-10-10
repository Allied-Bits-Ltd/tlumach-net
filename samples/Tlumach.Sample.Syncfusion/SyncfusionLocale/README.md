# Official translations of Syncfusion (optional)

The sample takes the texts of Syncfusion components from the `Syncfusion` group of `Translations/Strings*.json`. The official translations of Syncfusion, from
[syncfusion/blazor-locale](https://github.com/syncfusion/blazor-locale), can provide the texts that the group lacks. They are not included in this repository,
because that repository states no license; download them into this folder:

```bash
curl -fsSL -o SfResources.resx https://raw.githubusercontent.com/syncfusion/blazor-locale/master/src/SfResources.resx
curl -fsSL -o SfResources.de.resx https://raw.githubusercontent.com/syncfusion/blazor-locale/master/src/SfResources.de.resx
```

Restart the sample. `Program.cs` then loads them through `Syncfusion.cfg` and sets them as `FallbackTranslationManager`. The `.resx` files are ignored by git.
