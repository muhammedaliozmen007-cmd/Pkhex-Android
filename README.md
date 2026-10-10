# PKHeX Android 0.4.1 — inoffizieller Community-Port

Eine native Android-Oberfläche mit dem unveränderten **PKHeX.Core** aus
https://github.com/kwsch/PKHeX, Quellstand
`66ef5983a5d349efa2dd615f2926a1b1ee97306e`.

Diese Android-Version enthält grundlegende Editoren und eine für Smartphones
überarbeitete Oberfläche mit offline verfügbaren Pokémon-Bildern. Sie ist kein offizieller
Release von Kaphotics und hat noch nicht den vollständigen Funktionsumfang der
Windows-Anwendung.

## Repository und APK-Downloads

Das vollständige Projekt wird auf `main` in
[muhammedaliozmen007-cmd/Pkhex-Android](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android)
gepflegt. Die Ordner `App/`, `Editor/`, `Updates/`, `Tests/`, `UpdateTests/`,
`upstream/`, die Pokémon-Bilder und die Build-Skripte
sind enthalten.

Signierschlüssel gehören nicht in dieses öffentliche Repository. Für APK-Releases
die vier in [UPDATES.md](UPDATES.md) beschriebenen GitHub-Actions-Secrets hinterlegen.

Neue APKs, das Update-Manifest und der vollständige Quellcode erscheinen nach
einem erfolgreichen [Android-Build](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android/actions/workflows/android-release.yml)
unter [Releases](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android/releases).
In einer bereits installierten APK einmalig unter **☰ → Update** diese
Repository-URL eintragen. Künftig vom Workflow gebaute APKs enthalten sie bereits.

## Installation und Benutzung

- Android **8.0 oder neuer**. ARM64, ARM32 und x86-64 sind in einer APK enthalten.
- `PKHeX-Android-0.4.1.apk` auf das Gerät laden, öffnen und für die verwendete
  Download-/Datei-App bei Bedarf „Unbekannte Apps installieren“ erlauben.
- „Datei öffnen“ wählen: einen entschlüsselten Spielstand oder eine Pokémon-Datei.
  Die App liest Dateien über den Android-Dateidialog. Es sind keine allgemeinen
  Speicherberechtigungen erforderlich.
- In einem Spielstand: Box oder Team und anschließend einen Platz auswählen.
- Ein freier Platz bietet **„Pokémon erstellen“** und optional „Pokémon importieren“.
  Zum Erstellen eine verfügbare Art wählen, die Felder bearbeiten und **„Übernehmen“**
  antippen. Auch ein leeres Team zeigt einen freien Platz; maximal sechs Pokémon.
- Änderungen mit „Übernehmen“ bestätigen. Anschließend „Exportieren“
  verwenden und eine neue Ausgabedatei auswählen.
- „Original sichern“ schreibt eine bytegenaue Kopie der ursprünglich importierten
  Datei. „Zurücksetzen“ stellt diese Datei wieder her. Die letzten drei übernommenen
  Änderungen lassen sich während der laufenden Sitzung rückgängig machen.
- Das exportierte Ergebnis im Emulator bzw. auf der Konsole wieder importieren.
  Die App extrahiert keine Spielstände aus verschlüsselten Konsolen-Backups und
  greift nicht direkt auf Spiele oder Emulatoren zu.

## Neu in Version 0.4.1

- Fehler beim Öffnen und Aktualisieren der Listen für Wesen, Items, Fähigkeiten
  und Attacken behoben. Der gemeinsame Listenadapter verwendet veränderbare Daten.
- Wiederholte Suchwechsel und leere Trefferlisten unterstützen; verspätete Klicks
  außerhalb der aktuellen Trefferliste ignorieren.
- Die APK kann über die bisherige Testversion installiert werden; Paketkennung
  und Entwicklungsschlüssel bleiben gleich.

## Pokémon-Erstellung seit Version 0.4.0

- Pokémon in freien Box- und Teamplätzen direkt erstellen, ohne eine Pokémon-Datei
  zu importieren. Die Artenauswahl zeigt die im geöffneten Spiel verfügbaren Basisformen.
