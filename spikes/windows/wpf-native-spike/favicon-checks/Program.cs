using System.IO;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RgaaAuditor.Windows;

var png = MakePng();
var passed = 0;
await Check("fake PNG fetch is decoded and normalized", async () =>
{
    HttpRequestMessage? sent = null;
    var result = await Run("https://public.example/path?q=ignored", Response("image/png", png), request => sent = request);
    Assert(result is { Length: > 0 }, "valid PNG output missing");
    Assert(sent?.RequestUri?.AbsoluteUri == "https://public.example/favicon.ico", "request did not use the fixed same-origin path");
    Assert(!sent!.Headers.Contains("Referer") && !sent.Headers.Contains("Authorization") && !sent.Headers.Contains("Cookie"), "sensitive header present");
});
await Check("404 falls back", async () => Assert(await Run("https://public.example", new HttpResponseMessage(HttpStatusCode.NotFound)) is null, "404 returned bytes"));
await Check("malformed raster falls back", async () => Assert(await Run("https://public.example", Response("image/png", [1, 2, 3])) is null, "malformed image accepted"));
await Check("SVG active content is rejected", async () => Assert(await Run("https://public.example", Response("image/svg+xml", System.Text.Encoding.UTF8.GetBytes("<svg/>"))) is null, "SVG accepted"));
await Check("oversized body falls back", async () => Assert(await Run("https://public.example", Response("image/png", new byte[65 * 1024])) is null, "oversized image accepted"));
await Check("redirect is not followed", async () => Assert(await Run("https://public.example", new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://elsewhere.example/icon.png") } }) is null, "redirect accepted"));
await Check("timeout falls back", async () => Assert(await FaviconFetcher.FetchPngAsync("https://public.example", ResolvePublic, (_, _) => new HangingHandler()) is null, "timeout returned bytes"));
await Check("stalled response body is covered by the end-to-end timeout", async () => Assert(await Run("https://public.example", new HttpResponseMessage(HttpStatusCode.OK) { Content = new StallingContent() }) is null, "stalled response body did not time out"));
await Check("userinfo and custom port are rejected before DNS", async () =>
{
    var calls = 0;
    var resolver = (string host, CancellationToken token) => { calls++; return Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") }); };
    Assert(await FaviconFetcher.FetchPngAsync("https://user:secret@public.example", resolver, (_, _) => new StubHandler(new HttpResponseMessage(HttpStatusCode.OK))) is null, "userinfo accepted");
    Assert(await FaviconFetcher.FetchPngAsync("https://public.example:8443", resolver, (_, _) => new StubHandler(new HttpResponseMessage(HttpStatusCode.OK))) is null, "custom port accepted");
    Assert(calls == 0, "rejected URL triggered DNS");
});
await Check(".test and private/reserved IP ranges are rejected", async () =>
{
    var called = false;
    var resolver = (string host, CancellationToken token) => { called = true; return Task.FromResult(new[] { IPAddress.Parse("10.0.0.1") }); };
    Assert(await FaviconFetcher.FetchPngAsync("https://demo.test", resolver, (_, _) => new StubHandler(new HttpResponseMessage(HttpStatusCode.OK))) is null, ".test accepted");
    Assert(!called, ".test triggered DNS");
    var privateDns = (string host, CancellationToken token) => Task.FromResult(new[] { IPAddress.Parse("192.168.1.20") });
    Assert(await FaviconFetcher.FetchPngAsync("https://rebind.example", privateDns, (_, _) => new StubHandler(new HttpResponseMessage(HttpStatusCode.OK))) is null, "private DNS result accepted");
    foreach (var address in new[] { "0.0.0.0", "10.1.2.3", "100.64.1.2", "127.0.0.1", "169.254.1.2", "172.16.0.1", "192.0.2.1", "192.88.99.1", "192.168.1.1", "198.18.0.1", "198.51.100.1", "203.0.113.1", "224.0.0.1", "240.0.0.1", "::", "::1", "fc00::1", "fe80::1", "ff02::1", "2001:db8::1", "2002:c000:0201::1", "3fff::1" })
        Assert(!FaviconFetcher.IsPublicAddress(IPAddress.Parse(address)), $"special address {address} accepted");
});
await Check("opt-out does not invoke fetch and uses null fallback", async () =>
{
    var calls = 0;
    var result = await FaviconWorkflow.FetchIfConsentedAsync(false, "https://public.example", _ => { calls++; return Task.FromResult<byte[]?>(png); });
    Assert(result is null && calls == 0, "opt-out invoked fetch");
    var fallbackTab = new OpenAuditTab("audit-fallback", "Readable dossier", true, "pack://application:,,,/Assets/Icons/globe.svg");
    Assert(fallbackTab.FaviconImage is null && fallbackTab.Title == "Readable dossier", "globe fallback/text model invalid");
});
await Check("delayed result updates only its captured dossier after tab switch or close", async () =>
{
    var auditA = Guid.NewGuid().ToString("N"); var auditB = Guid.NewGuid().ToString("N");
    var tabs = new ObservableCollection<OpenAuditTab> { new(auditA, "A", true, "pack://application:,,,/Assets/Icons/globe.svg"), new(auditB, "B", false, "pack://application:,,,/Assets/Icons/globe.svg") };
    var list = new List<AuditListItem> { new(auditA, "A", "A summary", "date A", "pack://application:,,,/Assets/Icons/globe.svg"), new(auditB, "B", "B summary", "date B", "pack://application:,,,/Assets/Icons/globe.svg") };
    var delayed = new TaskCompletionSource<byte[]?>();
    var request = FaviconWorkflow.FetchIfConsentedAsync(true, auditA, _ => delayed.Task);
    tabs[0].IsActive = false; tabs[1].IsActive = true; // user switched while A's request is pending
    delayed.SetResult(png); var result = await request; Assert(result is not null, "fake response lost");
    var uri = "file:///temporary/a/favicon.png";
    list = FaviconWorkflow.ApplyIconByAuditId(auditA, uri, tabs, list);
    Assert(tabs[0].FaviconUri == uri && tabs[1].FaviconUri is null, "switch associated A icon with B");
    Assert(list[0].FaviconUri == uri && list[1].FaviconUri is null, "list icon association crossed dossier IDs");
    tabs.RemoveAt(0); // closing A before a separate completion must not attach to B
    list = FaviconWorkflow.ApplyIconByAuditId(auditA, uri, tabs, list);
    Assert(tabs.Count == 1 && tabs[0].Id == auditB && tabs[0].FaviconUri is null, "closed tab completion changed another tab");
});
await Check("WPF templates layer decorative globe and frozen PNG without replacing text names", async () =>
{
    var xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MainWindow.xaml"));
    var xaml = await File.ReadAllTextAsync(xamlPath);
    Assert(xaml.Contains("x:Key=\"FaviconTabTemplate\"") && xaml.Contains("x:Key=\"FaviconAuditCardTemplate\""), "favicon templates missing");
    Assert(xaml.Split("SvgImage Source=/Assets/Icons/globe.svg").Length - 1 >= 2, "globe fallback is not rendered in both templates");
    Assert(xaml.Split("Binding FaviconImage").Length - 1 >= 2, "cached ImageSource is not bound in both templates");
    Assert(xaml.Contains("Width=\"16\" Height=\"16\" Stretch=\"Uniform\"") && xaml.Contains("Width=\"20\" Height=\"20\" Stretch=\"Uniform\""), "icon sizing/contain mode missing");
    Assert(xaml.Split("AutomationProperties.Name=\"\"").Length - 1 >= 4 && xaml.Contains("AutomationProperties.Name=\"{Binding Title}\""), "decorative image or readable tab name contract missing");
});
await Check("validated data is normalized into only its matching dossier", async () =>
{
    var root = Path.Combine(Path.GetTempPath(), "favicon-check-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
    try
    {
        var a = Guid.NewGuid().ToString("N"); var b = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.Combine(root, a)); Directory.CreateDirectory(Path.Combine(root, b));
        await File.WriteAllTextAsync(Path.Combine(root, a, "manifest.json"), Manifest(a));
        await File.WriteAllTextAsync(Path.Combine(root, b, "manifest.json"), Manifest(b));
        var file = await FaviconStore.SaveAsync(root, a, png);
        Assert(file == Path.Combine(root, a, "favicon.png") && File.ReadAllBytes(file).Length > 0, "fixed PNG cache file missing");
        Assert(!File.Exists(Path.Combine(root, b, "favicon.png")), "other dossier received icon");
        using var manifest = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, a, "manifest.json")));
        Assert(manifest.RootElement.GetProperty("FaviconFile").GetString() == "favicon.png", "manifest metadata missing");
        var localUri = new Uri(file!).AbsoluteUri;
        var restored = FaviconImageLoader.Load(localUri);
        Assert(restored is BitmapSource source && source.IsFrozen && source.PixelWidth == 1 && source.PixelHeight == 1, "persisted PNG did not restore as a frozen image");
        var restoredItem = new AuditListItem(a, "A", "summary", "unchanged date", localUri);
        Assert(restoredItem.FaviconImage is BitmapSource restoredSource && restoredSource.IsFrozen, "list model failed cached image reload");
        Assert((await FaviconStore.SaveAsync(root, Guid.NewGuid().ToString("N"), png)) is null, "unknown dossier accepted");
    }
    finally { Directory.Delete(root, true); }
});
Console.WriteLine($"PASS: {passed} deterministic favicon checks; fake HTTP only, no live network.");

async Task<byte[]?> Run(string url, HttpResponseMessage response, Action<HttpRequestMessage>? observe = null)
    => await FaviconFetcher.FetchPngAsync(url, ResolvePublic, (_, _) => new StubHandler(response, observe));
Task<IPAddress[]> ResolvePublic(string host, CancellationToken token) => Task.FromResult(new[] { IPAddress.Parse("8.8.8.8") });
async Task Check(string name, Func<Task> test) { await test(); passed++; Console.WriteLine("PASS: " + name); }
void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
HttpResponseMessage Response(string type, byte[] data) { var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(data) }; response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(type); return response; }
string Manifest(string id) => System.Text.Json.JsonSerializer.Serialize(new { Id = id, SiteUrl = "https://public.example", Reference = "RGAA", Version = "RGAA 4.1.2", Level = "", Title = "Synthetic", UpdatedAt = DateTimeOffset.UnixEpoch });
byte[] MakePng()
{
    var pixels = new byte[] { 0x20, 0x60, 0xA0, 0xFF };
    var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var output = new MemoryStream(); encoder.Save(output); return output.ToArray();
}

sealed class StubHandler(HttpResponseMessage response, Action<HttpRequestMessage>? observe = null) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { observe?.Invoke(request); return Task.FromResult(response); }
}
sealed class HangingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith<HttpResponseMessage>(_ => throw new TaskCanceledException(), cancellationToken);
}
sealed class StallingContent : HttpContent
{
    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new StallingStream());
    protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) => throw new NotSupportedException();
    protected override bool TryComputeLength(out long length) { length = -1; return false; }
}
sealed class StallingStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return 0;
    }
}
