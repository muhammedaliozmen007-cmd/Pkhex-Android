using PKHeX.Core;
using PKHeX.Android.Editor;

if (args.Length == 3 && args[0] == "--verify-export")
{
    var saved = EditorSession.Open(File.ReadAllBytes(args[1]), "edited.pk9");
    if (saved.Entity?.CurrentLevel != byte.Parse(args[2])) throw new Exception("Exported level incorrect");
    Console.WriteLine("PASS emulator export has level " + args[2] + " and valid checksum");
    return;
}

int assertions = 0;
void Assert(bool value, string message)
{
    assertions++;
    if (!value) throw new Exception(message);
}
void Reject(Action action, string message)
{
    assertions++;
    try { action(); } catch (ArgumentException) { return; }
    throw new Exception(message);
}
void RejectOccupied(Action action, string message)
{
    assertions++;
    try { action(); } catch (InvalidOperationException) { return; }
    throw new Exception(message);
}
PokemonEdit Edit(PKM pk, byte level) => new(pk.Species, "ANDROID", true, level,
    pk.HeldItem, pk.Nature, pk.AbilityNumber switch { 4 => 2, 2 => 1, _ => 0 }, pk.IsShiny,
    [pk.IV_HP, pk.IV_ATK, pk.IV_DEF, pk.IV_SPA, pk.IV_SPD, pk.IV_SPE],
    [pk.EV_HP, pk.EV_ATK, pk.EV_DEF, pk.EV_SPA, pk.EV_SPD, pk.EV_SPE],
    [pk.Move1, pk.Move2, pk.Move3, pk.Move4]);

Reject(() => EditorSession.Open([], "empty"), "Empty file accepted");
Reject(() => EditorSession.Open([1, 2, 3], "invalid.sav"), "Junk accepted");
Reject(() => EditorSession.Open(new byte[EditorSession.MaxFileSize + 1], "large"), "Large file accepted");
var outputPath = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "pkhex-android-fixtures");
Directory.CreateDirectory(outputPath);
PKM[] formats = [new PK1(), new PK2(), new PK3(), new PK4(), new PK5(), new PK6(), new PK7(), new PK8(), new PB8(), new PA8(), new PK9(), new PA9()];
foreach (var pk in formats)
{
    pk.Species = 25; pk.Version = pk.Context.GetSingleGameVersion(); pk.Language = (int)LanguageID.English; pk.OriginalTrainerName = "TEST";
    pk.CurrentLevel = 10; pk.Move1 = 33; pk.Move1_PP = 10; pk.RefreshChecksum();
    var bytes = EditorSession.ExportEntity(pk);
    var s = EditorSession.Open(bytes, "test." + pk.Extension);
    var source = s.GetEntity(0, 0);
    var edited = Edit(source, 50).Apply(source);
    Assert(source.CurrentLevel == 10, "Draft mutated source " + pk.Extension);
    s.Commit(edited, 0, 0);
    var roundtrip = EditorSession.Open(s.Export(), "test." + pk.Extension);
    Assert(roundtrip.Entity!.CurrentLevel == 50, "Level roundtrip " + pk.Extension);
    Assert(roundtrip.Entity.Nickname == "ANDROID", "Nickname roundtrip " + pk.Extension);
    Assert(roundtrip.Entity.ChecksumValid, "Entity checksum " + pk.Extension);
    Assert(s.Original.SequenceEqual(bytes), "Original mutated " + pk.Extension);
    Assert(s.CanUndo && s.Dirty, "Undo state " + pk.Extension);
    s.Undo(); Assert(s.Entity!.CurrentLevel == 10, "Undo failed " + pk.Extension);
    s.Reset(); Assert(!s.Dirty && !s.CanUndo && s.Export().SequenceEqual(bytes), "Reset failed " + pk.Extension);
    Reject(() => (Edit(source, 101)).Apply(source), "Level limit not enforced");
    Reject(() => (Edit(source, 50) with { IVs = [100, 0, 0, 0, 0, 0] }).Apply(source), "IV limit not enforced");
    if (pk.Format >= 3) Reject(() => (Edit(source, 50) with { EVs = [252, 252, 252, 0, 0, 0] }).Apply(source), "EV total not enforced");
    File.WriteAllBytes(Path.Combine(outputPath, "Pikachu." + pk.Extension), s.Export());
    var la = new LegalityAnalysis(source); var report = la.Report("de", true);
    Assert(!string.IsNullOrWhiteSpace(report), "Legality report missing");
    Console.WriteLine("PASS entity " + pk.Extension);
}

