using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using PKHeX.Core;
using PKHeX.Android.Editor;
using AColor = Android.Graphics.Color;

namespace PKHeX.Android.App;

public sealed partial class MainActivity
{
    private PokemonArtwork? artwork;
    private PokemonArtwork Artwork => artwork ??= new PokemonArtwork(Assets!);
    private bool demo;
    private int editorTab;
    private LinearLayout? formParent;
    private TextView? draftStatus;
    private Button? applyButton;
    private readonly Dictionary<string, Button> choiceButtons = [];
    private CancellationTokenSource pictureCancellation = new();
    private ScrollView? pageScroll;
    private LinearLayout? pageFooter;
    private static readonly Typeface MediumFont = Typeface.Create("sans-serif-medium", TypefaceStyle.Normal)!;
    private SaveFile? checksumSave;
    private bool checksumValid;
    private LinearLayout Form => formParent ?? content;

    private TextView Text(string text, float size = 16, bool muted = false, bool bold = false)
    {
        var t = new TextView(this) { Text = text, TextSize = size }; t.SetIncludeFontPadding(false);
        t.SetTextColor(muted ? Muted : AColor.Rgb(237, 244, 252));
        if (bold) t.Typeface = MediumFont;
        t.SetPadding(0, D(4), 0, D(6)); return t;
    }
    private LinearLayout Column() => new(this) { Orientation = global::Android.Widget.Orientation.Vertical };
    private LinearLayout Row()
    {
        var row = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical); return row;
    }
    private void Add(View view, LinearLayout parent, bool weighted = false)
    {
        var lp = new LinearLayout.LayoutParams(weighted ? 0 : ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, weighted ? 1 : 0);
        lp.SetMargins(D(3), D(4), D(3), D(4)); parent.AddView(view, lp);
    }
    private GradientDrawable Shape(AColor color, int radius = 18, bool border = false)
    {
        var d = new GradientDrawable(); d.SetColor(color); d.SetCornerRadius(D(radius));
        if (border) d.SetStroke(D(1), AColor.Rgb(40, 60, 79)); return d;
    }
    private GradientDrawable Gradient()
    {
        var d = new GradientDrawable(GradientDrawable.Orientation.TlBr, [AColor.Rgb(26, 55, 66), AColor.Rgb(18, 29, 49)]);
        d.SetCornerRadius(D(24)); return d;
    }
    private Button Button(string label, Action action, bool primary = false)
    {
        var b = new Button(this) { Text = label, TextSize = 13 };
        b.SetAllCaps(false); b.SetTextColor(primary ? Background : AColor.Rgb(215, 233, 242));
        b.Typeface = MediumFont;
        b.Background = new RippleDrawable(ColorStateList.ValueOf(AColor.Argb(45, 255, 255, 255)), Shape(primary ? Accent : Surface, 14, !primary), null);
        b.SetPadding(D(10), D(9), D(10), D(9)); b.SetMinimumWidth(0); b.SetMinimumHeight(D(46));
        b.Click += (_, _) => Safe(action); return b;
    }
    private TextView Pill(string label, AColor? color = null)
    {
        var c = color ?? Accent; var t = Text(label, 10, bold: true);
        t.SetTextColor(c); t.Background = Shape(AColor.Argb(25, c.R, c.G, c.B), 8);
        t.SetPadding(D(9), D(5), D(9), D(5)); t.Gravity = GravityFlags.Center; return t;
    }
    private LinearLayout Card(LinearLayout? parent = null)
    {
        var c = Column(); c.Background = Shape(Surface, 20, true); c.SetPadding(D(15), D(12), D(15), D(14)); Add(c, parent ?? content); return c;
    }
    private ImageView Picture(ushort species, int size, byte form = 0, bool isShiny = false, byte gender = 2, EntityContext context = EntityContext.Gen9)
    {
        var image = new ImageView(this) { ContentDescription = SpeciesName(species) + (isShiny ? ", schillernd" : "") };
        image.SetScaleType(ImageView.ScaleType.FitCenter);
        Artwork.Bind(image, D(size), pictureCancellation.Token, species, form, isShiny, gender, context);
        image.LayoutParameters = new ViewGroup.LayoutParams(D(size), D(size)); return image;
    }
    private string SpeciesName(ushort id) => id < strings.specieslist.Length ? strings.specieslist[id] : $"#{id}";
    private string FormName(PKM pk)
    {
        var names = FormConverter.GetFormList(pk.Species, strings.types, strings.forms, pk.Context);
        return pk.Form < names.Length && names[pk.Form].Length > 0 ? names[pk.Form] : $"Form {pk.Form}";
    }

    private void Render()
    {
        pictureCancellation.Cancel(); pictureCancellation.Dispose(); pictureCancellation = new();
        building = true; fields.Clear(); choices.Clear(); choiceButtons.Clear(); selected = null;
        draftDirty = false; nicknamed = shiny = null; formParent = null; applyButton = null; draftStatus = null;
        previewImage = null; previewName = null; previewLevel = null; evTotal = null; typeChips = null; previewIdentity = null;
        if (pageScroll is null) BuildShell();
        content.RemoveAllViews(); pageFooter!.RemoveAllViews();
        pageFooter.Visibility = session is null ? ViewStates.Gone : ViewStates.Visible;
        if (session is null) RenderWelcome(); else RenderSession();
        if (session is not null) RenderFooter();
        pageScroll!.ScrollTo(0, 0);
        building = false;
    }

    private void BuildShell()
    {
        root = Column(); root.SetBackgroundColor(Background); root.SetOnApplyWindowInsetsListener(new InsetsListener());
        var header = Row(); header.SetPadding(D(19), D(12), D(19), D(8));
        header.AddView(new PokeBallView(this), new LinearLayout.LayoutParams(D(35), D(35)));
        var brand = Column(); brand.SetPadding(D(10), 0, 0, 0);
        brand.AddView(Text("PKHeX", 23, bold: true)); brand.AddView(Text("Android · " + InstalledPackage.VersionName, 10, true));
        header.AddView(brand, new LinearLayout.LayoutParams(0, -2, 1));
        header.AddView(Button("Öffnen", OpenFile), new LinearLayout.LayoutParams(D(77), D(46)));
        var menu = Button("☰", FileActions); menu.ContentDescription = "Menü mit Update und Werkzeugen"; menu.TextSize = 23;
        var menuLp = new LinearLayout.LayoutParams(D(45), D(46)); menuLp.LeftMargin = D(7); header.AddView(menu, menuLp);
        Add(header, root);
        pageScroll = new ScrollView(this) { FillViewport = true, VerticalScrollBarEnabled = false };
        content = Column(); content.SetPadding(D(17), D(3), D(17), D(20)); pageScroll.AddView(content);
        root.AddView(pageScroll, new LinearLayout.LayoutParams(-1, 0, 1));
        pageFooter = Row(); pageFooter.SetPadding(D(17), D(7), D(17), D(9)); pageFooter.SetBackgroundColor(Background);
        Add(pageFooter, root); SetContentView(root);
    }

    private void RenderWelcome()
    {
        var hero = Card(); hero.Background = Gradient(); hero.SetPadding(D(19), D(18), D(19), D(18));
        var badges = Row(); Add(Pill("DEIN TEAM. DEIN SPIELSTAND."), badges); Add(badges, hero);
        Add(Text("Deine Pokémon.\nImmer im Blick.", 30, bold: true), hero);
        Add(Text("Entdecke deine Boxen und bearbeite dein Team direkt auf dem Smartphone.", 14, true), hero);
        var stage = new FrameLayout(this);
        stage.AddView(Picture(6, 127), new FrameLayout.LayoutParams(D(127), D(127), GravityFlags.Left | GravityFlags.CenterVertical));
        stage.AddView(Picture(150, 125), new FrameLayout.LayoutParams(D(125), D(125), GravityFlags.Right | GravityFlags.CenterVertical));
        stage.AddView(Picture(25, 151), new FrameLayout.LayoutParams(D(151), D(151), GravityFlags.Center));
        hero.AddView(stage, new LinearLayout.LayoutParams(-1, D(164)));
        Add(Button("Spielstand öffnen", OpenFile, true), hero);
        Add(Button("Vorschau mit Pokémon", LoadDemo), hero);
        Add(Text("Alles für dein Team", 20, bold: true), content);
        var row = Row();
        foreach (var feature in new[] { ("Boxübersicht", "Mit Pokémon-Bildern"), ("Pokémon-Editor", "Werte & Attacken") })
        {
            var c = Column(); c.Background = Shape(Surface, 17); c.SetPadding(D(13), D(12), D(13), D(12));
            Add(Text(feature.Item1, 14, bold: true), c); Add(Text(feature.Item2, 11, true), c); Add(c, row, true);
        }
        Add(row, content);
        var local = Card(); var localRow = Row();
        Add(Pill("OFFLINE"), localRow, true); Add(Text("Deine Dateien bleiben auf dem Gerät.", 12, true), localRow, true); Add(localRow, local);
        Add(Button("Welche Dateien kann ich öffnen?", FileHelp), content);
    }
    private void FileHelp() => new AlertDialog.Builder(this).SetTitle("Dateien öffnen")!
        .SetMessage("Entschlüsselte Pokémon-Spielstände (.sav, .dsv, main …) und einzelne Pokémon-Dateien (.pk1–.pk9, .pb7, .pb8, .pa8, .pa9 …).\n\nWähle eine zuvor exportierte Datei vom Gerät. Die App liest keine Spielstände direkt aus einem laufenden Spiel oder einer Konsole.\n\nNach der Bearbeitung exportierst du eine neue Datei. Mit „Original sichern“ erhältst du zusätzlich eine unveränderte Kopie.")!
        .SetPositiveButton("Verstanden", (_, _) => { })?.Show();

    private void LoadDemo()
    {
        Navigate(() =>
        {
            using var source = Assets!.Open("demo.sav"); using var buffer = new MemoryStream(); source.CopyTo(buffer);
            session = EditorSession.Open(buffer.ToArray(), "Vorschau.sav"); demo = true; creationDraft = null;
            box = 0; slot = -1; party = false; editorTab = 0; Render(); Persist();
        });
    }
    private void RenderSession()
    {
        var s = session!;
        var card = Card(); var title = Row();
        Add(Text(demo ? "Pokémon-Vorschau" : s.FileName, 17, bold: true), title, true);
        var status = Pill(demo ? "DEMO" : s.Dirty ? "GEÄNDERT" : "GELADEN", demo ? AColor.Rgb(249, 200, 101) : Accent);
        title.AddView(status, new LinearLayout.LayoutParams(-2, -2)); Add(title, card);
        if (s.Save is { } sav)
        {
            Add(Text($"{GameInfo.GetVersionName(sav.Version)} · {sav.OT}", 12, true), card);
            if (demo) Add(Text("Beispieldaten zur Vorschau, kein spielbarer Spielstand.", 11, true), card);
            else if (!ChecksumsValidFor(sav)) Add(Text("Prüfsummen prüfen · Original über das Menü sichern.", 12), card);
            RenderStorage(sav);
        }
        else { slot = 0; selected = s.Entity!.Clone(); RenderEditor(); }
    }
    private bool ChecksumsValidFor(SaveFile save)
    {
        if (!ReferenceEquals(checksumSave, save)) { checksumValid = save.ChecksumsValid; checksumSave = save; }
        return checksumValid;
    }
    private void FileActions()
    {
        string[] items = session is null ? ["Update"] : ["Update", "Datei exportieren", "Original sichern", "Letzte Änderung rückgängig", "Original wiederherstellen", "Trainer bearbeiten"];
        new AlertDialog.Builder(this).SetTitle("Menü")!.SetItems(items, (_, e) => Safe(() =>
        {
            if (e.Which == 0) { CheckForUpdates(); return; }
            if (session is null) return;
            switch (e.Which)
            {
                case 1: Export(false); break;
                case 2: Export(true); break;
                case 3: if (session.CanUndo) Navigate(() => { session.Undo(); Render(); Persist(); }); else Toast.MakeText(this, "Keine Änderung zum Rückgängigmachen", ToastLength.Short)?.Show(); break;
                case 4: Confirm("Original wiederherstellen", "Alle Änderungen dieser Datei verwerfen?", () => { session.Reset(); creationDraft = null; slot = -1; Render(); Persist(); }); break;
                case 5: if (session.Save is not null) Trainer(); else Toast.MakeText(this, "Trainer-Editor benötigt einen Spielstand", ToastLength.Short)?.Show(); break;
            }
        }))?.Show();
    }
    private void ApplyChanges()
    {
        CommitDraft();
        ((InputMethodManager?)GetSystemService(InputMethodService))?.HideSoftInputFromWindow(root.WindowToken, HideSoftInputFlags.None);
        Render(); Persist(); Toast.MakeText(this, "Änderungen übernommen", ToastLength.Short)?.Show();
    }
    private void RenderFooter()
    {
        var footer = pageFooter!;
        if (selected is not null)
        {
            applyButton = Button("Übernehmen", ApplyChanges, true); applyButton.Enabled = draftDirty; applyButton.Alpha = draftDirty ? 1 : .5f;
            Add(applyButton, footer, true); Add(Button("Exportieren", () => Export(false)), footer, true);
        }
        else { Add(Button("Datei exportieren", () => Export(false), true), footer, true); Add(Button("Werkzeuge", FileActions), footer, true); }
    }
    private void MarkDirty(string? changedField = null)
    {
        if (building) return; draftDirty = true;
        if (applyButton is not null) { applyButton.Enabled = true; applyButton.Alpha = 1; }
        if (draftStatus is not null) { draftStatus.Text = "Änderungen noch nicht übernommen"; draftStatus.SetTextColor(AColor.Rgb(249, 200, 101)); }
        if (changedField is null or "species" or "shiny" or "level") UpdatePreview();
        if (changedField is null || changedField.StartsWith("ev", StringComparison.Ordinal)) UpdateEvTotal();
    }
}
