using System.Buffers.Binary;
using SkiaSharp;
using Svg.Skia;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: RgaaStudio.IconBuilder <source.svg> <output.ico>");
    return 2;
}

var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (!File.Exists(source))
{
    Console.Error.WriteLine($"Icon SVG not found: {source}");
    return 1;
}

using var svg = new SKSvg();
var picture = svg.Load(source);
if (picture is null || picture.CullRect.Width <= 0 || picture.CullRect.Height <= 0)
{
    Console.Error.WriteLine("The application SVG did not produce a renderable image.");
    return 1;
}

var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
var images = new List<(int Size, byte[] Png)>();
foreach (var size in sizes)
{
    using var bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
    using (var canvas = new SKCanvas(bitmap))
    {
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(size / picture.CullRect.Width, size / picture.CullRect.Height);
        canvas.Translate(-picture.CullRect.Left, -picture.CullRect.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();
    }
    using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        ?? throw new InvalidOperationException($"Failed to encode the {size}px icon frame.");
    images.Add((size, encoded.ToArray()));
}

using var icoStream = new MemoryStream();
using (var writer = new BinaryWriter(icoStream, System.Text.Encoding.UTF8, leaveOpen: true))
{
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write((ushort)images.Count);
    var imageOffset = 6 + images.Count * 16;
    foreach (var image in images)
    {
        writer.Write((byte)(image.Size == 256 ? 0 : image.Size));
        writer.Write((byte)(image.Size == 256 ? 0 : image.Size));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)image.Png.Length);
        writer.Write((uint)imageOffset);
        imageOffset += image.Png.Length;
    }
    foreach (var image in images) writer.Write(image.Png);
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllBytes(output, icoStream.ToArray());
Console.WriteLine($"Generated multi-resolution RGAA Studio application icon: {output}");
return 0;
