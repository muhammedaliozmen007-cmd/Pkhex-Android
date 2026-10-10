// SPDX-License-Identifier: GPL-3.0-or-later
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Android.Text;
using Android.Widget;
using PKHeX.Android.Updates;
using System.Security.Cryptography;
using System.Text.Json;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;

namespace PKHeX.Android.App;

public sealed partial class MainActivity
{
    private const int InstallPermissionRequest = 310;
    private bool updateBusy;
    private CancellationTokenSource? updateCancellation;
    private PackageInfo? installedPackage;
    private static PackageInfoFlags SigningFlags => OperatingSystem.IsAndroidVersionAtLeast(28) ? PackageInfoFlags.SigningCertificates : PackageInfoFlags.Signatures;
    private PackageInfo InstalledPackage => installedPackage ??= PackageManager!.GetPackageInfo(PackageName!, SigningFlags)
        ?? throw new InvalidOperationException("Installierte App-Version konnte nicht gelesen werden.");
    private long InstalledVersion => VersionCodeOf(InstalledPackage);
    private string UpdateApkPath => UpdateApkProvider.ApkPath(this);
    private string UpdateMetadataPath => Path.Combine(FilesDir!.AbsolutePath, "updates", "latest.json");
    private bool ActiveUi => !IsFinishing && !IsDestroyed;

    private static long VersionCodeOf(PackageInfo info) => OperatingSystem.IsAndroidVersionAtLeast(28) ? info.LongVersionCode : info.VersionCode;

    private string UpdateRepository()
    {
        var saved = GetSharedPreferences("updates", FileCreationMode.Private)!.GetString("repository", null);
        if (!string.IsNullOrWhiteSpace(saved)) return saved;
        using var stream = Assets!.Open("update-channel.json");
        using var json = JsonDocument.Parse(stream);
        return json.RootElement.GetProperty("repository").GetString() ?? "";
    }

    private void ConfigureUpdateSource()
    {
        if (updateBusy) return;
        var panel = Column(); panel.SetPadding(D(20), D(10), D(20), D(12));
        Add(Text("Einmal einrichten: das öffentliche GitHub-Repository, das die Android-Versionen dieser App bereitstellt. Danach lädt „Update“ die neueste APK.", 14, true), panel);
        var source = new EditText(this) { Hint = "https://github.com/name/PKHeX-Android", Text = UpdateRepository(), InputType = InputTypes.ClassText | InputTypes.TextVariationUri, TextSize = 14 };
        source.SetSingleLine(true); Add(source, panel);
        var error = Text("", 12); error.SetTextColor(global::Android.Graphics.Color.Rgb(255, 142, 142)); Add(error, panel);
        var dialog = new AlertDialog.Builder(this).SetTitle("Update-Quelle einrichten")!.SetView(panel)!
            .SetNegativeButton("Später", (_, _) => { })!.SetPositiveButton("Speichern & prüfen", (_, _) => { })!.Create()!;
        dialog.Show();
        dialog.GetButton((int)DialogButtonType.Positive)!.Click += (_, _) =>
        {
            try
            {
                var repository = UpdateSource.Parse(source.Text ?? "");
                GetSharedPreferences("updates", FileCreationMode.Private)!.Edit()!.PutString("repository", repository.FullName)!.Apply();
                dialog.Dismiss(); CheckForUpdates();
            }
            catch (ArgumentException ex) { error.Text = ex.Message; }
        };
    }

