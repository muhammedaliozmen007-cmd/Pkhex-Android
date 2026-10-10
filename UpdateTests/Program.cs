using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PKHeX.Android.Updates;

var passed = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; }
async Task Reject(Func<Task> action, string name)
{
    try { await action(); } catch (Exception ex) when (ex is InvalidDataException or ArgumentException or HttpRequestException or InvalidOperationException or OperationCanceledException) { passed++; return; }
    throw new Exception("Expected rejection: " + name);
}
const string repository = "tester/PKHeX-Android";
const string prefix = "https://github.com/" + repository + "/releases/download/v0.3.1/";
var source = UpdateSource.Parse(repository);
var data = Enumerable.Range(0, 300_000).Select(i => (byte)(i * 17)).ToArray();
var sha = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
var manifest = new UpdateManifest(1, AppIdentity.PackageId, 4, "0.3.1", "PKHeX-Android-0.3.1.apk", data.Length, sha, "Test update");
var update = new AvailableUpdate(manifest, prefix + manifest.ApkFile, repository);
string Release(bool preview = false, bool includeManifest = true, long? size = null, string? digest = null, string? assetUrl = null) => JsonSerializer.Serialize(new
{
    draft = false, prerelease = preview,
    assets = (includeManifest ? new[] { new { name = "update.json", browser_download_url = prefix + "update.json", size = 512L, digest = (string?)null } } : [])
        .Concat([new { name = manifest.ApkFile, browser_download_url = assetUrl ?? update.DownloadUrl, size = size ?? data.Length, digest = (string?)(digest ?? "sha256:" + sha) }])
});
HttpResponseMessage Response(byte[] bytes, long? length = null)
{
    var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    if (length.HasValue) response.Content.Headers.ContentLength = length;
    return response;
}
HttpClient Client(string? release = null, UpdateManifest? feed = null, byte[]? apk = null, long? contentLength = null) => new(new FakeHandler(request =>
{
    var url = request.RequestUri!.AbsoluteUri;
    if (url == source.LatestReleaseUrl) return Response(Encoding.UTF8.GetBytes(release ?? Release()));
    if (url.EndsWith("/update.json")) return Response(JsonSerializer.SerializeToUtf8Bytes(feed ?? manifest, UpdateClient.JsonOptions));
    return Response(apk ?? data, contentLength);
}));
Check(UpdateSource.Parse("https://github.com/" + repository + "/").FullName == repository, "Repository URL");
Check(UpdateSource.Parse(repository + ".git").FullName == repository, "Git clone URL");
foreach (var bad in new[] { "https://github.com.attacker.test/user/repo", "http://github.com/user/repo", "user/repo/../other", "https://github.com/user/repo?token=secret", "https://user@github.com/user/repo", "https://github.com:444/user/repo", "user/..", "user/.git" })
    await Reject(() => { UpdateSource.Parse(bad); return Task.CompletedTask; }, bad);
using (var http = Client())
{
    var client = new UpdateClient(http);
    Check((await client.FindLatestAsync(source, 3))?.Manifest.VersionCode == 4, "New release available");
    Check(await client.FindLatestAsync(source, 4) is null, "Already latest");
    Check(await client.FindLatestAsync(source, 5) is null, "Never downgrade");
}
foreach (var badRelease in new[] { Release(true), Release(includeManifest: false), Release(size: 1), Release(digest: "sha256:" + new string('0', 64)), Release(assetUrl: "https://github.com/other/repo/releases/download/v1/wrong.apk") })
{
    using var http = Client(badRelease);
    await Reject(() => new UpdateClient(http).FindLatestAsync(source, 3), "Bad release");
}
foreach (var invalid in new[] { manifest with { ApplicationId = "windows.pkhex" }, manifest with { Schema = 2 }, manifest with { ApkFile = "../file.apk" }, manifest with { SizeBytes = AppIdentity.MaximumApkBytes + 1 }, manifest with { Sha256 = "bad" }, manifest with { VersionCode = 0 } })
{
    using var http = Client(feed: invalid);
    await Reject(() => new UpdateClient(http).FindLatestAsync(source, 3), "Bad manifest");
}
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.NotFound))))
    await Reject(() => new UpdateClient(http).FindLatestAsync(source, 3), "No published release");
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.Forbidden))))
    await Reject(() => new UpdateClient(http).FindLatestAsync(source, 3), "Rate limit");

var directory = Path.Combine(Path.GetTempPath(), "pkhex-update-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory); var destination = Path.Combine(directory, "latest.apk");
try
{
    using (var http = Client())
    {
        await new UpdateClient(http).DownloadAsync(update, destination);
        Check(File.ReadAllBytes(destination).SequenceEqual(data), "Complete APK downloaded");
        Check(!File.Exists(destination + ".part"), "No temporary file on success");
    }
    foreach (var scenario in new[] { (update with { Manifest = manifest with { Sha256 = new string('0', 64) } }, data, (long?)null), (update, data[..100], (long?)data.Length), (update, data.Concat(new byte[1]).ToArray(), (long?)data.Length) })
    {
        File.WriteAllText(destination, "previous complete APK");
        using var http = Client(apk: scenario.Item2, contentLength: scenario.Item3);
        await Reject(() => new UpdateClient(http).DownloadAsync(scenario.Item1, destination), "Broken download");
        Check(File.ReadAllText(destination) == "previous complete APK", "Failed download preserves prior APK");
        Check(!File.Exists(destination + ".part"), "Failed partial download removed");
    }
    using (var cancel = new CancellationTokenSource())
    using (var http = Client())
    {
        await Reject(() => new UpdateClient(http).DownloadAsync(update, destination, new InlineProgress(_ => cancel.Cancel()), cancel.Token), "Interrupted download");
        Check(!File.Exists(destination + ".part"), "Cancelled partial removed");
        Check(File.ReadAllText(destination) == "previous complete APK", "Cancelled download preserves prior APK");
    }
    using (var http = Client())
        await Reject(() => new UpdateClient(http).DownloadAsync(update with { DownloadUrl = "https://github.com/other/repo/releases/download/v1/app.apk" }, destination), "Cross-repository download");
}
finally { Directory.Delete(directory, true); }
Console.WriteLine($"{passed} Update-Prüfungen bestanden.");

sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handle) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); var response = handle(request); response.RequestMessage = request; return Task.FromResult(response);
    }
}
sealed class InlineProgress(Action<DownloadProgress> action) : IProgress<DownloadProgress>
{
    public void Report(DownloadProgress value) => action(value);
}
