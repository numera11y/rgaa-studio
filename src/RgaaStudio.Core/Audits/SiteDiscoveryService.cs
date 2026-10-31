using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using SkiaSharp;
using Svg.Skia;

namespace RgaaStudio.Core.Audits;

public sealed record SitePageCandidate(string Name, string Url, string Category, bool IsPriority);

public sealed record SiteScanResult(IReadOnlyList<SitePageCandidate> PriorityPages, IReadOnlyList<SitePageCandidate> OtherPages)
{
    public int TotalCount => PriorityPages.Count + OtherPages.Count;
}

public sealed class SiteDiscoveryService
{
    public const int MaximumOtherPages = 25;
    private const int MaximumHtmlBytes = 1024 * 1024;
    private const int MaximumMetadataBytes = 256 * 1024;
    private const int MaximumSitemapUrls = 2000;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] PriorityLabels = ["Accueil", "Contact", "Mentions légales", "Déclaration d’accessibilité", "Plan du site"];

    public Task<SiteScanResult> ScanAsync(string siteUrl, CancellationToken cancellationToken = default) =>
        ScanAsync(siteUrl, excludedUrls: null, MaximumOtherPages, cancellationToken);

    public async Task<SiteScanResult> ScanAsync(
        string siteUrl,
        IReadOnlyCollection<string>? excludedUrls,
        int maximumOtherPages,
        CancellationToken cancellationToken = default)
    {
        var origin = ValidateOrigin(siteUrl);
        var collected = new Dictionary<string, SitePageCandidate>(StringComparer.OrdinalIgnoreCase);

        var home = await TryGetHtmlPageAsync(origin, cancellationToken);
        if (home is not null)
        {
            var title = string.IsNullOrWhiteSpace(home.Title) ? "Accueil" : home.Title;
            AddCandidate(collected, origin, title, "Accueil");
            foreach (var link in home.Links)
            {
                var resolved = ResolveSameOrigin(origin, link.Url);
                if (resolved is null)
                    continue;
                var category = Classify(link.Url, link.Label);
                var candidateName = string.IsNullOrWhiteSpace(link.Label) ? resolved.AbsolutePath : link.Label;
                AddCandidate(collected, resolved, candidateName, category);
            }
        }

        var sitemapUrls = await DiscoverSitemapsAsync(origin, cancellationToken);
        foreach (var url in sitemapUrls.Take(MaximumSitemapUrls))
        {
            var resolved = ResolveSameOrigin(origin, url);
            if (resolved is null)
                continue;
            AddCandidate(collected, resolved, resolved.AbsolutePath.Trim('/').Replace('/', ' '), Classify(url, string.Empty));
        }

        if (excludedUrls is not null)
        {
            foreach (var excludedUrl in excludedUrls)
            {
                if (ResolveSameOrigin(origin, excludedUrl) is { } resolved)
                    collected.Remove(resolved.AbsoluteUri);
            }
        }

        var priority = collected.Values.Where(item => item.IsPriority)
            .OrderBy(item => PriorityOrder(item.Category))
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var other = collected.Values.Where(item => !item.IsPriority)
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(maximumOtherPages, 0, MaximumOtherPages))
            .ToArray();
        return new SiteScanResult(priority, other);
    }

    public async Task<byte[]?> FetchFaviconAsync(string siteUrl, CancellationToken cancellationToken = default)
    {
        var origin = ValidateOrigin(siteUrl);
        try
        {
            var candidates = new List<Uri>();
            var page = await TryGetHtmlPageAsync(origin, cancellationToken);
            if (page is not null)
            {
                foreach (var icon in page.Icons.OrderBy(icon => icon.Priority))
                {
                    if (ResolveSameOrigin(origin, icon.Url) is { } iconUri && !candidates.Contains(iconUri))
                        candidates.Add(iconUri);
                    if (candidates.Count == 8)
                        break;
                }
            }

            var conventionalIcon = new UriBuilder(Uri.UriSchemeHttps, origin.Host, origin.Port, "/favicon.ico").Uri;
            if (!candidates.Contains(conventionalIcon))
                candidates.Add(conventionalIcon);

            foreach (var uri in candidates)
            {
                try
                {
                    var response = await SendAsync(uri, 64 * 1024, cancellationToken);
                    if (response?.StatusCode != HttpStatusCode.OK ||
                        response.ContentType is { } mediaType && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (IsIconContainer(response.Bytes))
                    {
                        if (ExtractPngFromIconContainer(response.Bytes) is { } png)
                            return png;
                        continue;
                    }

                    if (string.Equals(response.ContentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
                    {
                        if (SanitizeSvg(response.Bytes) is { } safeSvg)
                            return safeSvg;
                        continue;
                    }

                    if (IsSupportedImage(response.Bytes))
                        return response.Bytes;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
                catch (HttpRequestException) { }
                catch (IOException) { }
                catch (SocketException) { }
            }
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (HttpRequestException) { return null; }
        catch (IOException) { return null; }
        catch (SocketException) { return null; }
    }

    public static Uri ValidateOrigin(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Saisissez une URL HTTPS valide, sans identifiants ni port personnalisé.", nameof(value));
        return new UriBuilder(Uri.UriSchemeHttps, uri.Host, 443, uri.AbsolutePath).Uri;
    }

    private static async Task<HtmlPage?> TryGetHtmlPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendAsync(uri, MaximumHtmlBytes, cancellationToken);
            if (response is null || response.StatusCode != HttpStatusCode.OK ||
                response.ContentType is { } mediaType && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                return null;

            var html = Encoding.UTF8.GetString(response.Bytes);
            var context = BrowsingContext.New(Configuration.Default);
            using var document = await context.OpenAsync(request => request.Content(html), cancellationToken);
            var links = document.QuerySelectorAll("a[href]")
                .Select(anchor => new PageLink(anchor.GetAttribute("href") ?? string.Empty, NormalizeWhitespace(anchor.TextContent)))
                .Where(link => link.Url.Length > 0)
                .ToArray();
            var icons = document.QuerySelectorAll("link[rel][href]")
                .Select(link => new PageIcon(link.GetAttribute("href") ?? string.Empty, link.GetAttribute("rel") ?? string.Empty))
                .Where(icon => icon.Url.Length > 0 && IsFaviconRelation(icon.Relation))
                .ToArray();
            return new HtmlPage(NormalizeWhitespace(document.Title ?? string.Empty), links, icons);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (HttpRequestException) { return null; }
        catch (IOException) { return null; }
    }

    private static async Task<IReadOnlyList<string>> DiscoverSitemapsAsync(Uri origin, CancellationToken cancellationToken)
    {
        var sitemapUris = new HashSet<Uri> { new(origin, "/sitemap.xml") };
        var robotsUri = new Uri(origin, "/robots.txt");
        try
        {
            var robots = await SendAsync(robotsUri, MaximumMetadataBytes, cancellationToken);
            if (robots?.StatusCode == HttpStatusCode.OK)
            {
                var text = Encoding.UTF8.GetString(robots.Bytes);
                foreach (var line in text.Split('\n'))
                {
                    var separator = line.IndexOf(':');
                    if (separator > 0 && line[..separator].Trim().Equals("Sitemap", StringComparison.OrdinalIgnoreCase) &&
                        ResolveSameOrigin(origin, line[(separator + 1)..].Trim()) is { } sitemap)
                        sitemapUris.Add(sitemap);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        catch (HttpRequestException) { }
        catch (IOException) { }

        var urls = new List<string>();
        foreach (var sitemap in sitemapUris)
        {
            try
            {
                var response = await SendAsync(sitemap, MaximumMetadataBytes, cancellationToken);
                if (response?.StatusCode != HttpStatusCode.OK)
                    continue;
                using var stream = new MemoryStream(response.Bytes);
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumMetadataBytes });
                var document = XDocument.Load(reader);
                urls.AddRange(document.Descendants().Where(element => element.Name.LocalName == "loc")
                    .Select(element => element.Value.Trim()).Where(value => value.Length > 0));
                if (urls.Count >= MaximumSitemapUrls)
                    break;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            catch (HttpRequestException) { }
            catch (IOException) { }
            catch (XmlException) { }
        }
        return urls;
    }

    private static async Task<FetchResponse?> SendAsync(Uri uri, int maxBytes, CancellationToken cancellationToken)
    {
        var addresses = IPAddress.TryParse(uri.Host, out var address) ? [address] : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        var publicAddresses = addresses.Where(IsPublicAddress).Distinct().ToArray();
        if (publicAddresses.Length == 0 || publicAddresses.Length != addresses.Distinct().Count())
            return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false, ConnectTimeout = RequestTimeout };
        handler.ConnectCallback = async (context, token) =>
        {
            if (!context.DnsEndPoint.Host.Equals(uri.DnsSafeHost, StringComparison.OrdinalIgnoreCase) || context.DnsEndPoint.Port != uri.Port)
                throw new HttpRequestException("La destination HTTP ne correspond pas à l’hôte vérifié.");
            Exception? lastError = null;
            foreach (var ip in publicAddresses)
            {
                var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(ip, uri.Port), token);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch (Exception exception) { socket.Dispose(); lastError = exception; }
            }
            throw new HttpRequestException("Aucune adresse publique vérifiée n’a répondu.", lastError);
        };

        using (handler)
        using (var client = new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan })
        using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token))
        {
            if (response.Content.Headers.ContentLength is > 0 && response.Content.Headers.ContentLength > maxBytes)
                return null;

            var bytes = await ReadBoundedAsync(response.Content, maxBytes, timeout.Token);
            return new FetchResponse(response.StatusCode, response.Content.Headers.ContentType?.MediaType, bytes);
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximum, CancellationToken cancellationToken)
    {
        await using var input = await content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                return output.ToArray();
            if (output.Length + read > maximum)
                throw new IOException("La réponse dépasse la taille maximale autorisée.");
            output.Write(buffer, 0, read);
        }
    }

    private static bool IsSupportedImage(byte[] bytes) =>
        bytes.Length >= 8 && (bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ||
                              bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }) ||
                              bytes.AsSpan().StartsWith("GIF8"u8) ||
                              bytes.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0x01, 0x00 }) ||
                              bytes.AsSpan().StartsWith("RIFF"u8) && bytes.AsSpan(8).StartsWith("WEBP"u8));

    private static bool IsIconContainer(byte[] bytes) =>
        bytes.Length >= 6 && BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0, 2)) == 0 &&
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2)) == 1 &&
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4, 2)) > 0;

    private static bool IsSvg(byte[] bytes)
    {
        var start = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        while (start < bytes.Length && bytes[start] is 0x09 or 0x0A or 0x0D or 0x20)
            start++;
        return bytes.AsSpan(start).StartsWith("<svg"u8);
    }

    public static byte[]? ExtractPngFromIconContainer(byte[] bytes)
    {
        if (!IsIconContainer(bytes))
            return null;
        var frameCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4, 2));
        for (var frame = 0; frame < frameCount; frame++)
        {
            var entryOffset = 6 + frame * 16;
            if (entryOffset + 16 > bytes.Length)
                break;
            var frameLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entryOffset + 8, 4));
            var imageOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entryOffset + 12, 4));
            if (frameLength < 8 || imageOffset > int.MaxValue || frameLength > int.MaxValue ||
                (ulong)imageOffset + frameLength > (ulong)bytes.Length)
                continue;
            var image = bytes.AsSpan((int)imageOffset, (int)frameLength);
            if (image.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                return image.ToArray();
        }
        return null;
    }

    public static byte[]? SanitizeSvg(byte[] bytes)
    {
        if (bytes.Length is < 1 or > 64 * 1024)
            return null;

        try
        {
            using var input = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 64 * 1024
            });
            var document = XDocument.Load(reader, LoadOptions.None);
            if (document.Root?.Name.LocalName != "svg")
                return null;

            foreach (var element in document.Root.DescendantsAndSelf())
            {
                if (element.Name.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("foreignObject", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("image", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("feImage", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("iframe", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("object", StringComparison.OrdinalIgnoreCase) ||
                    element.Name.LocalName.Equals("embed", StringComparison.OrdinalIgnoreCase))
                    return null;

                foreach (var attribute in element.Attributes())
                {
                    var name = attribute.Name.LocalName;
                    var value = attribute.Value.Trim();
                    if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("base", StringComparison.OrdinalIgnoreCase))
                        return null;
                    if (name.Equals("href", StringComparison.OrdinalIgnoreCase) && value.Length > 0 && !value.StartsWith('#'))
                        return null;
                    if (ContainsExternalSvgReference(value) ||
                        name.Equals("style", StringComparison.OrdinalIgnoreCase) && value.Contains("@import", StringComparison.OrdinalIgnoreCase))
                        return null;
                }

                if (element.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase) &&
                    (element.Value.Contains("@import", StringComparison.OrdinalIgnoreCase) || ContainsExternalSvgReference(element.Value)))
                    return null;
            }

            var sanitized = Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting));
            return sanitized.Length <= 64 * 1024 ? sanitized : null;
        }
        catch (XmlException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public static byte[]? NormalizeFavicon(byte[] bytes)
    {
        if (bytes.Length is < 8 or > 64 * 1024)
            return null;

        if (IsSvg(bytes))
        {
            var safeSvg = SanitizeSvg(bytes);
            if (safeSvg is null)
                return null;

            try
            {
                using var input = new MemoryStream(safeSvg, writable: false);
                using var svg = new SKSvg();
                var picture = svg.Load(input);
                if (picture is null)
                    return null;
                var bounds = picture.CullRect;
                if (!float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0)
                    return null;
                var scale = Math.Min(256f / bounds.Width, 256f / bounds.Height);
                if (!float.IsFinite(scale) || scale <= 0)
                    return null;
                using var output = new MemoryStream();
                using var colorSpace = SKColorSpace.CreateSrgb();
                return picture.ToImage(output, SKColors.Transparent, SKEncodedImageFormat.Png, 100, scale, scale,
                    SKImageInfo.PlatformColorType, SKAlphaType.Premul, colorSpace)
                    ? output.ToArray()
                    : null;
            }
            catch (Exception) { return null; }
        }

        if (IsIconContainer(bytes))
        {
            bytes = ExtractPngFromIconContainer(bytes) ?? [];
            if (bytes.Length == 0)
                return null;
        }

        try
        {
            using var bitmap = SKBitmap.Decode(bytes);
            if (bitmap is null || bitmap.Width is < 1 or > 256 || bitmap.Height is < 1 or > 256 ||
                (long)bitmap.Width * bitmap.Height > 65_536)
                return null;
            using var image = SKImage.FromBitmap(bitmap);
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            return png?.ToArray();
        }
        catch (Exception) { return null; }
    }

    public static string FormatHtmlSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;
        var document = new HtmlParser().ParseDocument(source);
        var preservedTextNodes = document.QuerySelectorAll("pre,textarea,script,style").Cast<INode>().ToArray();
        var formatter = new PrettyMarkupFormatter(preservedTextNodes)
        {
            Indentation = "  ",
            NewLine = "\n"
        };
        return document.ToHtml(formatter);
    }

    public static string FormatHtmlFragment(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;
        var document = new HtmlParser().ParseDocument("<!doctype html><html><body>" + source + "</body></html>");
        var bodyNodes = document.Body?.ChildNodes;
        if (bodyNodes is null)
            return source;
        var preservedTextNodes = document.QuerySelectorAll("pre,textarea,script,style").Cast<INode>().ToArray();
        var formatter = new PrettyMarkupFormatter(preservedTextNodes)
        {
            Indentation = "  ",
            NewLine = "\n"
        };
        return string.Join("\n", bodyNodes.Select(node => node.ToHtml(formatter))).Trim();
    }

    private static bool ContainsExternalSvgReference(string value)
    {
        if (!value.Contains("url(", StringComparison.OrdinalIgnoreCase))
            return false;
        var internalReferencesRemoved = Regex.Replace(value, "url\\(\\s*(['\\\"]?)#[A-Za-z0-9_.:-]+\\1\\s*\\)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return internalReferencesRemoved.Contains("url(", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var first = bytes[0];
            var second = bytes[1];
            return !(first == 0 || first == 10 || first == 127 || first >= 224 ||
                     first == 100 && second is >= 64 and <= 127 || first == 169 && second == 254 ||
                     first == 172 && second is >= 16 and <= 31 || first == 192 && second == 168 ||
                     first == 192 && second == 0 || first == 198 && second is 18 or 19 ||
                     first == 198 && second == 51 && bytes[2] == 100 || first == 203 && second == 0 && bytes[2] == 113);
        }
        return address.AddressFamily == AddressFamily.InterNetworkV6 && bytes[0] is >= 0x20 and <= 0x3F &&
               !(address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6Loopback) ||
                 bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8 ||
                 bytes[0] == 0x3F && bytes[1] == 0xFF);
    }

    private static Uri? ResolveSameOrigin(Uri origin, string value)
    {
        if (!Uri.TryCreate(origin, value, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) ||
            !uri.Host.Equals(origin.Host, StringComparison.OrdinalIgnoreCase))
            return null;
        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri;
    }

    private static void AddCandidate(IDictionary<string, SitePageCandidate> candidates, Uri uri, string name, string category)
    {
        var key = new UriBuilder(uri) { Fragment = string.Empty }.Uri.AbsoluteUri;
        var priority = category.Length > 0;
        if (!candidates.TryGetValue(key, out var existing) || priority && !existing.IsPriority)
            candidates[key] = new SitePageCandidate(string.IsNullOrWhiteSpace(name) ? uri.AbsolutePath : name, key,
                priority ? category : "Autre page", priority);
    }

    private static string Classify(string value, string label)
    {
        var text = (value + " " + label).Normalize(NormalizationForm.FormD);
        var normalized = new string(text.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        if (normalized.Contains("contact")) return "Contact";
        if (normalized.Contains("mention-legale") || normalized.Contains("mentions-legales") || normalized.Contains("legal-notice")) return "Mentions légales";
        if (normalized.Contains("accessibilite") || normalized.Contains("accessibility")) return "Déclaration d’accessibilité";
        if (normalized.Contains("plan-du-site") || normalized.Contains("sitemap") || normalized.Contains("site-map")) return "Plan du site";
        return string.Empty;
    }

    private static int PriorityOrder(string category) => Array.IndexOf(PriorityLabels, category) is var index && index >= 0 ? index : int.MaxValue;
    private static bool IsFaviconRelation(string relation) => relation.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Any(token => token.Equals("icon", StringComparison.OrdinalIgnoreCase) ||
                      token.Equals("apple-touch-icon", StringComparison.OrdinalIgnoreCase) ||
                      token.Equals("mask-icon", StringComparison.OrdinalIgnoreCase));
    private static string NormalizeWhitespace(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    private sealed record PageLink(string Url, string Label);
    private sealed record PageIcon(string Url, string Relation)
    {
        public int Priority => Relation.Contains("icon", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }
    private sealed record HtmlPage(string Title, IReadOnlyList<PageLink> Links, IReadOnlyList<PageIcon> Icons);
    private sealed record FetchResponse(HttpStatusCode StatusCode, string? ContentType, byte[] Bytes);
}
