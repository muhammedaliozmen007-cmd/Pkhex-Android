using PKHeX.Core;
using PKHeX.Android.Editor;

// A format-valid visual fixture, deliberately identified as DEMO throughout the UI.
// It is not a completed in-game save and does not claim encounter legality.
var save = BlankSaveFile.Get(GameVersion.X);
save.OT = "VORSCHAU"; save.Language = (int)LanguageID.German;
ushort[] species = [25, 6, 133, 150, 9, 3, 94, 448, 149, 282, 257, 384, 245, 248, 700, 1, 4, 7, 143, 54];
for (var i = 0; i < species.Length; i++)
{
    var pk = new PK6 { Species = species[i], Version = GameVersion.X, Language = (int)LanguageID.German,
        OriginalTrainerName = "VORSCHAU", TID16 = 12345, SID16 = 54321, PID = (uint)(0x12345678 + i), Nature = Nature.Hardy };
    pk.CurrentLevel = (byte)(25 + i * 3); pk.ClearNickname(); pk.RefreshAbility(0);
    pk.IV_HP = 31; pk.IV_ATK = 31; pk.IV_DEF = 25; pk.IV_SPA = 31; pk.IV_SPD = 25; pk.IV_SPE = 31;
    pk.Move1 = 33; pk.Move1_PP = 35;
    if (i is 2 or 5 or 10) pk.SetShiny();
    pk.RefreshChecksum(); save.SetBoxSlotAtIndex(pk, 0, i, EntityImportSettings.None);
}
var bytes = save.Write().ToArray();
System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(bytes.Length - 0x1F0), 0x42454546);
var session = EditorSession.Open(bytes, "demo.sav");
if (session.Save is null || session.GetEntity(0, 0).Species != 25 || !session.GetEntity(0, 2).IsShiny)
    throw new Exception("Demo verification failed");
var path = args.Length > 0 ? args[0] : "App/Assets/demo.sav";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!); File.WriteAllBytes(path, bytes);
Console.WriteLine("PASS demo: 20 Pokémon, importable save, normal and shiny previews");
