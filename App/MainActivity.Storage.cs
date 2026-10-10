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
        if (!sav.HasBox && !sav.HasParty) { Add(Text("Dieser Spielstand hat keine bearbeitbaren Pokémon-Plätze.", 14, true), content); return; }
        if (!sav.HasBox) party = true; if (!sav.HasParty) party = false;
        if (!party) box = Math.Clamp(box, 0, sav.BoxCount - 1);
        // Show the next free party place as well, including an entirely empty team.
        var count = party ? Math.Min(sav.PartyCount + 1, 6) : sav.BoxSlotCount;
        if (slot >= 0 && slot < count)
        {
            Add(Button(party ? "‹ Zurück zum Team" : $"‹ Zurück zu Box {box + 1}", () => Navigate(() => { slot = -1; Render(); Persist(); })), content);
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
                Add(Text($"Platz {slot + 1} ist frei", 23, bold: true), empty);
                Add(Text("Erstelle hier ein Pokémon und bearbeite Art, Level, Attacken und Werte direkt in der App.", 14, true), empty);
                if (!party && sav.Generation <= 2) Add(Text("In Generation 1 und 2 werden Box-Pokémon beim Export ohne Lücken gespeichert.", 12, true), empty);
                Add(Button("Pokémon erstellen", CreatePokemon, true), empty);
                Add(Button("Pokémon importieren", ImportFile), empty);
            }
            return;
        }
        slot = -1;
        var nav = Row();
        if (sav.HasBox) Add(Button("Boxen", () => Navigate(() => { party = false; slot = -1; Render(); }), !party), nav, true);
        if (sav.HasParty) Add(Button("Team", () => Navigate(() => { party = true; slot = -1; Render(); }), party), nav, true);
        Add(nav, content);
        if (!party)
        {
            var row = Row();
            row.AddView(Button("‹", () => { box = (box + sav.BoxCount - 1) % sav.BoxCount; Render(); }), new LinearLayout.LayoutParams(D(44), D(46)));
            Add(Button($"Box {box + 1}   /   {sav.BoxCount}", () =>
                new AlertDialog.Builder(this).SetTitle("Box auswählen")!.SetItems(Enumerable.Range(1, sav.BoxCount).Select(n => $"Box {n}").ToArray(), (_, e) => { box = e.Which; slot = -1; Render(); })?.Show()), row, true);
            row.AddView(Button("›", () => { box = (box + 1) % sav.BoxCount; Render(); }), new LinearLayout.LayoutParams(D(44), D(46))); Add(row, content);
        }
        var entities = Enumerable.Range(0, count).Select(i => session!.GetEntity(box, i, party)).ToArray();
        var headline = Row();
        Add(Text(party ? "Dein Team" : "Deine Pokémon", 20, bold: true), headline, true);
        headline.AddView(Pill($"{entities.Count(p => p.Species > 0)} / {(party ? 6 : count)}"), new LinearLayout.LayoutParams(-2, -2)); Add(headline, content);
        if (party && sav.PartyCount == 0) Add(Text("Dein Team ist noch leer. Tippe auf den freien Platz, um ein Pokémon zu erstellen.", 14, true), content);
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
                tile.ContentDescription = pk.Species == 0 ? $"Leerer Platz {i + 1}" : $"Platz {i + 1}, {SpeciesName(pk.Species)}, Level {pk.CurrentLevel}";
                var imageStage = new FrameLayout(this);
                if (pk.Species != 0) imageStage.AddView(Picture(pk.Species, party ? 93 : 64, pk.Form, pk.IsShiny, pk.Gender, pk.Context), new FrameLayout.LayoutParams(-1, D(party ? 93 : 64), GravityFlags.Center));
                else { var ball = new PokeBallView(this) { Alpha = .26f }; imageStage.AddView(ball, new FrameLayout.LayoutParams(D(30), D(30), GravityFlags.Center)); }
                var number = Text($"{i+1:00}", 8, true); number.SetPadding(D(3), 0, 0, 0);
                imageStage.AddView(number, new FrameLayout.LayoutParams(-2, -2, GravityFlags.Left | GravityFlags.Top));
                if (pk.IsShiny) { var star = Text("★", 11); star.SetTextColor(AColor.Rgb(250, 207, 110)); imageStage.AddView(star, new FrameLayout.LayoutParams(-2, -2, GravityFlags.Right | GravityFlags.Top)); }
                tile.AddView(imageStage, new LinearLayout.LayoutParams(-1, D(party ? 95 : 65)));
                var name = Text(pk.Species == 0 ? "Frei" : SpeciesName(pk.Species), party ? 13 : 9, pk.Species == 0, true);
                name.Gravity = GravityFlags.Center; name.SetSingleLine(true); name.Ellipsize = TextUtils.TruncateAt.End; name.SetPadding(0, D(2), 0, D(2));
                tile.AddView(name, new LinearLayout.LayoutParams(-1, -2));
                var level = Text(pk.Species == 0 ? "+" : $"Lv. {pk.CurrentLevel}", 9, true); level.Gravity = GravityFlags.Center; level.SetPadding(0, 0, 0, 0);
                tile.AddView(level, new LinearLayout.LayoutParams(-1, -2));
                tile.Click += (_, _) => Navigate(() => { slot = index; editorTab = 0; Render(); }); Add(tile, row, true);
            }
            Add(row, content);
        }
        Add(Text("Tippe auf ein Pokémon zum Bearbeiten oder auf einen freien Platz zum Erstellen oder Importieren.", 12, true), content);
    }

    private void CreatePokemon()
    {
        if (session?.Save is not { } sav || slot < 0 || busy) return;
        Pick("Pokémon erstellen", strings.specieslist, sav.BlankPKM.MaxSpeciesID, 1, species =>
        {
            creationDraft = session.CreateEntity((ushort)species, box, slot, party);
            editorTab = 0; Render(); Persist();
        }, true, species => sav.Personal.IsPresentInGame((ushort)species, 0));
    }
}
