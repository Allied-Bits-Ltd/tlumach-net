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
/// Draws TlumachTemplate.png, the icon that the Add New Item dialog shows for every Tlumach item
/// template.
/// <para>
/// It is the only generated icon of the extension.  The icons of the commands in the project and
/// solution context menus are hand-made artwork in the Tlumach teal and orange, committed as they
/// are; this tool neither reads nor writes them.
/// </para>
/// </summary>
internal static class Program
{
    /// <summary>
    /// The tone of the frame.
    /// <para>
    /// The icon of a template is a mid gray rather than the near-black of a themed Visual Studio
    /// icon, because it is never re-themed: a .vstemplate names its icon through the
    /// <c>&lt;Icon&gt;</c> element, which the dialog draws as a plain image file, with no image
    /// moniker and therefore no color inversion on a dark background.  The one image has to be
    /// legible on both the light and the dark dialog background, which rules out near-black as
    /// well as white.
    /// </para>
    /// </summary>
    private static readonly Color Structure = ColorTranslator.FromHtml("#8C8C8C");

    /// <summary>The tone of the letter, a step darker than the frame so that it leads.</summary>
    private static readonly Color Subject = ColorTranslator.FromHtml("#737373");

    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="args">
    /// Optionally <c>--out &lt;directory&gt;</c> to write elsewhere than the Resources directory of
    /// the extension.
    /// </param>
    /// <returns>Zero on success, or one when the arguments are not usable.</returns>
    private static int Main(string[] args)
    {
        string outputDirectory = GetDefaultOutputDirectory();

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--out", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                outputDirectory = args[++i];
            }
            else
            {
                Console.Error.WriteLine($"Unrecognized argument '{args[i]}'.");
                Console.Error.WriteLine("Usage: dotnet run [--out <directory>]");
                return 1;
            }
        }

        Directory.CreateDirectory(outputDirectory);

        string destination = Path.Combine(outputDirectory, "TlumachTemplate.png");
        DrawTemplateIcon(destination, Structure, Subject);
        Console.WriteLine($"drew {destination}");

        return 0;
    }

    /// <summary>
    /// Reads the output directory that the project file baked into the assembly, so that the tool
    /// can be started from any working directory.
    /// </summary>
    /// <returns>The directory, as an absolute path.</returns>
    private static string GetDefaultOutputDirectory()
    {
        string? value = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, "OutputDirectory", StringComparison.Ordinal))
            ?.Value;

        return Path.GetFullPath(value ?? ".");
    }

    /// <summary>
    /// Draws the Tlumach mark: a rounded frame around a lowercase letter "a".
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
