using PKHeX.Core;

namespace PKHeX.Android.Editor;

public sealed class EditorSession
{
    public const int MaxFileSize = 32 * 1024 * 1024;
    public SaveFile? Save { get; private set; }
    public PKM? Entity { get; private set; }
    public string FileName { get; private set; } = "";
    public byte[] Original { get; private set; } = [];
    public bool Dirty { get; private set; }
    private readonly List<(SaveFile? Save, PKM? Entity)> history = [];
    public bool CanUndo => history.Count != 0;

    public static EditorSession Open(byte[] bytes, string fileName)
    {
        if (bytes.Length == 0 || bytes.Length > MaxFileSize)
            throw new ArgumentException("Die Datei ist leer oder größer als 32 MB.");
        var s = new EditorSession { FileName = Path.GetFileName(fileName), Original = (byte[])bytes.Clone() };
        s.Save = SaveUtil.GetSaveFile((byte[])bytes.Clone(), s.FileName);
        if (s.Save is not null)
        {
            s.Save.Metadata.SetExtraInfo(s.FileName);
            if (s.Save.Generation <= 3) SaveLanguage.TryRevise(s.Save);
            return s;
        }
        var hint = EntityFileExtension.GetContextFromExtension(Path.GetExtension(fileName));
        s.Entity = EntityFormat.GetFromBytes((byte[])bytes.Clone(), hint);
        if (s.Entity is null || s.Entity.Species == 0 || s.Entity.Species > s.Entity.MaxSpeciesID || !s.Entity.ChecksumValid)
            throw new ArgumentException("Kein unterstützter Spielstand oder keine gültige Pokémon-Datei. Bitte einen entschlüsselten Spielstand auswählen, keine ROM oder ZIP-Datei.");
        return s;
    }

    public void RestoreOriginal(byte[] original, bool dirty) { Original = (byte[])original.Clone(); Dirty = dirty; }

    public PKM GetEntity(int box, int slot, bool party = false)
    {
        if (Save is null) return Entity?.Clone() ?? throw new InvalidOperationException("Keine Datei geöffnet.");
        ValidateSlot(box, slot, party);
        // Slots beyond PartyCount can contain stale data. Only the next contiguous
        // place is available for adding a Pokémon; never expose those stale bytes.
        if (party && slot == Save.PartyCount) return Save.BlankPKM;
        return (party ? Save.GetPartySlotAtIndex(slot) : Save.GetBoxSlotAtIndex(box, slot)).Clone();
    }