var gen4 = new PK4 { Species = 133, Version = GameVersion.Pt, PID = 0 };
gen4.CurrentLevel = 10;
var abilityEdit = (Edit(gen4, 20) with { AbilitySlot = 1 }).Apply(gen4);
Assert(abilityEdit.PIDAbility == 1 && abilityEdit.Ability == abilityEdit.PersonalInfo.GetAbilityAtIndex(1), "Gen4 ability and PID disagree");
Assert(abilityEdit.Nature == gen4.Nature, "Ability change altered nature");
var gen5 = new PK5 { Species = 133, Version = GameVersion.B2, PID = 0, Nature = Nature.Jolly };
gen5.CurrentLevel = 10;
var ability5 = (Edit(gen5, 20) with { AbilitySlot = 1 }).Apply(gen5);
Assert(ability5.PIDAbility == 1 && ability5.AbilityNumber == 2, "Gen5 ability and PID disagree");
Assert(ability5.Nature == Nature.Jolly, "Gen5 ability change altered nature");

GameVersion[] games = [GameVersion.RD, GameVersion.C, GameVersion.E, GameVersion.Pt, GameVersion.B2, GameVersion.X, GameVersion.SN, GameVersion.BD];
foreach (var version in games)
{
    var sav = BlankSaveFile.Get(version);
    if (version == GameVersion.E)
    {
        // A Gen 3 blank object has no serialized sector container. Supply both flash slots.
        var flash = new byte[SaveUtil.SIZE_G3RAW];
        for (int g = 0; g < 2; g++) for (int i = 0; i < 14; i++)
        {
            int offset = g * 0xE000 + i * 0x1000;
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(flash.AsSpan(offset + 0xFF4), (short)i);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(flash.AsSpan(offset + 0xFF8), 0x08012025);
        }
        ((SAV3)sav).WriteBothSaveSlots(flash);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(flash.AsSpan(0xAC), 0x12345678);
        flash[0x900] = 1;
        // Gen 3 detects the international trainer encoding from the name tail.
        flash[6] = 0xFF;
        sav = new SAV3E(flash);
        sav.OT = "TEST";
    }
    if (version == GameVersion.Pt)
    {
        var blank = sav;
        var ds = new SAV4Pt(new byte[SaveUtil.SIZE_G4RAW]);
        ds.CopyChangesFrom(blank);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(ds.General[^12..], SAV4Pt.GeneralSize);
        const int storageSize = 0x121E4;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(ds.Data[(SAV4Pt.GeneralSize + storageSize - 12)..], storageSize);
        ds.Magic = SAV4.MAGIC_JAPAN_INTL;
        ds.Data[..0x40000].CopyTo(ds.Data[0x40000..]);
        sav = ds;
    }
    var pk = sav.BlankPKM;
    pk.Species = 25; pk.Version = version; pk.Language = (int)LanguageID.English; pk.OriginalTrainerName = "TEST";
    pk.CurrentLevel = 10; pk.Move1 = 33; pk.RefreshChecksum();
    sav.SetBoxSlotAtIndex(pk, 0, 0, EntityImportSettings.None);
    var input = sav.Write().ToArray();
    // Synthetic Gen 1/2 saves need the raw box-list sentinel used by upstream's detector.
    // A real in-game save already contains this marker.
    if (version == GameVersion.RD) { input[0x2F2C] = 0; input[0x2F2D] = 0xFF; }
    if (version == GameVersion.C) { input[0x2865] = 0; input[0x2866] = 0xFF; }
    if (sav.Generation is 6 or 7) System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(input.AsSpan(input.Length - 0x1F0), 0x42454546);
    var s = EditorSession.Open(input, "fixture.sav");
    Assert(s.Save is not null, "Save detection failed " + version);
    s.Commit(Edit(s.GetEntity(0, 0), 42).Apply(s.GetEntity(0, 0)), 0, 0);
    var reopened = EditorSession.Open(s.Export(), "fixture.sav");
    Assert(reopened.GetEntity(0, 0).CurrentLevel == 42, "Save level roundtrip " + version);
    Assert(reopened.Save!.ChecksumsValid, "Save checksum " + version);
    Assert(s.Original.SequenceEqual(input), "Save original changed");
    s.Undo(); Assert(s.GetEntity(0, 0).CurrentLevel == 10, "Save undo failed");
    s.Reset(); Assert(s.Export().SequenceEqual(input), "Save reset failed");

    // Creating is a draft operation. It must not write, consume an undo step,
    // overwrite an existing Pokémon, or depend on an imported PKM file.
    var created = s.CreateEntity(25, 0, 2);
    Assert(!s.Dirty && !s.CanUndo && s.GetEntity(0, 2).Species == 0 && s.Export().SequenceEqual(input), "Creation wrote before commit " + version);
    Assert(created.GetType() == s.Save!.BlankPKM.GetType() && created.Species == 25 && created.CurrentLevel == 5, "Creation format or defaults " + version);
    Assert(created.OriginalTrainerName == s.Save.OT && created.TID16 == s.Save.TID16, "Creation trainer " + version);
    if (created.Format >= 3) Assert(created.SID16 == s.Save.SID16 && created.Version == s.Save.Version, "Creation origin " + version);
    Assert(created.Move1 != 0 && created.ChecksumValid && created.IsGenderValid(), "Creation basic data " + version);
    if (created is ITeraType tera) Assert(tera.TeraType == (MoveType)created.PersonalInfo.Type1, "Creation Tera type " + version);
    var createdBytes = EditorSession.ExportEntity(created);
    var standalone = EditorSession.Open(createdBytes, "created." + created.Extension);
    Assert(standalone.Entity!.Species == 25 && standalone.Entity.CurrentLevel == 5 && standalone.Entity.ChecksumValid, "Created PKM export roundtrip " + version);
    var restoredDraft = s.RestoreCreationDraft(createdBytes, 0, 2);
    Assert(EditorSession.ExportEntity(restoredDraft).SequenceEqual(createdBytes), "Creation draft restoration changed data " + version);
    Reject(() => s.RestoreCreationDraft([1, 2, 3], 0, 2), "Junk creation draft accepted");
    Reject(() => s.CreateEntity(0, 0, 2), "Empty species created");
    Reject(() => s.CreateEntity((ushort)(created.MaxSpeciesID + 1), 0, 2), "Unavailable species created");
    Reject(() => s.CreateEntity(25, 0, s.Save.BoxSlotCount), "Creation outside box accepted");
    RejectOccupied(() => s.CreateEntity(25, 0, 0), "Creation overwrote occupied place");
    Assert(!s.Dirty && !s.CanUndo && s.Export().SequenceEqual(input), "Rejected creation changed save " + version);
    var editedCreation = Edit(created, 30).Apply(created);
    s.Commit(editedCreation, 0, 2);
    var createdSave = EditorSession.Open(s.Export(), "fixture.sav");
    // The GB games serialize compact lists rather than boxes with fixed gaps.
    var exportedSlot = created.Format <= 2 ? 1 : 2;
    var savedCreation = createdSave.GetEntity(0, exportedSlot);
    Assert(savedCreation.CurrentLevel == 30 && savedCreation.Nickname == "ANDROID" && createdSave.Save!.ChecksumsValid,
        $"Creation save roundtrip {version}: species={savedCreation.Species}, level={savedCreation.CurrentLevel}, nickname={savedCreation.Nickname}, checksums={createdSave.Save!.ChecksumsValid}");
    Assert(s.Original.SequenceEqual(input), "Creation changed original " + version);
    s.Undo(); Assert(s.GetEntity(0, 2).Species == 0 && s.GetEntity(0, 0).CurrentLevel == 10, "Creation undo did not restore empty place " + version);
    s.Reset();

    if (s.Save!.HasParty)
    {
        var nextSlot = s.Save.PartyCount;
        Assert(nextSlot == 0 && s.GetEntity(0, nextSlot, true).Species == 0, "Empty team has no free place " + version);
        Reject(() => s.CreateEntity(25, 0, nextSlot + 1, true), "Party creation allowed gap");
        var teammate = s.CreateEntity(25, 0, nextSlot, true);
        Assert(!s.Dirty && s.Save.PartyCount == nextSlot, "Party draft changed count " + version);
        s.Commit(teammate, 0, nextSlot, true);
        var teamSave = EditorSession.Open(s.Export(), "fixture.sav");
        Assert(teamSave.Save!.PartyCount == 1 && teamSave.GetEntity(0, 0, true).Species == 25 && teamSave.Save.ChecksumsValid, "Party creation roundtrip " + version);
        Assert(teamSave.GetEntity(0, 0, true).Stat_HPCurrent > 0 && teamSave.GetEntity(0, 0, true).Stat_HPMax > 0, "New party Pokémon not healed " + version);
        RejectOccupied(() => s.CreateEntity(25, 0, 0, true), "Party creation overwrote Pokémon");
        s.Undo(); Assert(s.Save!.PartyCount == 0 && s.GetEntity(0, 0, true).Species == 0, "Party creation undo " + version);
        s.Reset();
        // Import still works at the same newly available party place.
        s.ImportEntity(createdBytes, "created." + created.Extension, 0, 0, true);
        Assert(s.Save!.PartyCount == 1 && s.GetEntity(0, 0, true).Species == 25, "Import into empty team failed " + version);
        for (var index = 1; index < 6; index++) s.Commit(s.CreateEntity(25, 0, index, true), 0, index, true);
        var fullParty = s.Export();
        Reject(() => s.CreateEntity(25, 0, 6, true), "Seventh party Pokémon accepted");
        Assert(s.Save!.PartyCount == 6 && s.Export().SequenceEqual(fullParty), "Party capacity rejection changed save " + version);
        s.Reset();
    }
    s.ImportEntity(EditorSession.ExportEntity(pk), "Pikachu." + pk.Extension, 0, 1);
    Assert(s.GetEntity(0, 1).Species == 25, "Slot import failed");
    var beforeInvalid = s.Export();
    Reject(() => s.ImportEntity([1, 2, 3], "junk.pk9", 0, 1), "Invalid PKM import accepted");
    Assert(s.Export().SequenceEqual(beforeInvalid), "Invalid import changed save");
    File.WriteAllBytes(Path.Combine(outputPath, version + ".sav"), s.Export());
    Console.WriteLine("PASS save " + version);
}
Console.WriteLine($"ALL PASS: {assertions} assertions, {formats.Length} Pokémon formats, {games.Length} game saves.");