- Ein neues Pokémon beginnt als Entwurf mit Level 5, passenden Level-Attacken,
  Trainerdaten aus dem Spielstand und bearbeitbaren Werten.
- Erst „Übernehmen“ schreibt in den Spielstand. Beim Verlassen kann der Entwurf
  übernommen, verworfen oder weiterbearbeitet werden.
- Auch neue Entwürfe samt Eingaben nach einem App-Neustart wiederherstellen.
- Erstellen lässt sich rückgängig machen. Originaldatei und andere Plätze bleiben
  beim Anlegen eines Entwurfs erhalten. Teamplätze werden ohne Lücken ergänzt.
- Pokémon weiterhin optional importieren oder als einzelne Datei exportieren.

## Updates und Optimierungen seit Version 0.3.0

- **☰ → Update** prüft künftige Versionen und lädt die neueste APK mit
  Fortschrittsanzeige, Abbruchmöglichkeit und anschließender Android-Installation.
- Eine Update-Quelle einmalig in der App einrichten:
  `https://github.com/muhammedaliozmen007-cmd/Pkhex-Android`.
  Der enthaltene Release-Workflow veröffentlicht nach erfolgreichem Build
  neue APKs. Anleitung: [UPDATES.md](UPDATES.md).
- „Über die App“ aus dem Menü entfernt.
- Pokémon-Bilder außerhalb des Hauptthreads laden, für kleine Darstellungen
  passend verkleinern und in einem begrenzten LRU-Cache halten.
- Die Pokémon-Auswahlliste verwendet ihre Zeilen erneut und fasst schnelle
  Sucheingaben zusammen. Editor-Eingaben laden unveränderte Bilder nicht neu.
- Bildanforderungen für wiederverwendete Listenzeilen beim schnellen Scrollen abbrechen.
- Kopfzeile und Scroll-Container bleiben bei Ansichtswechseln erhalten.
- Box- und Tab-Wechsel schreiben den unveränderten Spielstand nicht erneut.
- Unveränderte Spielstände bei der Navigation nicht wiederholt auf Prüfsummen prüfen.

## Bilder und Oberfläche seit Version 0.2.0

- Bebilderte Boxübersicht und große Pokémon-Vorschau im Editor.
- HOME-Bilder für alle 1.025 Arten; verfügbare Shiny- und weibliche Varianten.
- Bild und Typanzeige aktualisieren sich beim Ändern der Art oder Shiny-Auswahl.
- Vier Editorbereiche: Basis, Attacken, Werte und Infos.
- Kompakte IV-/EV-Tabelle mit EV-Gesamtsumme.
- Feste Schaltflächen zum Übernehmen und Exportieren, Suchliste mit Pokémon-Bildern.
- Eine klar gekennzeichnete Vorschau mit 20 Beispiel-Pokémon. Diese synthetischen
  Beispieldaten sind kein spielbarer Komplett-Spielstand.
- Neues App-Symbol, klarere Karten, Navigation und Dialoge.

## Enthalten

- Import und Erkennung über die originale PKHeX.Core-Bibliothek.
- Box- und Teamübersicht, Pokémon-Erstellung und optionaler Import in freie Plätze.
- Einzelne Pokémon-Dateien separat öffnen und exportieren.
- Art, Spitzname, Level, Item, Wesen, Fähigkeit, Shiny, vier Attacken, IV/DV und EV.
- Trainername und Geld, sofern das jeweilige Spiel diese Eigenschaften bereitstellt.
- Deutsche Pokémon-Namen und deutsche Legalitätsberichte aus PKHeX.Core.
- Wertebereiche, EV-Gesamtsumme, geschützte Plätze und Spielverfügbarkeit prüfen.
- Korrekte Dateiformate und Prüfsummen über die originalen Core-Schreibfunktionen.
- Sitzung und noch nicht übernommene Eingabefelder lokal wiederherstellen.
- Spielstände bleiben lokal, kein Konto erforderlich. Nur die ausdrücklich
  gestartete Update-Prüfung und der APK-Download greifen auf das Internet zu.

