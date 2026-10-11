# PKHeX Web – GitHub Pages

Browser-Oberfläche für den bestehenden, unveränderten PKHeX.Core über Blazor WebAssembly.

## Hosting
- Öffentliche Projektadresse: https://muhammedaliozmen007-cmd.github.io/Pkhex-Android/
- **Einmalig erforderlich:** Unter **Settings → Pages → Build and deployment → Source** muss **GitHub Actions** ausgewählt sein. Der normale GitHub-Actions-Token darf GitHub Pages **nicht selbst aktivieren**.
- Danach baut und veröffentlicht `.github/workflows/pages.yml` bei Änderungen auf `main`.
- Alternativ: **Actions → PKHeX Web on GitHub Pages → Run workflow**.
- Der Workflow verwendet den Unterpfad `/Pkhex-Android/`. Bei Umbenennung des Repositories auch `wwwroot/index.html` (base href) anpassen.

## Fehlerbehebung bei der Bereitstellung
- **HTTP 404 in Deploy**: Meist ist GitHub Pages noch nicht aktiviert. Die Pages-Einstellung ist von GitHub Actions unabhängig und muss im Repository konfiguriert werden.
- **Multiple artifacts named github-pages**: Das trat in einem früheren Workflow auf, wenn alle Jobs desselben Runs erneut ausgeführt wurden. Der jetzige Workflow verwendet nur einen Job und einzigartige Artefaktnamen je Ausführungsversuch.
- Nach der einmaligen Aktivierung kannst du einen neuen Workflow-Run starten. **Re-run failed jobs** funktioniert im aktuellen Workflow ebenfalls, da Build und Deploy im selben Job laufen.
- **Erfolgreicher Build, fehlgeschlagener Deploy** bedeutet nicht, dass die Web-App bereits öffentlich erreichbar ist.

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
