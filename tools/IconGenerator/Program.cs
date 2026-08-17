// <copyright file="Program.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;

namespace AlliedBits.Tlumach.Tools.IconGenerator;

/// <summary>
/// Produces the monochrome icons of the Visual Studio extension.
/// <para>
/// The command icons are derived from the brand-colored artwork in <c>source</c> by mapping the
/// two Tlumach colors onto two tones of gray.  The artwork is the input and is never written to,
/// so the tool can be run repeatedly; running it over its own output would not work, because gray
/// has no hue to classify.
/// </para>
/// <para>
/// The item template icon has no source artwork: it used to be a filled tile of four colored
/// quadrants, which had to be replaced rather than recolored, so it is drawn here.
/// </para>
/// </summary>
internal static class Program
{
    /// <summary>
    /// The command icons are registered through TlumachImages.imagemanifest, whose
    /// AllowColorInversion attribute defaults to true, so the Visual Studio image service lightens
    /// them on a dark background.  They are therefore drawn in near-black, as the built-in Visual
    /// Studio icons are, and the frame is kept a tone lighter than the letter and the action mark
    /// so that the subject of each icon leads.
    /// </summary>
    private static readonly Color StructureThemed = ColorTranslator.FromHtml("#5A5A5A");

    /// <summary>The near-black tone of the letter, the arrow and the play head.</summary>
    private static readonly Color SubjectThemed = ColorTranslator.FromHtml("#1F1F1F");

    /// <summary>
    /// The Add New Item dialog draws the Icon element of a .vstemplate as a plain image file and
    /// never re-themes it, so the single template icon has to carry on both the light and the dark
    /// dialog background.  Its tones are therefore mid gray rather than near-black.
    /// </summary>
    private static readonly Color StructureStatic = ColorTranslator.FromHtml("#8C8C8C");

    /// <summary>The mid tone of the letter in the template icon.</summary>
    private static readonly Color SubjectStatic = ColorTranslator.FromHtml("#737373");

    /// <summary>The brand teal lies in this hue range; everything else is the accent orange.</summary>
    private const float StructureHueMinimum = 150f;

    /// <summary>The upper bound of the brand teal hue range.</summary>
    private const float StructureHueMaximum = 220f;

    private static readonly string[] CommandIcons =
    {
        "GoToDef.png", "GoToDef16.png",
        "RunGen.png", "RunGen16.png",
        "RunGenAll.png", "RunGenAll16.png",
    };

    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="args">
    /// Optionally <c>--out &lt;directory&gt;</c> to write elsewhere than the Resources directory of
    /// the extension, and <c>--source &lt;directory&gt;</c> to read the artwork from elsewhere.
    /// </param>
    /// <returns>Zero on success, or one when the arguments or the directories are not usable.</returns>
    private static int Main(string[] args)
    {
        string sourceDirectory = GetDefaultDirectory("SourceDirectory");
        string outputDirectory = GetDefaultDirectory("OutputDirectory");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    outputDirectory = args[++i];
                    break;

                case "--source" when i + 1 < args.Length:
                    sourceDirectory = args[++i];
                    break;

                default:
                    Console.Error.WriteLine($"Unrecognized argument '{args[i]}'.");
                    Console.Error.WriteLine("Usage: dotnet run [--source <directory>] [--out <directory>]");
                    return 1;
            }
        }

        if (!Directory.Exists(sourceDirectory))
        {
            Console.Error.WriteLine($"The artwork directory '{sourceDirectory}' does not exist.");
            return 1;
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (string name in CommandIcons)
        {
            string source = Path.Combine(sourceDirectory, name);
            if (!File.Exists(source))
            {
                Console.Error.WriteLine($"The artwork file '{source}' does not exist.");
                return 1;
            }

            Recolor(source, Path.Combine(outputDirectory, name), StructureThemed, SubjectThemed);
            Console.WriteLine($"recolored {name}");
        }

        DrawTemplateIcon(Path.Combine(outputDirectory, "TlumachTemplate.png"), StructureStatic, SubjectStatic);
        Console.WriteLine("drew      TlumachTemplate.png");

        Console.WriteLine($"written to {outputDirectory}");
        return 0;
    }

    /// <summary>
    /// Reads one of the directories that the project file baked into the assembly.
    /// </summary>
    /// <param name="key">The name of the assembly metadata entry.</param>
    /// <returns>The directory, as an absolute path.</returns>
    private static string GetDefaultDirectory(string key)
    {
        string? value = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.Ordinal))
            ?.Value;

        return Path.GetFullPath(value ?? ".");
    }

    /// <summary>
    /// Maps the brand teal onto the structure tone and the brand orange onto the subject tone.
    /// The alpha channel is kept as it is, because that is where the antialiasing of these icons
    /// lives; only the color channels are replaced, so the shapes survive untouched.
    /// </summary>
    /// <param name="source">The brand-colored artwork to read.</param>
    /// <param name="destination">The monochrome icon to write.</param>
    /// <param name="structure">The tone of the frame.</param>
    /// <param name="subject">The tone of the letter and of the action mark.</param>
    private static void Recolor(string source, string destination, Color structure, Color subject)
    {
        using var src = new Bitmap(source);
        using var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                if (c.A == 0)
                    continue;

                float hue = c.GetHue();
                Color target = hue >= StructureHueMinimum && hue <= StructureHueMaximum ? structure : subject;
                dst.SetPixel(x, y, Color.FromArgb(c.A, target));
            }
        }

        dst.Save(destination, ImageFormat.Png);
    }

    /// <summary>
    /// Draws the mark shared by the family: a rounded frame around a lowercase letter "a".
    /// </summary>
    /// <param name="destination">The icon to write.</param>
    /// <param name="structure">The tone of the frame.</param>
    /// <param name="subject">The tone of the letter.</param>
    private static void DrawTemplateIcon(string destination, Color structure, Color subject)
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        using (var pen = new Pen(structure, 2f) { Alignment = PenAlignment.Center, LineJoin = LineJoin.Round })
        using (GraphicsPath frame = RoundedRectangle(new RectangleF(4.5f, 4.5f, 23f, 23f), 6f))
        {
            g.DrawPath(pen, frame);
        }

        using var family = new FontFamily("Segoe UI");
        using var path = new GraphicsPath();
        path.AddString("a", family, (int)FontStyle.Bold, 21f, new PointF(0f, 0f), StringFormat.GenericTypographic);

        // The glyph is centered on its own ink rather than on its typographic box, so that it sits
        // in the middle of the frame regardless of the metrics of the font.
        RectangleF bounds = path.GetBounds();
        using (var move = new Matrix())
        {
            move.Translate(((32f - bounds.Width) / 2f) - bounds.X, ((32f - bounds.Height) / 2f) - bounds.Y);
            path.Transform(move);
        }

        using (var brush = new SolidBrush(subject))
        {
            g.FillPath(brush, path);
        }

        bmp.Save(destination, ImageFormat.Png);
    }

    /// <summary>
    /// Builds a rounded rectangle.
    /// </summary>
    /// <param name="r">The bounds of the rectangle.</param>
    /// <param name="radius">The radius of the corners.</param>
    /// <returns>The path of the rectangle.</returns>
    private static GraphicsPath RoundedRectangle(RectangleF r, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();

        path.AddArc(r.X, r.Y, d, d, 180f, 90f);
        path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
        path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();

        return path;
    }
}
