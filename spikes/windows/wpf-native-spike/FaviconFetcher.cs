using System.IO;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media.Imaging;

namespace RgaaAuditor.Windows;

public static class FaviconFetcher
{
    private const int MaxBytes = 64 * 1024;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(4);

    public static async Task<byte[]?> FetchPngAsync(string enteredUrl, CancellationToken cancellationToken = default)
        => await FetchPngAsync(enteredUrl, Dns.GetHostAddressesAsync, CreatePinnedHandler, cancellationToken);

    internal static async Task<byte[]?> FetchPngAsync(string enteredUrl, Func<string, CancellationToken, Task<IPAddress[]>> resolve, Func<Uri, IPAddress[], HttpMessageHandler> handlerFactory, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(enteredUrl, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            !origin.IsDefaultPort || !string.IsNullOrEmpty(origin.UserInfo) || origin.Host.EndsWith(".test", StringComparison.OrdinalIgnoreCase) ||
            origin.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return null;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);
        var requestToken = timeoutSource.Token;
        try
        {
            var addresses = IPAddress.TryParse(origin.Host, out var literal) ? [literal] : await resolve(origin.DnsSafeHost, requestToken);
            var publicAddresses = addresses.Where(IsPublicAddress).Distinct().ToArray();
            if (publicAddresses.Length == 0 || publicAddresses.Length != addresses.Distinct().Count()) return null;
            var requestUri = new UriBuilder(Uri.UriSchemeHttps, origin.Host, 443, "/favicon.ico").Uri;
            using var handler = handlerFactory(requestUri, publicAddresses);
            using var client = new HttpClient(handler) { Timeout = Timeout };
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestToken);
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentLength is > MaxBytes) return null;
            var type = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (type is not ("image/png" or "image/jpeg" or "image/gif" or "image/bmp" or "image/x-icon" or "image/vnd.microsoft.icon")) return null;
            await using var stream = await response.Content.ReadAsStreamAsync(requestToken);
            using var data = new MemoryStream();
            var buffer = new byte[8192];
            while (true) { var count = await stream.ReadAsync(buffer, requestToken); if (count == 0) break; if (data.Length + count > MaxBytes) return null; data.Write(buffer, 0, count); }
            data.Position = 0;
            var decoder = BitmapDecoder.Create(data, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
            var frame = decoder.Frames.FirstOrDefault();
            if (frame is null || frame.PixelWidth is < 1 or > 256 || frame.PixelHeight is < 1 or > 256 || (long)frame.PixelWidth * frame.PixelHeight > 65536) return null;
            var converted = new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            var pixels = new byte[checked(converted.PixelWidth * converted.PixelHeight * 4)];
            converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
            var safeFrame = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(safeFrame)); using var output = new MemoryStream(); png.Save(output); return output.ToArray();
        }
        catch (Exception) { return null; }
    }

    private static HttpMessageHandler CreatePinnedHandler(Uri requestUri, IPAddress[] addresses)
    {
        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false, ConnectTimeout = Timeout };
        handler.ConnectCallback = async (context, token) =>
        {
            if (!context.DnsEndPoint.Host.Equals(requestUri.DnsSafeHost, StringComparison.OrdinalIgnoreCase) || context.DnsEndPoint.Port != 443) throw new HttpRequestException("Unexpected favicon destination.");
            Exception? last = null;
            foreach (var address in addresses)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try { await socket.ConnectAsync(new IPEndPoint(address, 443), token); return new NetworkStream(socket, ownsSocket: true); }
                catch (Exception ex) { socket.Dispose(); last = ex; }
            }
            throw new HttpRequestException("No validated public address was reachable.", last);
        };
        return handler;
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var b = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var a = b[0]; var c = b[1];
            return !(a == 0 || a == 10 || a == 127 || a >= 224 || (a == 100 && c is >= 64 and <= 127) || (a == 169 && c == 254) || (a == 172 && c is >= 16 and <= 31) || (a == 192 && c == 168) || (a == 192 && c == 0) || (a == 192 && c == 88 && b[2] == 99) || (a == 192 && c == 0 && b[2] == 2) || (a == 198 && c is 18 or 19) || (a == 198 && c == 51 && b[2] == 100) || (a == 203 && c == 0 && b[2] == 113));
        }
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return false;
        return b[0] is >= 0x20 and <= 0x3F && !(address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6Loopback) || b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8 || b[0] == 0x20 && b[1] == 0x01 && (b[2] & 0xF8) == 0x00 || b[0] == 0x20 && b[1] == 0x02 || b[0] == 0x3F && b[1] == 0xFF);
    }
}

