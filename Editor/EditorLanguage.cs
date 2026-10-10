// SPDX-License-Identifier: GPL-3.0-or-later
using PKHeX.Core;
using PKHeX.Android.Localization;

namespace PKHeX.Android.Editor;

public static class EditorLanguage
{
    public static GameStrings Use(string code)
    {
        AppText.Language = code;
        var strings = GameInfo.GetStrings(AppText.Language);
        GameInfo.CurrentLanguage = AppText.Language;
        GameInfo.Strings = strings;
        return strings;
    }
}