    private async void CheckForUpdates()
    {
        if (updateBusy) return;
        AvailableUpdate? downloaded = null;
        try
        {
            var configured = UpdateRepository();
            if (string.IsNullOrWhiteSpace(configured)) { ConfigureUpdateSource(); return; }
            var source = UpdateSource.Parse(configured);
            downloaded = DownloadedUpdate(source);
            updateBusy = true; updateCancellation = new(); updateCancellation.CancelAfter(TimeSpan.FromSeconds(40));
            var panel = Column(); panel.SetPadding(D(20), D(15), D(20), D(15));
            panel.AddView(new ProgressBar(this)); Add(Text("Neueste Version wird geprüft …", 14), panel);
            using var progress = new AlertDialog.Builder(this).SetTitle("Update")!.SetView(panel)!.SetCancelable(false)!
                .SetNegativeButton("Abbrechen", (_, _) => updateCancellation?.Cancel())!.Create()!;
            progress.Show();
            AvailableUpdate? latest;
            try
            {
                using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
                latest = await new UpdateClient(http).FindLatestAsync(source, InstalledVersion, updateCancellation.Token);
            }
            finally { if (ActiveUi) progress.Dismiss(); }
            if (!ActiveUi) return;
            if (latest is null)
                new AlertDialog.Builder(this).SetTitle("Du bist auf dem neuesten Stand")!
                    .SetMessage("Installiert: " + InstalledPackage.VersionName)!
                    .SetNeutralButton("Quelle ändern", (_, _) => ConfigureUpdateSource())!.SetPositiveButton("OK", (_, _) => { })?.Show();
            else ShowAvailableUpdate(latest, downloaded?.Manifest.VersionCode == latest.Manifest.VersionCode &&
                downloaded.Manifest.Sha256.Equals(latest.Manifest.Sha256, StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException) { if (ActiveUi) Toast.MakeText(this, "Update-Prüfung abgebrochen oder Zeitlimit erreicht", ToastLength.Short)?.Show(); }
        catch (Exception ex) { if (ActiveUi) UpdateError(ex.Message, downloaded); }
        finally { updateBusy = false; updateCancellation?.Dispose(); updateCancellation = null; }
    }

    private AvailableUpdate? DownloadedUpdate(UpdateSource source)
    {
        try
        {
            if (!File.Exists(UpdateApkPath) || !File.Exists(UpdateMetadataPath)) return null;
            var update = JsonSerializer.Deserialize<AvailableUpdate>(File.ReadAllText(UpdateMetadataPath), UpdateClient.JsonOptions);
            if (update is null || update.Repository != source.FullName || update.Manifest.VersionCode <= InstalledVersion) return null;
            UpdateClient.ValidateManifest(update.Manifest);
            return new FileInfo(UpdateApkPath).Length == update.Manifest.SizeBytes ? update : null;
        }
        catch { return null; }
    }

    private void ShowAvailableUpdate(AvailableUpdate update, bool downloaded)
    {
        var manifest = update.Manifest;
        var notes = manifest.Notes ?? ""; if (notes.Length > 2000) notes = notes[..2000];
        var message = $"Installiert: {InstalledPackage.VersionName}\nNeue Version: {manifest.VersionName}\nDownload: {manifest.SizeBytes / (1024.0 * 1024):F1} MB";
        if (notes.Length > 0) message += "\n\n" + notes;
        if (downloaded) message += "\n\nDie APK ist bereits heruntergeladen.";
        new AlertDialog.Builder(this).SetTitle(downloaded ? "Update bereit" : "Neue Version verfügbar")!.SetMessage(message)!
            .SetNegativeButton("Später", (_, _) => { })!.SetNeutralButton("Quelle ändern", (_, _) => ConfigureUpdateSource())!
            .SetPositiveButton(downloaded ? "Installieren" : "Herunterladen", (_, _) =>
            {
                if (downloaded) InstallDownloadedUpdate(update); else DownloadUpdate(update);
            })?.Show();
    }

    private async void DownloadUpdate(AvailableUpdate update)
    {
        if (updateBusy) return;
        updateBusy = true; updateCancellation = new(); updateCancellation.CancelAfter(TimeSpan.FromMinutes(20));
        var panel = Column(); panel.SetPadding(D(20), D(15), D(20), D(15));
        var bar = new ProgressBar(this, null, global::Android.Resource.Attribute.ProgressBarStyleHorizontal) { Max = 100, Indeterminate = false };
        Add(bar, panel); var status = Text("Download startet …", 14); Add(status, panel);
        using var dialog = new AlertDialog.Builder(this).SetTitle("Update herunterladen")!.SetView(panel)!.SetCancelable(false)!
            .SetNegativeButton("Abbrechen", (_, _) => updateCancellation?.Cancel())!.Create()!;
        dialog.Show(); var succeeded = false;
        try
        {
            using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            var progress = new Progress<DownloadProgress>(value =>
            {
                if (!ActiveUi || dialog.Handle == IntPtr.Zero || !dialog.IsShowing) return;
                bar.Progress = value.Percent;
                status.Text = $"{value.Percent} % · {value.Received / (1024.0 * 1024):F1} / {value.Total / (1024.0 * 1024):F1} MB";
            });
            await new UpdateClient(http).DownloadAsync(update, UpdateApkPath, progress, updateCancellation.Token);
            status.Text = "APK wird geprüft …"; bar.Indeterminate = true;
            await Task.Run(() => ValidateUpdateApk(update));
            File.WriteAllText(UpdateMetadataPath, JsonSerializer.Serialize(update, UpdateClient.JsonOptions));
            succeeded = true;
        }
        catch (OperationCanceledException) { if (ActiveUi) Toast.MakeText(this, "Download abgebrochen. Du kannst ihn erneut starten.", ToastLength.Short)?.Show(); }
        catch (Exception ex) { if (ActiveUi) UpdateError(ex.Message); }
        finally
        {
            if (ActiveUi) dialog.Dismiss();
            updateBusy = false; updateCancellation?.Dispose(); updateCancellation = null;
        }
        if (succeeded && ActiveUi) InstallDownloadedUpdate(update);
    }

    private void ValidateUpdateApk(AvailableUpdate update)
    {
        var archive = PackageManager!.GetPackageArchiveInfo(UpdateApkPath, SigningFlags)
            ?? throw new InvalidDataException("Die heruntergeladene Datei ist keine gültige Android-APK.");
        if (archive.PackageName != PackageName || VersionCodeOf(archive) != update.Manifest.VersionCode ||
            VersionCodeOf(archive) <= InstalledVersion || archive.VersionName != update.Manifest.VersionName)
            throw new InvalidDataException("Diese APK passt nicht zur App oder ihrer angekündigten Version.");
        static string[] Signatures(PackageInfo info)
        {
            var signatures = OperatingSystem.IsAndroidVersionAtLeast(28) ? info.SigningInfo?.GetApkContentsSigners() : info.Signatures;
            return (signatures ?? []).Select(s => Convert.ToHexString(SHA256.HashData(s.ToByteArray()!))).Order().ToArray();
        }
        var currentSignatures = Signatures(InstalledPackage);
        if (currentSignatures.Length == 0 || !currentSignatures.SequenceEqual(Signatures(archive)))
            throw new InvalidDataException("Diese APK ist mit einem anderen Schlüssel signiert und kann die installierte App nicht aktualisieren.");
        using var stream = File.OpenRead(UpdateApkPath);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(stream), Convert.FromHexString(update.Manifest.Sha256)))
            throw new InvalidDataException("Die APK-Prüfsumme stimmt nicht. Bitte erneut herunterladen.");
    }

