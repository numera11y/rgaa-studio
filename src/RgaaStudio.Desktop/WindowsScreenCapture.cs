using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RgaaStudio.Desktop;

internal static class WindowsScreenCapture
{
    private const uint BiRgb = 0;
    private const uint DibRgbColors = 0;
    private const uint SrcCopy = 0x00CC0020;
    private const uint CaptureBlt = 0x40000000;

    public static byte[] CapturePng(int x, int y, int width, int height)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("La capture native de zone est disponible sous Windows uniquement.");
        if (width is < 1 or > 4096 || height is < 1 or > 4096 || (long)width * height > 8_000_000)
            throw new ArgumentException("La zone de capture est vide ou dépasse la taille maximale.");

        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero) throw new ExternalException("Impossible d’accéder à l’écran.");
        var memoryDc = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;
        try
        {
            memoryDc = CreateCompatibleDC(screenDc);
            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height,
                    Planes = 1,
                    BitCount = 32,
                    Compression = BiRgb,
                    SizeImage = checked((uint)(width * height * 4))
                }
            };
            bitmap = CreateDIBSection(screenDc, ref info, DibRgbColors, out var pixels, IntPtr.Zero, 0);
            if (memoryDc == IntPtr.Zero || bitmap == IntPtr.Zero || pixels == IntPtr.Zero)
                throw new ExternalException("Impossible de créer le tampon de capture.");
            previous = SelectObject(memoryDc, bitmap);
            if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, SrcCopy | CaptureBlt))
                throw new ExternalException("La zone de l’élément n’a pas pu être capturée.");

            var bytes = new byte[checked(width * height * 4)];
            Marshal.Copy(pixels, bytes, 0, bytes.Length);
            for (var index = 3; index < bytes.Length; index += 4) bytes[index] = 255;
            var pin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            using var image = new WriteableBitmap(PixelFormat.Bgra8888, AlphaFormat.Opaque, pin.AddrOfPinnedObject(),
                new PixelSize(width, height), new Vector(96, 96), width * 4);
            pin.Free();
            using var output = new MemoryStream();
            image.Save(output, new PngBitmapEncoderOptions());
            if (output.Length > 8_000_000) throw new InvalidOperationException("La capture dépasse la limite de 8 Mo.");
            return output.ToArray();
        }
        finally
        {
            if (previous != IntPtr.Zero && memoryDc != IntPtr.Zero) SelectObject(memoryDc, previous);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (memoryDc != IntPtr.Zero) DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)] private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool DeleteDC(IntPtr deviceContext);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr CreateDIBSection(IntPtr deviceContext, ref BitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr obj);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
}
