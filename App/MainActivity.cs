using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using PKHeX.Core;
using PKHeX.Android.Editor;
using System.Text.Json;
using AColor = Android.Graphics.Color;
using Uri = Android.Net.Uri;
using Path = System.IO.Path;

namespace PKHeX.Android.App;

[Activity(Label = "PKHeX Android", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public sealed partial class MainActivity : Activity
{
    private const int OpenRequest = 100, ImportRequest = 101, ExportRequest = 200;
    private static readonly AColor Background = AColor.Rgb(10, 17, 29), Surface = AColor.Rgb(20, 31, 48),
        Muted = AColor.Rgb(148, 166, 188), Accent = AColor.Rgb(77, 224, 197);
    private EditorSession? session;
    private readonly GameStrings strings = GameInfo.GetStrings("de");
    private LinearLayout root = null!, content = null!;
    private readonly Dictionary<string, EditText> fields = [];
    private readonly Dictionary<string, int> choices = [];
    private CheckBox? nicknamed, shiny;
    private PKM? selected;
    private PKM? creationDraft;
    private int box, slot = -1;
    private bool party, draftDirty, building, busy;
    private EditorSession? persistedSession;
    private object? persistedEntity;
    private string DataPath(string name) => Path.Combine(FilesDir!.AbsolutePath, name);
    private int D(float value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);

    private sealed record StoredSession(string FileName, bool Dirty, int Box, int Slot, bool Party,
        Dictionary<string, string>? Fields, Dictionary<string, int>? Choices, bool Nicknamed, bool Shiny, bool DraftDirty, int EditorTab = 0, bool Demo = false,
        byte[]? CreationDraft = null);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        StoredSession? stored = null;
        try
        {
            if (File.Exists(DataPath("session.json")))
            {
                stored = JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(DataPath("session.json")));
                session = EditorSession.Open(File.ReadAllBytes(DataPath("current.bin")), stored!.FileName);
                session.RestoreOriginal(File.ReadAllBytes(DataPath("original.bin")), stored.Dirty);
                box = stored.Box; slot = stored.Slot; party = stored.Party; editorTab = stored.EditorTab; demo = stored.Demo;
                if (stored.CreationDraft is not null && stored.DraftDirty)
                {
                    try { creationDraft = session.RestoreCreationDraft(stored.CreationDraft, box, slot, party); }
                    catch { creationDraft = null; }
                }
            }
        }
        catch { session = null; slot = -1; creationDraft = null; }
        Render();
        if (stored?.DraftDirty is true && selected is not null)
        {
            building = true;
            if (stored.Choices is not null) foreach (var pair in stored.Choices) choices[pair.Key] = pair.Value;
            if (stored.Fields is not null) foreach (var pair in stored.Fields) if (fields.TryGetValue(pair.Key, out var f)) f.Text = pair.Value;
            if (nicknamed is not null) nicknamed.Checked = stored.Nicknamed;
            if (shiny is not null) shiny.Checked = stored.Shiny;
            building = false; draftDirty = true;
            // Render the selected names as well as their underlying IDs.
            RefreshChoiceLabels();
        }
    }

    protected override void OnPause()
    {
        Persist();
        base.OnPause();
    }

    private void Persist()
    {
        if (session is null) return;
        try
        {
            // Box navigation and tab changes do not rewrite/checksum the entire save.
            var current = (object?)session.Save ?? session.Entity;
            if (!ReferenceEquals(persistedSession, session))
            {
                AtomicWrite("original.bin", session.Original); persistedEntity = null;
            }
            if (!ReferenceEquals(persistedEntity, current))
            {
                AtomicWrite("current.bin", session.Export()); persistedEntity = current;
            }
            persistedSession = session;
            var stored = new StoredSession(session.FileName, session.Dirty, box, slot, party,
                fields.ToDictionary(p => p.Key, p => p.Value.Text ?? ""), new(choices), nicknamed?.Checked ?? false,
                shiny?.Checked ?? false, draftDirty, editorTab, demo,
                creationDraft is null ? null : EditorSession.ExportEntity(creationDraft));
            AtomicWrite("session.json", System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stored)));
        }
        catch (Exception ex) { Toast.MakeText(this, "Sitzung konnte nicht gespeichert werden: " + ex.Message, ToastLength.Long)?.Show(); }
    }

    private void AtomicWrite(string name, byte[] data)
    {
        File.WriteAllBytes(DataPath(name + ".tmp"), data);
        File.Move(DataPath(name + ".tmp"), DataPath(name), true);
    }

    private void Safe(Action action)
    {
        try { action(); } catch (Exception ex) { Error(ex.Message); }
    }
    private void Error(string message) => new AlertDialog.Builder(this).SetTitle("Bitte prüfen")!.SetMessage(message)!.SetPositiveButton("OK", (_, _) => { })?.Show();
    private void Confirm(string title, string message, Action yes) => new AlertDialog.Builder(this).SetTitle(title)!.SetMessage(message)!
        .SetNegativeButton("Abbrechen", (_, _) => { })!.SetPositiveButton("Fortfahren", (_, _) => Safe(yes))?.Show();
    private void Navigate(Action next)
    {
        if (!draftDirty) { next(); return; }
        new AlertDialog.Builder(this).SetTitle(creationDraft is null ? "Pokémon geändert" : "Neues Pokémon")!
            .SetMessage(creationDraft is null ? "Änderungen vor dem Wechsel übernehmen?" : "Neues Pokémon vor dem Wechsel in den freien Platz speichern?")!
            .SetNegativeButton("Abbrechen", (_, _) => { })!
            .SetNeutralButton("Verwerfen", (_, _) => { draftDirty = false; creationDraft = null; Safe(next); })!
            .SetPositiveButton("Übernehmen", (_, _) => Safe(() => { CommitDraft(); next(); }))?.Show();
    }

    private PokemonEdit ReadDraft()
    {
        var pk = selected ?? throw new InvalidOperationException("Kein Pokémon ausgewählt.");
        int Number(string key) => int.TryParse(fields[key].Text, out var n) ? n : throw new ArgumentException("Bitte alle Zahlenfelder gültig ausfüllen.");
        var level = Number("level"); if (level is < 1 or > 100) throw new ArgumentException("Level: 1–100.");
        return new PokemonEdit((ushort)choices["species"], fields["nickname"].Text ?? "", nicknamed!.Checked, (byte)level,
            choices.GetValueOrDefault("item", pk.HeldItem), (Nature)choices.GetValueOrDefault("nature", (int)pk.Nature),
            choices.GetValueOrDefault("ability", 0), shiny!.Checked,
            Enumerable.Range(0, 6).Select(i => Number("iv" + i)).ToArray(),
            Enumerable.Range(0, 6).Select(i => Number("ev" + i)).ToArray(),
            Enumerable.Range(0, 4).Select(i => (ushort)choices["move" + i]).ToArray());
    }
    private PKM DraftEntity() => draftDirty ? ReadDraft().Apply(selected!) : selected!.Clone();
    private void CommitDraft()
    {
        if (!draftDirty || selected is null) return;
        var candidate = DraftEntity(); session!.Commit(candidate, box, slot, party); selected = candidate; draftDirty = false; creationDraft = null;
    }
    private async void CheckLegality()
    {
        try
        {
            var pk = DraftEntity(); var table = session!.Save?.Personal;
            var progressView = Column(); progressView.SetPadding(D(20), D(12), D(20), D(12));
            progressView.AddView(new ProgressBar(this)); progressView.AddView(Text("PKHeX.Core prüft das Pokémon …", 14));
            using var progress = new AlertDialog.Builder(this).SetTitle("Legalität")!.SetView(progressView)!.SetCancelable(false)!.Create()!;
            progress.Show();
            var report = await Task.Run(() => { var analysis = table is null ? new LegalityAnalysis(pk) : new LegalityAnalysis(pk, table); return analysis.Report("de", true); });
            progress?.Dismiss();
            new AlertDialog.Builder(this).SetTitle("Legalitätsbericht")!.SetMessage(report)!.SetPositiveButton("OK", (_, _) => { })?.Show();
        }
        catch (Exception ex) { Error(ex.Message); }
    }

    protected override void OnDestroy()
    {
        pictureCancellation.Cancel(); updateCancellation?.Cancel();
        base.OnDestroy();
    }

    private void Trainer()
    {
        Navigate(() =>
        {
            // Keep the underlying editor consistent if this dialog is cancelled.
            Render(); Persist();
            var sav = session!.Save!; var panel = Column(); panel.SetPadding(D(18), D(8), D(18), 0);
            var name = new EditText(this) { Text = sav.OT, Hint = "Trainername" }; name.SetSingleLine(true); panel.AddView(name);
            var money = new EditText(this) { Text = sav.Money.ToString(), Hint = "Geld", InputType = InputTypes.ClassNumber }; panel.AddView(money);
            var supportsName = sav.GetType().GetProperty(nameof(SaveFile.OT))?.DeclaringType != typeof(SaveFile);
            var supportsMoney = sav.GetType().GetProperty(nameof(SaveFile.Money))?.DeclaringType != typeof(SaveFile);
            name.Enabled = supportsName; money.Enabled = supportsMoney;
            panel.AddView(Text($"Name: maximal {sav.MaxStringLengthTrainer} Zeichen\nGeld: 0–{sav.MaxMoney}", 13, true));
            var dialog = new AlertDialog.Builder(this).SetTitle("Trainer bearbeiten")!.SetView(panel)!
                .SetNegativeButton("Abbrechen", (_, _) => { })!.SetPositiveButton("Übernehmen", (_, _) => { })!.Create()!;
            dialog.Show(); dialog.GetButton((int)DialogButtonType.Positive)!.Click += (_, _) => Safe(() =>
            {
                if (!uint.TryParse(money.Text, out var amount)) throw new ArgumentException("Bitte einen gültigen Geldbetrag eingeben.");
                session.EditTrainer(name.Text ?? "", amount); dialog.Dismiss(); Render(); Persist();
            });
        });
    }

    private void OpenFile()
    {
        if (busy) return;
        void PickFile() => LaunchPicker(OpenRequest);
        if (session?.Dirty is true || draftDirty) Confirm("Andere Datei öffnen", "Die aktuelle Datei hat Änderungen. Exportiere sie vorher, wenn du sie behalten möchtest.", PickFile);
        else PickFile();
    }
    private void ImportFile()
    {
        if (session?.Save is null || slot < 0 || busy) return;
        Navigate(() =>
        {
            void PickFile() => LaunchPicker(ImportRequest);
            // Reflect saved/discarded drafts even if the file picker is cancelled.
            Render(); Persist();
            if (session.GetEntity(box, slot, party).Species != 0)
                Confirm("Platz ersetzen", "Das Pokémon an diesem Platz wird durch die importierte Datei ersetzt. Die Änderung kann rückgängig gemacht werden.", PickFile);
            else PickFile();
        });
    }
    private void LaunchPicker(int request)
    {
        Persist();
        var intent = new Intent(Intent.ActionOpenDocument); intent.SetType("*/*"); intent.AddCategory(Intent.CategoryOpenable);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission); StartActivityForResult(intent, request);
    }
    private void Export(bool original)
    {
        if (session is null || busy) return;
        if (!original) { CommitDraft(); Render(); }
        BeginExport(original ? session.Original : session.Export(), original ? "original-" + session.FileName : session.FileName);
    }
    private void ExportPokemon()
    {
        if (selected is null || busy) return;
        var pk = DraftEntity(); BeginExport(EditorSession.ExportEntity(pk), $"{SpeciesName(pk.Species)}.{pk.Extension}");
    }
    private void BeginExport(byte[] bytes, string name)
    {
        AtomicWrite("pending-export.bin", bytes); Persist();
        var intent = new Intent(Intent.ActionCreateDocument); intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("application/octet-stream"); intent.PutExtra(Intent.ExtraTitle, name);
        intent.AddFlags(ActivityFlags.GrantWriteUriPermission); StartActivityForResult(intent, ExportRequest);
    }
    private async Task<byte[]> ReadFile(Uri uri)
    {
        using var stream = ContentResolver!.OpenInputStream(uri) ?? throw new IOException("Datei nicht lesbar.");
        using var output = new MemoryStream(); var buffer = new byte[81920]; int read;
        while ((read = await stream.ReadAsync(buffer)) != 0)
        {
            if (output.Length + read > EditorSession.MaxFileSize) throw new ArgumentException("Die Datei ist größer als 32 MB.");
            await output.WriteAsync(buffer.AsMemory(0, read));
        }
        return output.ToArray();
    }
    private string FileName(Uri uri)
    {
        using var cursor = ContentResolver!.Query(uri, ["_display_name"], null, null, null);
        return cursor?.MoveToFirst() is true ? Path.GetFileName(cursor.GetString(0) ?? "main") : "main";
    }
    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (HandleUpdateActivityResult(requestCode)) return;
        if (resultCode != Result.Ok || data?.Data is null)
        {
            if (requestCode == ExportRequest) File.Delete(DataPath("pending-export.bin"));
            return;
        }
        busy = true;
        try
        {
            var uri = data.Data;
            if (requestCode == ExportRequest)
            {
                var bytes = File.ReadAllBytes(DataPath("pending-export.bin"));
                using var output = ContentResolver!.OpenOutputStream(uri, "wt") ?? throw new IOException("Zieldatei nicht beschreibbar.");
                await output.WriteAsync(bytes); await output.FlushAsync(); File.Delete(DataPath("pending-export.bin"));
                Toast.MakeText(this, "Datei exportiert", ToastLength.Long)?.Show(); return;
            }
            var name = FileName(uri); var input = await ReadFile(uri);
            if (requestCode == OpenRequest)
            {
                var opened = await Task.Run(() => EditorSession.Open(input, name));
                session = opened; box = 0; slot = -1; party = false; demo = false; editorTab = 0; creationDraft = null;
            }
            else if (requestCode == ImportRequest) session!.ImportEntity(input, name, box, slot, party);
            Render(); Persist();
        }
        catch (Exception ex) { Error(ex.Message); }
        finally { busy = false; }
    }
    public override void OnBackPressed()
    {
        if (slot >= 0 && session?.Save is not null) Navigate(() => { slot = -1; Render(); Persist(); });
        else Navigate(() => { Persist(); Finish(); });
    }
    private sealed class InsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View? view, WindowInsets? insets)
        {
            if (view is not null && insets is not null)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    var bars = insets.GetInsets(WindowInsets.Type.SystemBars());
                    view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
                }
                else view.SetPadding(insets.SystemWindowInsetLeft, insets.SystemWindowInsetTop, insets.SystemWindowInsetRight, insets.SystemWindowInsetBottom);
            }
            return insets!;
        }
    }
}
