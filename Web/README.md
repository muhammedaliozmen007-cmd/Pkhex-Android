# PKHeX Web – GitHub Pages

Browser-Oberfläche für den bestehenden, unveränderten PKHeX.Core über Blazor WebAssembly.

## Hosting
- Öffentliche Projektadresse: https://muhammedaliozmen007-cmd.github.io/Pkhex-Android/
- Unter **Settings → Pages → Build and deployment → Source** muss **GitHub Actions** ausgewählt sein.
- Danach baut und veröffentlicht `.github/workflows/pages.yml` bei Änderungen auf `main`.
- Alternativ: **Actions → PKHeX Web on GitHub Pages → Run workflow**.
- Der Workflow verwendet den Unterpfad `/Pkhex-Android/`. Bei Umbenennung des Repositories auch `wwwroot/index.html` (base href) anpassen.

## Funktionen
- Entschlüsselte Spielstände und unterstützte Pokémon-Dateien importieren (max. 32 MiB)
- Boxen / Team ansehen, Pokémon erstellen, bearbeiten, importieren, exportieren
- Level, Art, Spitzname, Wesen, Item, Fähigkeit, Shiny, IVs, EVs, Attacken bearbeiten
- Trainername / Geld, Rückgängig, Original wiederherstellen, Legalitätsbericht
- Ausgabe eines bearbeiteten Spielstands oder einer einzelnen Pokémon-Datei

Alle Dateiinhalte werden **ausschließlich im Browser** verarbeitet. Es gibt keinen Upload an GitHub und keine cloudbasierte Speicherung/Synchronisierung der Spielstände. GitHub Pages ist ein statischer Hoster und kein Backend. Browser-Refresh beendet die Sitzung; vorher exportieren. Eine echte synchronisierte Cloudspeicherung benötigt zusätzliche Authentifizierung, einen separaten Speicherdienst und ein bewusstes Datenschutzkonzept. Kein ROM-/verschlüsseltes Save-Format und keine vollständige Windows-PKHeX-Oberfläche.

## Lokaler Build

```sh
dotnet restore Web/PKHeX.Web.csproj
dotnet publish Web/PKHeX.Web.csproj -c Release -o publish
```

Danach ist `publish/wwwroot` der statische Website-Ordner. Für einen lokalen Entwicklungsserver kann `dotnet run --project Web/PKHeX.Web.csproj` verwendet werden (ggf. DevServer-Paket installieren).

## Lizenz
PKHeX.Core von kwsch/PKHeX, dieses Community-Web-Frontend unter GPL-3.0-or-later. Nicht offiziell.
