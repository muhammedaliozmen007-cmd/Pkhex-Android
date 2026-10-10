// SPDX-License-Identifier: GPL-3.0-or-later
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PKHeX.Android.Updates;

public static class AppIdentity
{
    public const string PackageId = "org.pkhex.android.community";
    public const long MaximumApkBytes = 500L * 1024 * 1024;
}

public sealed record UpdateSource(string Owner, string Repository)
{
    public string FullName => Owner + "/" + Repository;
    public string LatestReleaseUrl => "https://api.github.com/repos/" + FullName + "/releases/latest";

    public static UpdateSource Parse(string value)
    {
        value = value.Trim().TrimEnd('/');
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Host != "github.com" ||
                !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
                throw new ArgumentException("Bitte eine GitHub-Repository-Adresse eingeben: https://github.com/name/repository");
            value = uri.AbsolutePath.Trim('/');
        }
        var parts = value.Split('/');
        if (parts.Length != 2 || !Regex.IsMatch(parts[0], @"^[A-Za-z0-9][A-Za-z0-9-]{0,38}$") ||
            !Regex.IsMatch(parts[1], @"^[A-Za-z0-9_.-]{1,100}$") || parts[1] is "." or "..")
            throw new ArgumentException("Bitte die Repository-URL oder name/repository eingeben.");
        var repository = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        if (repository.Length == 0 || repository is "." or "..") throw new ArgumentException("Bitte einen gültigen Repository-Namen eingeben.");
        return new(parts[0], repository);
    }

    internal bool IsReleaseAssetUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" && uri.Host == "github.com" && uri.IsDefaultPort && uri.UserInfo.Length == 0 &&
        uri.AbsolutePath.StartsWith("/" + FullName + "/releases/download/", StringComparison.OrdinalIgnoreCase);
}

public sealed record UpdateManifest(int Schema, string ApplicationId, long VersionCode, string VersionName,
    string ApkFile, long SizeBytes, string Sha256, string Notes = "");

public sealed record AvailableUpdate(UpdateManifest Manifest, string DownloadUrl, string Repository);
public sealed record DownloadProgress(long Received, long Total)
{
    public int Percent => Total > 0 ? (int)Math.Min(100, Received * 100 / Total) : 0;
}