internal static class FaviconStore
{
    internal static Task<string?> SaveAsync(string auditRoot, string auditId, byte[] validatedPng)
    {
        if (!Guid.TryParseExact(auditId, "N", out _)) return Task.FromResult<string?>(null);
        var dossier = Path.GetFullPath(Path.Combine(auditRoot, auditId));
        var root = Path.GetFullPath(auditRoot) + Path.DirectorySeparatorChar;
        if (!dossier.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return Task.FromResult<string?>(null);
        var manifestPath = Path.Combine(dossier, "manifest.json");
        if (!File.Exists(manifestPath)) return Task.FromResult<string?>(null);
        string? imageTemp = null;
        string? manifestTemp = null;
        try
        {
            // This small local write is intentionally synchronous on the UI dispatcher after the network
            // request completes, so it cannot race the spike's other manifest writes.
            var json = File.ReadAllText(manifestPath);
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("Id", out var id) || id.GetString() != auditId) return Task.FromResult<string?>(null);
            Directory.CreateDirectory(dossier);
            var path = Path.Combine(dossier, "favicon.png");
            imageTemp = Path.Combine(dossier, $".favicon-{Guid.NewGuid():N}.tmp");
            File.WriteAllBytes(imageTemp, validatedPng);
            File.Move(imageTemp, path, overwrite: true);
            imageTemp = null;
            using var output = new MemoryStream();
            using var jsonWriter = new System.Text.Json.Utf8JsonWriter(output, new System.Text.Json.JsonWriterOptions { Indented = true });
            using var source = System.Text.Json.JsonDocument.Parse(json);
            jsonWriter.WriteStartObject();
            foreach (var property in source.RootElement.EnumerateObject())
            {
                if (property.NameEquals("FaviconFile")) continue;
                property.WriteTo(jsonWriter);
            }
            jsonWriter.WriteString("FaviconFile", "favicon.png");
            jsonWriter.WriteEndObject(); jsonWriter.Flush();
            manifestTemp = Path.Combine(dossier, $".manifest-{Guid.NewGuid():N}.tmp");
            File.WriteAllBytes(manifestTemp, output.ToArray());
            File.Move(manifestTemp, manifestPath, overwrite: true);
            manifestTemp = null;
            return Task.FromResult<string?>(path);
        }
        catch (Exception) { return Task.FromResult<string?>(null); }
        finally
        {
            if (imageTemp is not null) { try { File.Delete(imageTemp); } catch (IOException) { } }
            if (manifestTemp is not null) { try { File.Delete(manifestTemp); } catch (IOException) { } }
        }
    }
}

internal static class FaviconWorkflow
{
    internal static Task<byte[]?> FetchIfConsentedAsync(bool consented, string url, Func<string, Task<byte[]?>> fetch)
        => consented ? fetch(url) : Task.FromResult<byte[]?>(null);

    internal static List<AuditListItem> ApplyIconByAuditId(string auditId, string iconPath, IEnumerable<OpenAuditTab> tabs, IEnumerable<AuditListItem> items)
    {
        var tab = tabs.FirstOrDefault(candidate => candidate.Id == auditId);
        if (tab is not null) tab.IconPath = iconPath;
        return items.Select(item => item.Id == auditId ? new AuditListItem(item.Id, item.Title, item.Summary, item.Updated, iconPath) : item).ToList();
    }
}

internal static class FaviconImageLoader
{
    internal static System.Windows.Media.ImageSource? Load(string? fileUri)
    {
        if (!Uri.TryCreate(fileUri, UriKind.Absolute, out var uri) || !uri.IsFile || !File.Exists(uri.LocalPath)) return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile; image.UriSource = uri; image.EndInit();
            image.Freeze(); return image;
        }
        catch (Exception) { return null; }
    }
}