    private async void InstallDownloadedUpdate(AvailableUpdate update)
    {
        if (updateBusy) return;
        updateBusy = true;
        try
        {
            await Task.Run(() => ValidateUpdateApk(update));
            if (!ActiveUi) return;
            if (!PackageManager!.CanRequestPackageInstalls())
            {
                new AlertDialog.Builder(this).SetTitle("Update installieren")!
                    .SetMessage("Erlaube PKHeX Android einmalig „Unbekannte Apps installieren“. Danach zeigt Android die Installation des Updates an.")!
                    .SetNegativeButton("Später", (_, _) => { })!
                    .SetPositiveButton("Einstellungen öffnen", (_, _) => Safe(() => StartActivityForResult(
                        new Intent(Settings.ActionManageUnknownAppSources, Uri.Parse("package:" + PackageName)), InstallPermissionRequest)))?.Show();
                return;
            }
            LaunchUpdateInstaller();
        }
        catch (Exception ex) { if (ActiveUi) UpdateError(ex.Message); }
        finally { updateBusy = false; }
    }

    private void LaunchUpdateInstaller()
    {
        Persist();
        var uri = Uri.Parse("content://" + UpdateApkProvider.Authority + "/apk/latest.apk")!;
        var intent = new Intent(Intent.ActionView); intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.ClipData = ClipData.NewRawUri("PKHeX Android Update", uri);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        StartActivity(intent);
    }

    private bool HandleUpdateActivityResult(int requestCode)
    {
        if (requestCode != InstallPermissionRequest) return false;
        Safe(() =>
        {
            if (!PackageManager!.CanRequestPackageInstalls()) { Toast.MakeText(this, "Installation nicht freigegeben. Die APK bleibt für später verfügbar.", ToastLength.Long)?.Show(); return; }
            var source = UpdateSource.Parse(UpdateRepository());
            if (DownloadedUpdate(source) is { } update) InstallDownloadedUpdate(update);
        });
        return true;
    }

    private void UpdateError(string message, AvailableUpdate? downloaded = null)
    {
        var builder = new AlertDialog.Builder(this).SetTitle("Update nicht möglich")!
            .SetMessage(message + "\n\nFür Updates wird eine Internetverbindung benötigt. Deine Spielstände bleiben auf dem Gerät.")!
            .SetNeutralButton("Quelle ändern", (_, _) => ConfigureUpdateSource())!.SetPositiveButton("OK", (_, _) => { })!;
        if (downloaded is not null) builder.SetNegativeButton("APK installieren", (_, _) => InstallDownloadedUpdate(downloaded));
        builder.Show();
    }
}