    private void ValidateSlot(int box, int slot, bool party)
    {
        var sav = Save ?? throw new InvalidOperationException("Kein Spielstand geöffnet.");
        if (party)
        {
            if (!sav.HasParty || slot < 0 || slot >= 6 || slot > sav.PartyCount) throw new ArgumentOutOfRangeException(nameof(slot));
        }
        else if (!sav.HasBox || box < 0 || box >= sav.BoxCount || slot < 0 || slot >= sav.BoxSlotCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
    }

    private void CheckWritable(int box, int slot, bool party)
    {
        ValidateSlot(box, slot, party);
        if (!party && Save!.IsBoxSlotOverwriteProtected(box, slot))
            throw new InvalidOperationException("Dieser Platz ist vom Spiel geschützt.");
    }

    private SaveFile CheckEmptySlot(int box, int slot, bool party)
    {
        CheckWritable(box, slot, party);
        if (GetEntity(box, slot, party).Species != 0)
            throw new InvalidOperationException("Dieser Platz ist bereits belegt. Wähle einen freien Platz.");
        return Save!;
    }

    /// <summary>Creates an editable draft without writing to the save or undo history.</summary>
    public PKM CreateEntity(ushort species, int box, int slot, bool party = false)
    {
        var sav = CheckEmptySlot(box, slot, party);
        var pk = sav.BlankPKM;
        // Some older formats normalize out-of-range species in their setter.
        // Validate the requested ID before assigning it to the format object.
        if (species == 0 || species > pk.MaxSpeciesID || !sav.Personal.IsPresentInGame(species, 0))
            throw new ArgumentException("Diese Pokémon-Art ist in diesem Spiel nicht verfügbar.");
        pk.Species = species;
        ValidateCandidate(pk);
        var language = (int)BlankSaveFile.GetSafeLanguage(sav);
        if (language is 0 or 6) language = (int)LanguageID.English;
        // Trainer names in older formats must be encoded using the right language.
        pk.Language = language; sav.ApplyTo(pk); pk.Language = language;
        if (pk.Format >= 3 && (!pk.Version.IsValidSavedVersion() || sav.Version == GameVersion.BATREV))
            pk.Version = pk.Context.GetSingleGameVersion();
        pk.CurrentLevel = 5;
        pk.IsNicknamed = false; pk.ClearNickname();
        pk.SetRandomIVs();
        pk.Gender = pk.PersonalInfo.RandomGender();
        if (pk.Format >= 3)
        {
            pk.Nature = Nature.Hardy; pk.SetPIDNature(Nature.Hardy); pk.RefreshAbility(0);
            if (pk.Format >= 6) pk.EncryptionConstant = Util.Rand.Rand32();
        }
        pk.OriginalTrainerFriendship = pk.PersonalInfo.BaseFriendship;
        if (pk is ITeraType tera)
        {
            tera.TeraTypeOriginal = (MoveType)pk.PersonalInfo.Type1;
            tera.TeraTypeOverride = (MoveType)TeraTypeUtil.OverrideNone;
        }
        pk.MetLevel = 5; pk.MetDate = DateOnly.FromDateTime(DateTime.Today);
        pk.Ball = (byte)Ball.Poke;
        // Supply the game's level-up moves, leaving all values editable.
        var version = sav.Version == GameVersion.BATREV ? pk.Version : sav.Version;
        EncounterUtil.SetEncounterMoves(pk, version, 5);
        pk.Heal(); pk.RefreshChecksum();
        return pk;
    }

    public PKM RestoreCreationDraft(byte[] bytes, int box, int slot, bool party = false)
    {
        var sav = CheckEmptySlot(box, slot, party);
        var pk = EntityFormat.GetFromBytes((byte[])bytes.Clone(), sav.Context);
        if (pk is null || !pk.ChecksumValid)
            throw new ArgumentException("Der Pokémon-Entwurf ist ungültig.");
        ValidateCandidate(pk);
        return pk.Clone();
    }

    private void ValidateCandidate(PKM pk)
    {
        if (pk.Species == 0 || pk.Species > pk.MaxSpeciesID)
            throw new ArgumentException("Ungültige Pokémon-Art.");
        if (Save is null) return;
        if (pk.GetType() != Save.BlankPKM.GetType())
            throw new ArgumentException("Das Pokémon-Format passt nicht zu diesem Spielstand.");
        if (!Save.Personal.IsPresentInGame(pk.Species, pk.Form))
            throw new ArgumentException("Diese Pokémon-Art oder Form ist in diesem Spiel nicht verfügbar.");
    }

    private void Snapshot()
    {
        if (history.Count == 3) history.RemoveAt(0);
        history.Add((Save?.Clone(), Entity?.Clone()));
        Dirty = true;
    }

    public void Commit(PKM candidate, int box, int slot, bool party = false)
    {
        ValidateCandidate(candidate);
        var pk = candidate.Clone();
        pk.RefreshChecksum();
        if (Save is null) { Snapshot(); Entity = pk; return; }
        CheckWritable(box, slot, party);
        // Apply to a clone so a failed game-specific write never corrupts the current session.
        var next = Save.Clone();
        if (party) next.SetPartySlotAtIndex(pk, slot, EntityImportSettings.None);
        else next.SetBoxSlotAtIndex(pk, box, slot, EntityImportSettings.None);
        Snapshot(); Save = next;
    }

    public void ImportEntity(byte[] bytes, string fileName, int box, int slot, bool party = false)
    {
        CheckWritable(box, slot, party);
        var pk = EntityFormat.GetFromBytes(bytes, EntityFileExtension.GetContextFromExtension(Path.GetExtension(fileName), Save!.Context));
        if (pk is null || pk.Species == 0 || pk.Species > pk.MaxSpeciesID || !pk.ChecksumValid)
            throw new ArgumentException("Die Pokémon-Datei ist ungültig.");
        // This initial port intentionally requires the matching format; no implicit generation conversion.
        Commit(pk, box, slot, party);
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var item = history[^1]; history.RemoveAt(history.Count - 1);
        Save = item.Save; Entity = item.Entity;
        Dirty = true;
    }

    public void Reset()
    {
        var restored = Open(Original, FileName);
        Save = restored.Save; Entity = restored.Entity; Dirty = false; history.Clear();
    }

    public byte[] Export() => !Dirty ? (byte[])Original.Clone() : Save is not null ? Save.Clone().Write().ToArray() : ExportEntity(Entity!);
    public static byte[] ExportEntity(PKM pk)
    {
        var data = new byte[Math.Max(pk.SIZE_STORED, 1024)];
        var length = pk.WriteDecryptedDataStored(data);
        return data[..length];
    }

    public void EditTrainer(string name, uint money)
    {
        var sav = Save ?? throw new InvalidOperationException("Kein Spielstand geöffnet.");
        if (name.Length == 0 || name.Length > sav.MaxStringLengthTrainer)
            throw new ArgumentException($"Trainername: 1–{sav.MaxStringLengthTrainer} Zeichen.");
        if (money > sav.MaxMoney) throw new ArgumentException($"Geld: 0–{sav.MaxMoney}.");
        var next = sav.Clone(); next.OT = name; next.Money = money;
        Snapshot(); Save = next;
    }
}

public sealed record PokemonEdit(ushort Species, string Nickname, bool IsNicknamed, byte Level,
    int HeldItem, Nature Nature, int AbilitySlot, bool Shiny, int[] IVs, int[] EVs, ushort[] Moves)
{
    public PKM Apply(PKM original)
    {
        if (Species == 0 || Species > original.MaxSpeciesID) throw new ArgumentException("Ungültige Pokémon-Art.");
        if (Nickname.Length > original.MaxStringLengthNickname) throw new ArgumentException($"Spitzname: maximal {original.MaxStringLengthNickname} Zeichen.");
        if (IsNicknamed && string.IsNullOrWhiteSpace(Nickname)) throw new ArgumentException("Bitte einen Spitznamen eingeben oder Spitzname deaktivieren.");
        if (Level is < 1 or > 100) throw new ArgumentException("Level: 1–100.");
        if (HeldItem < 0 || HeldItem > Math.Max(0, original.MaxItemID)) throw new ArgumentException("Ungültiges Item.");
        if ((uint)Nature >= 25 || AbilitySlot is < 0 or > 2) throw new ArgumentException("Ungültiges Wesen oder Fähigkeit.");
        if (IVs.Length != 6 || IVs.Any(v => v < 0 || v > original.MaxIV)) throw new ArgumentException($"IVs: 0–{original.MaxIV}.");
        if (EVs.Length != 6 || EVs.Any(v => v < 0 || v > original.MaxEV)) throw new ArgumentException($"EVs: 0–{original.MaxEV}.");
        if (original.Format >= 3 && EVs.Sum() > 510) throw new ArgumentException("EVs dürfen zusammen höchstens 510 ergeben.");
        if (Moves.Length != 4 || Moves.Any(m => m > original.MaxMoveID)) throw new ArgumentException("Ungültige Attacke.");
        var pk = original.Clone();
        if (pk.Species != Species) { pk.Species = Species; pk.Form = 0; }
        pk.IsNicknamed = IsNicknamed;
        if (IsNicknamed) pk.Nickname = Nickname;
        else pk.ClearNickname();
        pk.CurrentLevel = Level; pk.HeldItem = HeldItem;
        if (pk.Format >= 3)
        {
            if (pk.Format <= 4 && pk.Nature != Nature) pk.SetPIDNature(Nature);
            else pk.Nature = Nature;
            var abilities = pk.PersonalInfo.AbilityCount;
            if (AbilitySlot >= abilities) throw new ArgumentException("Diese Fähigkeit existiert für dieses Pokémon nicht.");
            if (pk.Format is >= 3 and <= 5 && AbilitySlot < 2 && pk.PIDAbility != AbilitySlot && pk.Version != GameVersion.CXD)
            {
                uint bit = pk.Gen5 ? 0x10000u : 1u;
                var targetPID = (pk.PID & ~bit) | (AbilitySlot == 1 ? bit : 0u);
                pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Form, targetPID);
            }
            pk.RefreshAbility(AbilitySlot);
        }
        (pk.IV_HP, pk.IV_ATK, pk.IV_DEF, pk.IV_SPA, pk.IV_SPD, pk.IV_SPE) = (IVs[0], IVs[1], IVs[2], IVs[3], IVs[4], IVs[5]);
        (pk.EV_HP, pk.EV_ATK, pk.EV_DEF, pk.EV_SPA, pk.EV_SPD, pk.EV_SPE) = (EVs[0], EVs[1], EVs[2], EVs[3], EVs[4], EVs[5]);
        (pk.Move1, pk.Move2, pk.Move3, pk.Move4) = (Moves[0], Moves[1], Moves[2], Moves[3]);
        // Existing PP/PP Ups stay untouched unless the move itself changed.
        if (Moves[0] != original.Move1) { pk.Move1_PPUps = 0; pk.Move1_PP = pk.GetMovePP(Moves[0], 0); }
        if (Moves[1] != original.Move2) { pk.Move2_PPUps = 0; pk.Move2_PP = pk.GetMovePP(Moves[1], 0); }
        if (Moves[2] != original.Move3) { pk.Move3_PPUps = 0; pk.Move3_PP = pk.GetMovePP(Moves[2], 0); }
        if (Moves[3] != original.Move4) { pk.Move4_PPUps = 0; pk.Move4_PP = pk.GetMovePP(Moves[3], 0); }
        if (pk.Format >= 2 && Shiny != pk.IsShiny)
        {
            if (Shiny) pk.SetShiny();
            else if (pk is GBPKM) pk.IV_DEF = pk.IV_DEF == 10 ? 9 : pk.IV_DEF;
            else pk.SetPIDNature(pk.Nature);
        }
        pk.ResetPartyStats(); pk.RefreshChecksum();
        return pk;
    }
}
