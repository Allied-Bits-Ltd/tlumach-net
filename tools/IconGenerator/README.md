# Icon generator

Produces the monochrome icons of the Visual Studio extension, in the style of Visual Studio 2026.

```bash
dotnet run --project tools/IconGenerator
```

The tool writes the seven PNG files of `src/Extension.VisualStudio/Resources` and nothing else. The
Tlumach logo in the same directory, `TlumachIcon.png`, is not touched: it identifies the extension
in the marketplace and in the Extensions manager, where a product logo is expected to be in color.

Pass `--out <directory>` to write elsewhere - useful for comparing a change against the icons that
are committed - and `--source <directory>` to read the artwork from elsewhere.

## What comes from where

`source` holds the brand-colored artwork of the three command icons, in the Tlumach teal and orange,
at 16 and 32 pixels. The generator maps the teal onto the tone of the frame and the orange onto the
tone of the letter and of the action mark, keeping the alpha channel, which is where the antialiasing
of these icons lives. The shapes are therefore not redrawn, only recolored.

The artwork is an input and is never written to. Do not point `--source` at the generated icons: gray
has no hue to classify, so a second pass over the output would collapse the two tones into one.

The item template icon has no source artwork. It was a filled tile of four colored quadrants, which
had to be replaced rather than recolored, so `DrawTemplateIcon` draws it: the same rounded frame and
lowercase letter as the rest of the family, on a transparent background.

## Why the two sets of tones differ

The command icons are near-black. They are registered through `TlumachImages.imagemanifest`, and the
`AllowColorInversion` attribute of an `Image` element
[defaults to true](https://learn.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog),
so the Visual Studio image service lightens them on a dark background, the way it does for the
built-in icons. Near-black is what that mechanism expects.

The item template icon is a mid gray, because that mechanism is not available to it. A `.vstemplate`
refers to its icon through the `<Icon>` element, which names a plain image file that the Add New Item
dialog draws as it is; there is no image moniker and no re-theming. The one image therefore has to be
legible on both the light and the dark dialog background, which rules out both near-black and white.

Should the dialog ever start theming template icons, the template icon can move to the same
near-black as the rest.
