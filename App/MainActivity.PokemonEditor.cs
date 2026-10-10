using static PKHeX.Android.Localization.AppText;
using Android.App;
using Android.Graphics;
using Android.Text;
using Android.Views;
using Android.Widget;
using PKHeX.Core;
using AColor = Android.Graphics.Color;

namespace PKHeX.Android.App;

public sealed partial class MainActivity
{
    private ImageView? previewImage;
    private TextView? previewName, previewLevel, previewBadge, previewFormHint, evTotal;
    private LinearLayout? typeChips;
    private LinearLayout[] editorPanels = [];
    private Button[] editorTabs = [];
    private (ushort Species, byte Form, bool Shiny, byte Gender, EntityContext Context)? previewIdentity;
    private string previewFormCaption = "";
    private string[] ItemNames() => strings.GetItemStrings(selected!.Context, selected.Version);
    private string[] AbilityNames()
    {
        var pk = selected!.Clone();
        if (choices.TryGetValue("species", out var species) && pk.Species != species) { pk.Species = (ushort)species; pk.Form = 0; }
        var pi = pk.PersonalInfo;
        return Enumerable.Range(0, pi.AbilityCount).Select(i => $"{strings.abilitylist[pi.GetAbilityAtIndex(i)]}{(i == 2 ? T(" (versteckt)", " (hidden)") : "")}").ToArray();
    }
    private EditText Input(string key, string value, string label, bool number = false)
    {
        var f = new EditText(this) { Text = value, ContentDescription = label, TextSize = 15, InputType = number ? InputTypes.ClassNumber : InputTypes.ClassText };
        f.SetSingleLine(true); f.SetTextColor(AColor.Rgb(237, 244, 252)); f.SetHintTextColor(Muted);
        f.SetPadding(D(12), D(10), D(12), D(10)); f.Background = Shape(AColor.Rgb(13, 23, 38), 11, true);
        f.SetMinimumHeight(D(46)); fields[key] = f; f.TextChanged += (_, _) => MarkDirty(key); return f;
    }
    private EditText Field(string key, string label, string value, bool number = false)
    {
        Add(Text(label, 11, true, true), Form); var f = Input(key, value, label, number); Add(f, Form); return f;
    }
    private void Choice(string key, string label, int initial, string[] names, int max, int min = 0)
    {
        choices[key] = initial; Add(Text(label, 11, true, true), Form);
        var button = Button(ChoiceName(initial, names) + "   ›", () =>
        {
            var currentNames = key == "ability" ? AbilityNames() : names;
            var currentMax = key == "ability" ? currentNames.Length - 1 : Math.Min(max, currentNames.Length - 1);
            Pick(label, currentNames, currentMax, min, value =>
            {
                choices[key] = value; choiceButtons[key].Text = ChoiceName(value, currentNames) + "   ›";
                if (key == "species" && choices.ContainsKey("ability")) { choices["ability"] = 0; choiceButtons["ability"].Text = ChoiceName(0, AbilityNames()) + "   ›"; }
                MarkDirty(key);
            }, key == "species", key == "species" && session?.Save is { } sav
                ? value => sav.Personal.IsPresentInGame((ushort)value, 0) : null);
        });
        button.Gravity = GravityFlags.CenterVertical | GravityFlags.Left; choiceButtons[key] = button; Add(button, Form);
    }
    private static string ChoiceName(int id, string[] names) => id >= 0 && id < names.Length ? names[id] : $"#{id}";
    private void Pick(string title, string[] names, int max, int min, Action<int> chosen, bool pictures = false, Func<int, bool>? allowed = null)
    {
        var panel = Column(); panel.SetPadding(D(14), 0, D(14), 0);
        var search = new EditText(this) { Hint = T("Name oder Nummer suchen", "Search by name or number"), InputType = InputTypes.ClassText };
        search.SetSingleLine(true); panel.AddView(search);
        var list = new ListView(this); panel.AddView(list, new LinearLayout.LayoutParams(-1, D(370)));
        var indices = new List<int>(); AlertDialog? dialog = null;
        CancellationTokenSource? filterCancellation = null;
        var pictured = pictures ? new SpeciesAdapter(this, [], names) : null;
        // The array overload wraps a fixed-size Java list: Clear/AddAll throw.
        // The empty constructor owns a mutable ArrayList for repeated filtering.
        var plain = pictures ? null : new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1);
        plain?.SetNotifyOnChange(false);
        list.Adapter = pictured is not null ? pictured : plain;
        void Filter()
        {
            var q = search.Text?.Trim() ?? ""; indices.Clear();
            for (var i = min; i <= Math.Min(max, names.Length - 1); i++)
                if ((allowed is null || allowed(i)) && (q.Length == 0 || names[i].Contains(q, StringComparison.OrdinalIgnoreCase) || i.ToString() == q)) indices.Add(i);
            if (pictured is not null) pictured.SetItems(indices.ToArray());
            else { plain!.SetNotifyOnChange(false); plain.Clear(); plain.AddAll((System.Collections.ICollection)indices.Select(i => $"{ChoiceName(i, names)} · #{i}").ToArray()); plain.NotifyDataSetChanged(); }
        }
        search.TextChanged += async (_, _) =>
        {
            filterCancellation?.Cancel(); filterCancellation?.Dispose();
            filterCancellation = new(); var token = filterCancellation.Token;
            try { await Task.Delay(120, token); if (dialog?.IsShowing is true) Safe(Filter); }
            catch (OperationCanceledException) { }
        };
        list.ItemClick += (_, e) =>
        {
            // A queued tap may refer to the previous results during fast searches.
            if ((uint)e.Position >= (uint)indices.Count) return;
            var value = indices[e.Position]; Safe(() => chosen(value)); dialog?.Dismiss();
        };
        dialog = new AlertDialog.Builder(this).SetTitle(title)!.SetView(panel)!.SetNegativeButton(T("Abbrechen", "Cancel"), (_, _) => { })!.Create()!;
        dialog.DismissEvent += (_, _) => filterCancellation?.Cancel();
        Filter(); dialog.Show();
    }
    private sealed class SpeciesAdapter(MainActivity owner, int[] initialIds, string[] names) : BaseAdapter<int>
    {
        private int[] ids = initialIds;
        internal void SetItems(int[] next) { ids = next; NotifyDataSetChanged(); }
        public override int Count => ids.Length;
        public override int this[int position] => ids[position];
        public override long GetItemId(int position) => ids[position];
        public override View GetView(int position, View? convertView, ViewGroup? parent)
        {
            var row = convertView as LinearLayout;
            if (row is null)
            {
                row = owner.Row(); row.SetPadding(0, owner.D(4), 0, owner.D(4));
                var picture = new ImageView(owner); picture.SetScaleType(ImageView.ScaleType.FitCenter);
                row.AddView(picture, new LinearLayout.LayoutParams(owner.D(47), owner.D(47)));
                var text = owner.Text("", 15); text.SetPadding(owner.D(12), 0, 0, 0); row.AddView(text);
            }
            var id = ids[position]; var image = (ImageView)row.GetChildAt(0)!; var label = (TextView)row.GetChildAt(1)!;
            image.ContentDescription = names[id]; label.Text = $"{names[id]}  ·  #{id}";
            owner.Artwork.Bind(image, owner.D(47), owner.pictureCancellation.Token, (ushort)id);
            return row;
        }
    }
    private void RenderEditor()
    {
        var pk = selected!;
        var hero = Card(); hero.Background = Gradient();
        var badgeRow = Row(); previewBadge = Pill($"#{pk.Species:0000} · {pk.GetType().Name.ToUpperInvariant()}"); Add(previewBadge, badgeRow, true);
        if (session!.Save is not null) Add(Pill(party ? T($"TEAM {slot + 1}", $"PARTY {slot + 1}") : T($"PLATZ {slot + 1}", $"SLOT {slot + 1}")), badgeRow, true);
        Add(badgeRow, hero);
        previewImage = Picture(pk.Species, 168, pk.Form, pk.IsShiny, pk.Gender, pk.Context);
        previewIdentity = (pk.Species, pk.Form, pk.IsShiny, pk.Gender, pk.Context); previewFormCaption = FormName(pk);
        hero.AddView(previewImage, new LinearLayout.LayoutParams(-1, D(169)));
        previewName = Text(SpeciesName(pk.Species) + (pk.IsShiny ? " ★" : ""), 26, bold: true); previewName.Gravity = GravityFlags.Center; Add(previewName, hero);
        previewLevel = Text($"Level {pk.CurrentLevel} · {FormName(pk)}", 12, true); previewLevel.Gravity = GravityFlags.Center; Add(previewLevel, hero);
        typeChips = Row(); typeChips.SetGravity(GravityFlags.Center); Add(typeChips, hero); DrawTypeChips(pk);
        previewFormHint = Text(T("Abbildung zeigt die Basisform.", "Artwork shows the base form."), 10, true); previewFormHint.Gravity = GravityFlags.Center;
        previewFormHint.Visibility = Artwork.HasExactForm(pk) ? ViewStates.Gone : ViewStates.Visible; Add(previewFormHint, hero);
        var shortcuts = Row(); Add(Button(T("Legalität prüfen", "Check legality"), CheckLegality), shortcuts, true);
        Add(Button(T("Pokémon exportieren", "Export Pokémon"), ExportPokemon), shortcuts, true); Add(shortcuts, content);
        var tabs = Row(); string[] labels = [T("Basis", "Basic"), T("Attacken", "Moves"), T("Werte", "Stats"), T("Infos", "Info")];
        editorPanels = new LinearLayout[4]; editorTabs = new Button[4];
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i; var button = Button(labels[i], () => SelectEditorTab(index)); button.TextSize = 11; editorTabs[i] = button; Add(button, tabs, true);
        }
        Add(tabs, content);
        for (var i = 0; i < 4; i++) editorPanels[i] = Card();
        formParent = editorPanels[0]; Add(Text(creationDraft is null ? T("Pokémon bearbeiten", "Edit Pokémon") : T("Neues Pokémon erstellen", "Create a new Pokémon"), 18, bold: true), Form);
        Choice("species", T("Pokémon-Art", "Species"), pk.Species, strings.specieslist, pk.MaxSpeciesID, 1);
        Field("nickname", T("Spitzname", "Nickname"), pk.Nickname);
        nicknamed = new CheckBox(this) { Text = T("Eigenen Spitznamen verwenden", "Use a custom nickname"), Checked = pk.IsNicknamed, TextSize = 12 };
        nicknamed.SetTextColor(Muted); nicknamed.CheckedChange += (_, _) => MarkDirty("nickname"); Add(nicknamed, Form);
        Field("level", "Level · 1–100", pk.CurrentLevel.ToString(), true);
        if (pk.Format >= 2) Choice("item", T("Getragenes Item", "Held item"), pk.HeldItem, ItemNames(), pk.MaxItemID);
        if (pk.Format >= 3)
        {
            Choice("nature", T("Wesen", "Nature"), (int)pk.Nature, strings.natures, 24);
            var labelsAbility = AbilityNames(); Choice("ability", T("Fähigkeit", "Ability"), pk.AbilityNumber switch { 4 => 2, 2 => 1, _ => 0 }, labelsAbility, labelsAbility.Length - 1);
        }
        shiny = new CheckBox(this) { Text = T("Schillernd (Shiny)", "Shiny"), Checked = pk.IsShiny, Enabled = pk.Format >= 2, TextSize = 14 };
        shiny.SetTextColor(AColor.Rgb(249, 200, 101)); shiny.CheckedChange += (_, _) => MarkDirty("shiny"); Add(shiny, Form);
        Add(Text(T("Die Bildvorschau zeigt Änderungen sofort. Übernimm sie vor dem Export.", "The preview shows changes immediately. Apply them before exporting."), 11, true), Form);
        formParent = editorPanels[1]; Add(Text(T("Attacken", "Moves"), 18, bold: true), Form);
        Add(Text(T("Tippe auf eine Attacke, um nach Name oder Nummer zu suchen.", "Tap a move to search by name or number."), 12, true), Form);
        for (var i = 0; i < 4; i++) Choice("move" + i, T($"Attacke {i + 1}", $"Move {i + 1}"), pk.GetMove(i), strings.movelist, pk.MaxMoveID);
        formParent = editorPanels[2]; RenderStats(pk);
        formParent = editorPanels[3]; Add(Text(T("Herkunft & Prüfung", "Origin & checks"), 18, bold: true), Form);
        Add(Text(T($"Originaltrainer\n{pk.OriginalTrainerName}\n\nTrainer-ID: {pk.DisplayTID}\nGeheime ID: {pk.DisplaySID}\nFormat: {pk.GetType().Name}\nForm: {FormName(pk)}", $"Original trainer\n{pk.OriginalTrainerName}\n\nTrainer ID: {pk.DisplayTID}\nSecret ID: {pk.DisplaySID}\nFormat: {pk.GetType().Name}\nForm: {FormName(pk)}"), 14, true), Form);
        Add(Button(T("Legalitätsbericht öffnen", "Open legality report"), CheckLegality), Form);
        if (session.Save is not null) Add(Button(T("Diesen Platz durch Import ersetzen", "Replace this slot by importing"), ImportFile), Form);
        Add(Text(T("Bearbeitung garantiert keine Legalität. Beim Wechsel der Art wird die Form auf 0 gesetzt.", "Editing does not guarantee legality. Changing the species resets the form to 0."), 12, true), Form);
        formParent = null; SelectEditorTab(Math.Clamp(editorTab, 0, 3));
        draftStatus = Text(creationDraft is null ? T("Änderungen mit „Übernehmen“ speichern.", "Save changes with “Apply”.") : T("Neues Pokémon mit „Übernehmen“ in den freien Platz speichern.", "Save the new Pokémon to the empty slot with “Apply”."), 11, true); Add(draftStatus, content);
    }
    private void SelectEditorTab(int index)
    {
        editorTab = index;
        for (var i = 0; i < editorPanels.Length; i++)
        {
            editorPanels[i].Visibility = i == index ? ViewStates.Visible : ViewStates.Gone;
            editorTabs[i].SetTextColor(i == index ? Background : Muted);
            editorTabs[i].Background = Shape(i == index ? Accent : Surface, 11);
        }
    }
    private void RenderStats(PKM pk)
    {
        Add(Text(T("Werte & Training", "Stats & training"), 18, bold: true), Form);
        Add(Text(pk.Format <= 2 ? T("DVs und Stat-Exp. KP-DV sowie Spezialwerte sind gekoppelt.", "DVs and Stat Exp. HP DV and Special stats are linked.") : T("IVs bestimmen das Potenzial. EVs entstehen durch Training.", "IVs determine potential. EVs come from training."), 12, true), Form);
        var heading = Row(); Add(Text(T("Wert", "Stat"), 11, true), heading, true); Add(Text(pk.Format <= 2 ? "DV" : "IV", 11, true), heading, true); Add(Text("EV", 11, true), heading, true); Add(heading, Form);
        string[] stats = [T("KP", "HP"), T("Angriff", "Attack"), T("Vert.", "Defense"), T("Sp.-Ang.", "Sp. Atk"), T("Sp.-Vert.", "Sp. Def"), T("Initiative", "Speed")];
        int[] ivs = [pk.IV_HP, pk.IV_ATK, pk.IV_DEF, pk.IV_SPA, pk.IV_SPD, pk.IV_SPE];
        int[] evs = [pk.EV_HP, pk.EV_ATK, pk.EV_DEF, pk.EV_SPA, pk.EV_SPD, pk.EV_SPE];
        for (var i = 0; i < 6; i++)
        {
            var row = Row(); var label = Text(stats[i], 12, bold: true); Add(label, row, true);
            var iv = Input("iv"+i, ivs[i].ToString(), $"{stats[i]} IV", true); iv.Gravity = GravityFlags.Center;
            var ev = Input("ev"+i, evs[i].ToString(), $"{stats[i]} EV", true); ev.Gravity = GravityFlags.Center;
            if (pk.Format <= 2) { if (i is 0 or 4) iv.Enabled = false; if (i == 4) ev.Enabled = false; }
            Add(iv, row, true); Add(ev, row, true); Add(row, Form);
        }
        evTotal = Text("", 12, true); Add(evTotal, Form); UpdateEvTotal();
        Add(Text(T($"{(pk.Format <= 2 ? "DV" : "IV")}: 0–{pk.MaxIV} · EV pro Wert: 0–{pk.MaxEV}", $"{(pk.Format <= 2 ? "DV" : "IV")}: 0–{pk.MaxIV} · EV per stat: 0–{pk.MaxEV}"), 11, true), Form);
    }
    private void UpdateEvTotal()
    {
        if (evTotal is null || selected is null || fields.Count == 0) return;
        int total = 0;
        for (var i = 0; i < 6; i++) if (fields.TryGetValue("ev"+i, out var field) && int.TryParse(field.Text, out var value)) total += value;
        evTotal.Text = selected.Format <= 2 ? T($"Stat-Exp. gesamt: {total}", $"Total Stat Exp.: {total}") : T($"EV gesamt: {total} / 510", $"Total EVs: {total} / 510");
        evTotal.SetTextColor(selected.Format >= 3 && total > 510 ? AColor.Rgb(255, 142, 142) : Accent);
    }
    private void DrawTypeChips(PKM pk)
    {
        if (typeChips is null) return; typeChips.RemoveAllViews();
        string[] colors = ["#A4AFBE","#DE7865","#AFA2E6","#B689D5","#D4AD76","#C5BA8D","#A8C65F","#9D96CE","#A0BCCF","#F4A36F","#7BB3F4","#80C69C","#F2D56F","#F099B5","#87D1D4","#AC9DF0","#AEA5A1","#ECAACC"];
        foreach (var type in new[] { pk.PersonalInfo.Type1, pk.PersonalInfo.Type2 }.Distinct())
        {
            if (type >= strings.types.Length || type >= colors.Length) continue;
            var lp = new LinearLayout.LayoutParams(-2, -2); lp.SetMargins(D(4), 0, D(4), 0);
            typeChips.AddView(Pill(strings.types[type], AColor.ParseColor(colors[type])), lp);
        }
    }
    private void UpdatePreview()
    {
        if (selected is null || previewImage is null || previewName is null || previewLevel is null) return;
        var species = (ushort)choices.GetValueOrDefault("species", selected.Species);
        var form = species == selected.Species ? selected.Form : (byte)0;
        var isShiny = shiny?.Checked ?? selected.IsShiny;
        var identity = (species, form, isShiny, selected.Gender, selected.Context);
        if (previewIdentity != identity)
        {
            var typesChanged = previewIdentity is not { } old || old.Species != species || old.Form != form;
            var pk = selected.Clone(); pk.Species = species; pk.Form = form;
            Artwork.Bind(previewImage, D(168), pictureCancellation.Token, species, form, isShiny, pk.Gender, pk.Context);
            previewImage.ContentDescription = SpeciesName(species) + (isShiny ? T(", schillernd", ", shiny") : "");
            previewName.Text = SpeciesName(species) + (isShiny ? " ★" : "");
            if (previewBadge is not null) previewBadge.Text = $"#{species:0000} · {selected.GetType().Name.ToUpperInvariant()}";
            if (previewFormHint is not null) previewFormHint.Visibility = Artwork.HasExactForm(pk) ? ViewStates.Gone : ViewStates.Visible;
            previewFormCaption = FormName(pk); if (typesChanged) DrawTypeChips(pk);
            previewIdentity = identity;
        }
        var level = fields.TryGetValue("level", out var value) ? value.Text : selected.CurrentLevel.ToString();
        var caption = $"Level {level} · {previewFormCaption}";
        if (previewLevel.Text != caption) previewLevel.Text = caption;
    }
    private void RefreshChoiceLabels()
    {
        foreach (var pair in choiceButtons)
        {
            if (!choices.TryGetValue(pair.Key, out var id)) continue;
            var names = pair.Key switch { "species" => strings.specieslist, "item" => ItemNames(), "nature" => strings.natures,
                "ability" => AbilityNames(), _ when pair.Key.StartsWith("move") => strings.movelist, _ => null };
            if (names is not null) pair.Value.Text = ChoiceName(id, names) + "   ›";
        }
        if (draftDirty) MarkDirty(); else { UpdatePreview(); UpdateEvTotal(); }
    }
}
