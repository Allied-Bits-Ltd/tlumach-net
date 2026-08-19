# Icon generator

Draws `TlumachTemplate.png`, the icon that the **Add New Item** dialog shows for every Tlumach item
template.

```bash
dotnet run --project tools/IconGenerator
```

The tool writes that one file into `src/Extension.VisualStudio/Resources` and touches nothing else.
Pass `--out <directory>` to write elsewhere, which is useful for comparing a change against the icon
that is committed.

## What is and is not generated

Only the template icon is generated. It has no source artwork to start from: it used to be a filled
tile of four colored quadrants, which had to be replaced rather than recolored when it was restyled,
so `DrawTemplateIcon` draws it outright - the rounded frame and lowercase letter of the Tlumach mark,
on a transparent background.

Everything else in `Resources` is hand-made artwork, committed as it is, and this tool neither reads
nor writes it:

- `GoToDef.png`, `RunGen.png`, `RunGenAll.png` and their 16-pixel variants - the icons of the
  commands in the project and solution context menus, in the Tlumach teal and orange.
- `TlumachIcon.png` - the logo that identifies the extension in the marketplace and in the
  Extensions manager.

## Why the template icon is gray while the command icons are in color

The command icons are registered through `TlumachImages.imagemanifest`, and the
`AllowColorInversion` attribute of an `Image` element
[defaults to true](https://learn.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog),
so the Visual Studio image service adapts them to the theme of the shell. They can be in color and
still sit correctly in a menu on either a light or a dark background.

The template icon cannot use that mechanism. A `.vstemplate` refers to its icon through the `<Icon>`
element, which names a plain image file that the Add New Item dialog draws as it is; there is no
image moniker, no theming and no inversion. The single image therefore has to be legible on both the
light and the dark dialog background, which is why it is drawn in two mid grays - `#8C8C8C` for the
frame and `#737373` for the letter - rather than in near-black, in white, or in the brand colors.

Should the dialog ever start theming template icons, this constraint goes away and the icon could
follow the brand colors like the rest.
