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
    private void RenderStorage(SaveFile sav)
    {
        if (!sav.HasBox && !sav.HasParty) { Add(Text(T("Dieser Spielstand hat keine bearbeitbaren Pokémon-Plätze.", "This save file has no editable Pokémon slots."), 14, true), content); return; }
        if (!sav.HasBox) party = true; if (!sav.HasParty) party = false;
        if (!party) box = Math.Clamp(box, 0, sav.BoxCount - 1);
        // Show the next free party place as well, including an entirely empty team.
        var count = party ? Math.Min(sav.PartyCount + 1, 6) : sav.BoxSlotCount;
        if (slot >= 0 && slot < count)
        {
            Add(Button(party ? T("‹ Zurück zum Team", "‹ Back to party") : T($"‹ Zurück zu Box {box + 1}", $"‹ Back to Box {box + 1}"), () => Navigate(() => { slot = -1; Render(); Persist(); })), content);
            selected = session!.GetEntity(box, slot, party);
            if (selected.Species != 0) RenderEditor();
            else if (creationDraft is not null)
            {
                selected = creationDraft; draftDirty = true; RenderEditor();
            }
            else
            {
                selected = null; var empty = Card();
                empty.AddView(new PokeBallView(this), new LinearLayout.LayoutParams(-1, D(100)));
                Add(Text(T($"Platz {slot + 1} ist frei", $"Slot {slot + 1} is empty"), 23, bold: true), empty);
                Add(Text(T("Erstelle hier ein Pokémon und bearbeite Art, Level, Attacken und Werte direkt in der App.", "Create a Pokémon here and edit its species, level, moves and stats in the app."), 14, true), empty);
                if (!party && sav.Generation <= 2) Add(Text(T("In Generation 1 und 2 werden Box-Pokémon beim Export ohne Lücken gespeichert.", "In Generations 1 and 2, box Pokémon are stored without gaps when exporting."), 12, true), empty);
                Add(Button(T("Pokémon erstellen", "Create Pokémon"), CreatePokemon, true), empty);
                Add(Button(T("Pokémon importieren", "Import Pokémon"), ImportFile), empty);
            }
            return;
        }
        slot = -1;
        var nav = Row();
        if (sav.HasBox) Add(Button(T("Boxen", "Boxes"), () => Navigate(() => { party = false; slot = -1; Render(); }), !party), nav, true);
        if (sav.HasParty) Add(Button(T("Team", "Party"), () => Navigate(() => { party = true; slot = -1; Render(); }), party), nav, true);
        Add(nav, content);
        if (!party)
        {
            var row = Row();
            row.AddView(Button("‹", () => { box = (box + sav.BoxCount - 1) % sav.BoxCount; Render(); }), new LinearLayout.LayoutParams(D(44), D(46)));
            Add(Button($"Box {box + 1}   /   {sav.BoxCount}", () =>
                new AlertDialog.Builder(this).SetTitle(T("Box auswählen", "Select box"))!.SetItems(Enumerable.Range(1, sav.BoxCount).Select(n => $"Box {n}").ToArray(), (_, e) => { box = e.Which; slot = -1; Render(); })?.Show()), row, true);
            row.AddView(Button("›", () => { box = (box + 1) % sav.BoxCount; Render(); }), new LinearLayout.LayoutParams(D(44), D(46))); Add(row, content);
        }
        var entities = Enumerable.Range(0, count).Select(i => session!.GetEntity(box, i, party)).ToArray();
        var headline = Row();
        Add(Text(party ? T("Dein Team", "Your party") : T("Deine Pokémon", "Your Pokémon"), 20, bold: true), headline, true);
        headline.AddView(Pill($"{entities.Count(p => p.Species > 0)} / {(party ? 6 : count)}"), new LinearLayout.LayoutParams(-2, -2)); Add(headline, content);
        if (party && sav.PartyCount == 0) Add(Text(T("Dein Team ist noch leer. Tippe auf den freien Platz, um ein Pokémon zu erstellen.", "Your party is empty. Tap the empty slot to create a Pokémon."), 14, true), content);
        var columns = party ? 3 : (Resources!.DisplayMetrics!.WidthPixels / Resources.DisplayMetrics.Density >= 420 ? 6 : 5);
        for (var start = 0; start < count; start += columns)
        {
            var row = Row();
            for (var i = start; i < start + columns; i++)
            {
                if (i >= count) { Add(new View(this), row, true); continue; }
                var index = i; var pk = entities[i]; var tile = Column();
                tile.Background = Shape(Surface, 14, pk.Species != 0); tile.SetPadding(D(3), D(4), D(3), D(5));
                tile.Clickable = true; tile.Focusable = true;
                tile.ContentDescription = pk.Species == 0 ? T($"Leerer Platz {i + 1}", $"Empty slot {i + 1}") : T($"Platz {i + 1}, {SpeciesName(pk.Species)}, Level {pk.CurrentLevel}", $"Slot {i + 1}, {SpeciesName(pk.Species)}, Level {pk.CurrentLevel}");
                var imageStage = new FrameLayout(this);
                if (pk.Species != 0) imageStage.AddView(Picture(pk.Species, party ? 93 : 64, pk.Form, pk.IsShiny, pk.Gender, pk.Context), new FrameLayout.LayoutParams(-1, D(party ? 93 : 64), GravityFlags.Center));
                else { var ball = new PokeBallView(this) { Alpha = .26f }; imageStage.AddView(ball, new FrameLayout.LayoutParams(D(30), D(30), GravityFlags.Center)); }
                var number = Text($"{i+1:00}", 8, true); number.SetPadding(D(3), 0, 0, 0);
                imageStage.AddView(number, new FrameLayout.LayoutParams(-2, -2, GravityFlags.Left | GravityFlags.Top));
                if (pk.IsShiny) { var star = Text("★", 11); star.SetTextColor(AColor.Rgb(250, 207, 110)); imageStage.AddView(star, new FrameLayout.LayoutParams(-2, -2, GravityFlags.Right | GravityFlags.Top)); }
                tile.AddView(imageStage, new LinearLayout.LayoutParams(-1, D(party ? 95 : 65)));
                var name = Text(pk.Species == 0 ? T("Frei", "Empty") : SpeciesName(pk.Species), party ? 13 : 9, pk.Species == 0, true);
                name.Gravity = GravityFlags.Center; name.SetSingleLine(true); name.Ellipsize = TextUtils.TruncateAt.End; name.SetPadding(0, D(2), 0, D(2));
                tile.AddView(name, new LinearLayout.LayoutParams(-1, -2));
                var level = Text(pk.Species == 0 ? "+" : $"Lv. {pk.CurrentLevel}", 9, true); level.Gravity = GravityFlags.Center; level.SetPadding(0, 0, 0, 0);
                tile.AddView(level, new LinearLayout.LayoutParams(-1, -2));
                tile.Click += (_, _) => Navigate(() => { slot = index; editorTab = 0; Render(); }); Add(tile, row, true);
            }
            Add(row, content);
        }
        Add(Text(T("Tippe auf ein Pokémon zum Bearbeiten oder auf einen freien Platz zum Erstellen oder Importieren.", "Tap a Pokémon to edit it, or an empty slot to create or import one."), 12, true), content);
    }

    private void CreatePokemon()
    {
        if (session?.Save is not { } sav || slot < 0 || busy) return;
        Pick(T("Pokémon erstellen", "Create Pokémon"), strings.specieslist, sav.BlankPKM.MaxSpeciesID, 1, species =>
        {
            creationDraft = session.CreateEntity((ushort)species, box, slot, party);
            editorTab = 0; Render(); Persist();
        }, true, species => sav.Personal.IsPresentInGame((ushort)species, 0));
    }
}