## Grenzen dieser Version

- Kein vollständiger Desktop-Klon: spezielle Event-, Inventar-, Pokédex-, Ribbon-,
  Begegnungs- und spielabhängige Editoren sind noch nicht enthalten.
- 468 Formzuordnungen für die Bilder. Für weitere Formen wird die Basisabbildung
  gezeigt; der Editor weist darauf hin. Nicht jede Spezialvariante besitzt ein
  eigenes Bild. Die Bildquelle ist in `third-party/POKEMON-ARTWORK.md` dokumentiert.
- Bestehende Formen bleiben erhalten; ein Wechsel der Art setzt die Form auf 0.
- Keine automatische Konvertierung zwischen Pokémon-Generationen. Der Import in
  einen Spielstand verlangt das passende Pokémon-Format und eine verfügbare Art/Form.
- Änderungen machen ein Pokémon nicht automatisch legal. Den Legalitätsbericht prüfen.
- In Generation 1/2 sind KP-DVs sowie Spezialwerte gekoppelt; in Generation 2 ist
  Shiny an DVs gebunden und kann deren Werte verändern.
- In Generation 1/2 speichert das Spiel Box-Pokémon als zusammenhängende Liste;
  leere Lücken werden beim Export durch PKHeX.Core entfernt.
- Der Import ist auf 32 MiB begrenzt. Originale werden nur im privaten App-Speicher
  für die zuletzt geöffnete Sitzung gehalten. Für eine dauerhafte Sicherung
  „Original sichern“ verwenden. Android deinstallieren/App-Daten löschen entfernt die Sitzung.
- Die APK ist als persönliche Testversion mit einem Entwicklungs-
  schlüssel signiert. Dieser Schlüssel eignet sich nicht für eine öffentliche
  Store-Veröffentlichung. Für eine eigene Veröffentlichung einen privaten Schlüssel
  erzeugen und die Signierparameter ändern; für dessen erste Installation muss
  diese Testversion gegebenenfalls deinstalliert werden.

## Quellcode und Lizenz

`upstream/PKHeX.Core/` enthält den vollständigen verwendeten Core einschließlich
seiner eingebetteten Ressourcen. `upstream/Directory.Build.props`,
`upstream/COMMIT.txt` und `upstream/LICENSE` halten Version und Herkunft fest.
PKHeX.Core ist von Kaphotics und den PKHeX-Mitwirkenden. Die neue Android-Oberfläche
und Bearbeitungslogik in diesem Paket werden ebenfalls unter **GPL-3.0-or-later**
bereitgestellt. Siehe `LICENSE`. Quellcode und APK dürfen nach dieser Lizenz
weitergegeben und verändert werden; bei einer Weitergabe auch den entsprechenden
Quellcode und die Lizenz bereitstellen.

## Lokal bauen

Voraussetzungen: .NET SDK 10.0, .NET-Android-Workload, JDK 21 und Android SDK
mit Plattform 36 sowie Build-Tools 36.0.0.

```bash
dotnet workload install android
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project UpdateTests/UpdateTests.csproj -c Release
export ANDROID_SDK_ROOT=/pfad/zum/android-sdk
export JAVA_HOME=/pfad/zum/jdk-21
export ANDROID_SIGNING_KEYSTORE=/privater/pfad/zum/schluessel.keystore
export ANDROID_SIGNING_KEY_ALIAS=pkhex
# ANDROID_SIGNING_STORE_PASS und ANDROID_SIGNING_KEY_PASS privat setzen.
bash scripts/build.sh
```

Die signierte APK liegt danach unter
`App/bin/Release/net10.0-android/org.pkhex.android.community-Signed.apk`.

Die Tests erzeugen synthetische Pokémon- und Spielstanddateien. Diese Dateien
dienen nur zur Prüfung des Dateiformats und sind keine spielbaren Komplett-Spielstände.
Die tatsächlichen Prüfergebnisse der ausgelieferten Version stehen in `VALIDATION.md`.
