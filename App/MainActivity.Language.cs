// SPDX-License-Identifier: GPL-3.0-or-later
using Android.App;
using Android.Content;
using PKHeX.Android.Editor;
using static PKHeX.Android.Localization.AppText;

namespace PKHeX.Android.App;

public sealed partial class MainActivity
{
    private void SetUiLanguage(string code)
    {
        strings = EditorLanguage.Use(code);
    }

    private void ChooseLanguage()
    {
        AlertDialog? dialog = null;
        dialog = new AlertDialog.Builder(this).SetTitle("Sprache / Language")!
            .SetSingleChoiceItems(["Deutsch", "English"], Language == "de" ? 0 : 1, (_, e) => Safe(() =>
            {
                dialog!.Dismiss();
                SwitchLanguage(e.Which == 0 ? "de" : "en");
            }))!.SetNegativeButton(T("Abbrechen", "Cancel"), (_, _) => { })!.Create()!;
        dialog.Show();
    }

    private void SwitchLanguage(string code)
    {
        if (Language == code) return;
        // Keep raw input, including incomplete numbers. A language change must
        // neither validate/commit a draft nor replace its Pokémon language.
        var input = fields.ToDictionary(p => p.Key, p => p.Value.Text ?? "");
        var ids = new Dictionary<string, int>(choices);
        var wasDirty = draftDirty;
        var useNickname = nicknamed?.Checked ?? false;
        var isShiny = shiny?.Checked ?? false;
        var scrollY = pageScroll?.ScrollY ?? 0;
        SetUiLanguage(code);
        GetSharedPreferences("settings", FileCreationMode.Private)!.Edit()!.PutString("language", Language)!.Apply();
        openButton!.Text = T("Öffnen", "Open");
        menuButton!.ContentDescription = T("Menü mit Update und Werkzeugen", "Menu with updates and tools");
        Render();
        if (wasDirty) RestoreEditorInputs(input, ids, useNickname, isShiny);
        pageScroll!.Post(() => pageScroll.ScrollTo(0, scrollY));
        Persist();
    }

    private void RestoreEditorInputs(Dictionary<string, string>? input, Dictionary<string, int>? ids,
        bool useNickname, bool isShiny)
    {
        if (selected is null) return;
        building = true;
        try
        {
            if (ids is not null) foreach (var pair in ids) choices[pair.Key] = pair.Value;
            if (input is not null) foreach (var pair in input)
                if (fields.TryGetValue(pair.Key, out var field)) field.Text = pair.Value;
            if (nicknamed is not null) nicknamed.Checked = useNickname;
            if (shiny is not null) shiny.Checked = isShiny;
        }
        finally { building = false; }
        draftDirty = true;
        RefreshChoiceLabels();
    }
}