/// <summary>Public GitHub Releases only. No accounts, save files or personal data are sent.</summary>
public sealed class UpdateClient(HttpClient http)
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private HttpRequestMessage Request(string url)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.UserAgent.ParseAdd("PKHeX-Android-Updater/1.0");
        message.Headers.Accept.ParseAdd("application/vnd.github+json");
        message.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return message;
    }

    public async Task<AvailableUpdate?> FindLatestAsync(UpdateSource source, long currentVersion, CancellationToken cancel = default)
    {
        using var request = Request(source.LatestReleaseUrl);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Für dieses Repository gibt es noch keine veröffentlichte Android-Version. Die APK und update.json müssen als GitHub-Release bereitgestellt werden.");
        CheckStatus(response);
        using var release = JsonDocument.Parse(await ReadSmallAsync(response, 2 * 1024 * 1024, cancel).ConfigureAwait(false));
        var root = release.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("Die Update-Quelle enthält keine freigegebene stabile Version.");
        var assets = root.GetProperty("assets").EnumerateArray().ToArray();
        var manifests = assets.Where(a => a.GetProperty("name").GetString() == "update.json").ToArray();
        if (manifests.Length != 1) throw new InvalidDataException("In diesem Release fehlt die Android-Updatedatei update.json.");
        var manifestUrl = manifests[0].GetProperty("browser_download_url").GetString()!;
        if (!source.IsReleaseAssetUrl(manifestUrl)) throw new InvalidDataException("Die Update-Adresse passt nicht zum ausgewählten Repository.");
        using var manifestRequest = Request(manifestUrl);
        using var manifestResponse = await http.SendAsync(manifestRequest, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
        CheckStatus(manifestResponse);
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(await ReadSmallAsync(manifestResponse, 32 * 1024, cancel).ConfigureAwait(false), JsonOptions)
            ?? throw new InvalidDataException("Die Updatedatei ist leer.");
        ValidateManifest(manifest);
        var apks = assets.Where(a => a.GetProperty("name").GetString() == manifest.ApkFile).ToArray();
        if (apks.Length != 1 || apks[0].GetProperty("size").GetInt64() != manifest.SizeBytes)
            throw new InvalidDataException("Die APK fehlt oder ihre Größe stimmt nicht mit der Updatedatei überein.");
        var url = apks[0].GetProperty("browser_download_url").GetString()!;
        if (!source.IsReleaseAssetUrl(url)) throw new InvalidDataException("Die APK-Adresse passt nicht zum ausgewählten Repository.");
        if (apks[0].TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String &&
            digest.GetString() is { Length: > 0 } hash && !hash.Equals("sha256:" + manifest.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Die APK-Prüfsummen der Update-Quelle stimmen nicht überein.");
        return manifest.VersionCode > currentVersion ? new(manifest, url, source.FullName) : null;
    }

    public static void ValidateManifest(UpdateManifest manifest)
    {
        if (manifest.Schema != 1 || manifest.ApplicationId != AppIdentity.PackageId || manifest.VersionCode < 1 ||
            string.IsNullOrWhiteSpace(manifest.VersionName) || manifest.VersionName.Length > 32 ||
            string.IsNullOrEmpty(manifest.ApkFile) || !Regex.IsMatch(manifest.ApkFile, @"^[A-Za-z0-9][A-Za-z0-9._-]{0,140}\.apk$", RegexOptions.IgnoreCase) ||
            manifest.SizeBytes is <= 0 or > AppIdentity.MaximumApkBytes ||
            string.IsNullOrEmpty(manifest.Sha256) || !Regex.IsMatch(manifest.Sha256, @"^[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("Die Updatedatei ist ungültig oder gehört zu einer anderen App.");
    }

    public async Task DownloadAsync(AvailableUpdate update, string destination, IProgress<DownloadProgress>? progress = null, CancellationToken cancel = default)
    {
        ValidateManifest(update.Manifest);
        if (!UpdateSource.Parse(update.Repository).IsReleaseAssetUrl(update.DownloadUrl))
            throw new InvalidDataException("Ungültige APK-Download-Adresse.");
        var partial = destination + ".part";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        try
        {
            using var request = Request(update.DownloadUrl);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
            CheckStatus(response);
            if (response.Content.Headers.ContentLength is long length && length != update.Manifest.SizeBytes)
                throw new InvalidDataException("Die Download-Größe stimmt nicht mit der APK überein.");
            await using var source = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long received = 0, lastReport = 0;
            var buffer = new byte[128 * 1024];
            await using (var file = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                while (true)
                {
                    var count = await source.ReadAsync(buffer, cancel).ConfigureAwait(false);
                    if (count == 0) break;
                    received += count;
                    if (received > update.Manifest.SizeBytes) throw new InvalidDataException("Der Download ist größer als die angekündigte APK.");
                    hash.AppendData(buffer.AsSpan(0, count));
                    await file.WriteAsync(buffer.AsMemory(0, count), cancel).ConfigureAwait(false);
                    var now = Environment.TickCount64;
                    if (now - lastReport >= 200) { progress?.Report(new(received, update.Manifest.SizeBytes)); lastReport = now; }
                }
                await file.FlushAsync(cancel).ConfigureAwait(false);
            }
            if (received != update.Manifest.SizeBytes || !CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), Convert.FromHexString(update.Manifest.Sha256)))
                throw new InvalidDataException("Der APK-Download ist unvollständig oder die Prüfsumme stimmt nicht. Bitte erneut herunterladen.");
            cancel.ThrowIfCancellationRequested();
            File.Move(partial, destination, true);
            progress?.Report(new(received, received));
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }

    private static async Task<byte[]> ReadSmallAsync(HttpResponseMessage response, int maximum, CancellationToken cancel)
    {
        if (response.Content.Headers.ContentLength > maximum) throw new InvalidDataException("Die Updatedatei ist zu groß.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
        using var buffer = new MemoryStream(); var block = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(block, cancel).ConfigureAwait(false);
            if (count == 0) return buffer.ToArray();
            if (buffer.Length + count > maximum) throw new InvalidDataException("Die Updatedatei ist zu groß.");
            buffer.Write(block, 0, count);
        }
    }

    private static void CheckStatus(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
            throw new HttpRequestException("GitHub erlaubt gerade keine weitere Update-Abfrage. Bitte später erneut versuchen.");
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri?.Scheme is { } scheme && scheme != "https")
            throw new InvalidDataException("Updates werden nur über HTTPS heruntergeladen.");
    }
}
