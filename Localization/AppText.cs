// SPDX-License-Identifier: GPL-3.0-or-later
namespace PKHeX.Android.Localization;

/// <summary>UI language only; never changes a Pokémon's stored language.</summary>
public static class AppText
{
    private static volatile string language = "de";
    public static string Language
    {
        get => language;
        set => language = Normalize(value);
    }

    public static string Normalize(string? value) =>
        string.Equals(value?.Split('-', '_')[0], "de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";

    public static string T(string german, string english) => Language == "de" ? german : english;
}
