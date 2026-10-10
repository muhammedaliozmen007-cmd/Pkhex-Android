# Validation of version 0.4.2

## Build and package

- Final signed Android Release build: **0 errors, 0 warnings**.
- .NET SDK 10.0.401, .NET Android 36.1.2, Android SDK/API 36, Build-Tools 36.0.0, JDK 21.0.12.1.
- Package `org.pkhex.android.community`, version **0.4.2**, version code **6**.
- Minimum Android API 26; target API 36; ARM64, ARM32 and x86-64.
- Valid APK v2 and v3 signatures, with the same certificate as 0.4.1:
  `9f5116bd6d1d408d8b5d110bdb32b972318523a1d2caa09f74a9bf80fe3624e1`.
- APK size: **148,912,889 bytes**.
- APK SHA-256: `0ab53fb6049b4ce72c593aa5ac67058aaddc73eed00bcaa16b72427a7e81e4b2`.
- All APK ZIP entries passed integrity checks, including **3,258 offline artwork files**.
- Embedded update channel points to `muhammedaliozmen007-cmd/Pkhex-Android`.

## Automated checks

- **446 editor/file checks**, covering 12 Pokémon formats and eight synthetic game saves.
- **43 update checks**, including English and German update-source validation errors.
- Language checks cover device-language normalization, English fallback, switching back,
  species/nature/ability/move/item names, editor validation errors and legality report language.
- Switching language leaves the tested save bytes, undo state, Pokémon draft, nickname
  and stored Pokémon language unchanged.
- Syntax inspection: **219 bilingual text calls**, with no unlocalized German string literals
  outside the deliberately bilingual language selector.
- `git diff --check` passed.

## UI review and limits

The language action is available before and after opening a file. The selected language
is stored separately from the save session. Changing it rebuilds the current content and
updates the retained header, while restoring raw field text, numeric choice IDs, nickname
and shiny checkboxes, editor tab and scroll position. This includes incomplete numeric
input and new Pokémon drafts; it does not call Apply or discard the draft.

The Android code compiled successfully. No physical Android device or emulator was
available for an interactive UI test; the input restoration and preference persistence
were reviewed in source, rather than verified by tapping through a device session.

---

# Historical validation: version 0.4.1

## APK und Android-Build

- .NET SDK 10.0.401, .NET-Android 36.1.2, Android SDK/Build-Tools 36, JDK 21.
- Abschließender Release-Build: **0 Fehler, 0 Warnungen**.
- Paket `org.pkhex.android.community`, Versionscode **5**, Version **0.4.1**.
- Android **8.0 / API 26** oder neuer, Ziel-API 36.
- ARM64, ARM32 und x86-64 in einer APK; Paketmetadaten mit `aapt2` geprüft.
- Gültige v2- und v3-Signatur; derselbe Entwicklungsschlüssel wie 0.1.0 bis 0.4.0.
- Signaturzertifikat SHA-256:
  `9f5116bd6d1d408d8b5d110bdb32b972318523a1d2caa09f74a9bf80fe3624e1`.
- APK-Größe: **148,888,313 Bytes**.
- APK-SHA-256: `301543e52dfb42b36c8c1543edcd6aacb371e37be42dcfdf86002b647a5f1f2c`.
- ZIP-Integrität aller APK-Einträge erfolgreich geprüft.
- Berechtigungen: `INTERNET` und `REQUEST_INSTALL_PACKAGES` für die vom Nutzer
  gestartete Update-Funktion. Keine allgemeine Speicherberechtigung.
- APK-Provider im kompilierten Manifest: nicht exportiert, URI-Leseberechtigung
  einzeln vergebbar. Der Provider liefert ausschließlich `updates/latest.apk`.

## Fehler in den Auswahllisten

- Den gemeldeten Fehlertyp mit einer festen Java-Liste nachgestellt: `addAll`
  auf `Arrays.asList(new String[0])` wirft `UnsupportedOperationException`.
- Der bisherige ArrayAdapter-Konstruktor mit einem String-Array verwendet laut
  Android-Quellcode genau eine solche Liste. Der leere Konstruktor verwendet
  `new ArrayList<>()`. Die .NET-Android-Bindung ruft hierfür ausdrücklich die
  passende native Überladung `(Landroid/content/Context;I)V` auf.
- In einem JVM-Regressionsexperiment alle vier Feldgruppen mehrfach mit vollständigen,
  gefilterten und leeren Ergebnissen in veränderbarem Listenspeicher aktualisiert.
- Alle eigenen Adapterstellen geprüft: nur der gemeinsame Text-Auswahldialog
  nutzte diese problematische Array-Überladung. Pokémon-Arten verwenden einen
  eigenen BaseAdapter; Box- und Menüauswahl mutieren ihre Listen nicht.
- Der gemeinsame Textdialog betrifft Wesen, Items, Fähigkeiten und alle vier
  Attackenplätze. Filterfehler nach der kurzen Suche werden ebenfalls über den
  Fehlerdialog abgefangen; Klicks außerhalb der aktuellen Ergebnisliste ignoriert.
- Beide bestehenden Prüfprogramme erneut ausgeführt: **427 Datei-/Bearbeitungsprüfungen
  und 41 Update-Prüfungen** bestanden.

Primärquellen für die Ursachenprüfung:
[Android ArrayAdapter](https://android.googlesource.com/platform/frameworks/base/+/e0938b8/core/java/android/widget/ArrayAdapter.java)
und [.NET-Android-Bindung](https://github.com/dotnet/android/blob/main/src/Mono.Android/Android.Widget/ArrayAdapter.cs).

Das JVM-Experiment prüft die Java-Listenoperationen und ersetzt keinen Test der
Android-Oberfläche oder JNI-Bindung auf einem Gerät. Der Fix wurde mit dem Android-SDK
kompiliert; eine Bedienungsprüfung auf einem Android-Gerät war hier nicht möglich.

## Update-Funktion

**41 bestandene Update-Prüfungen** ohne Netzwerkabhängigkeit:

- Neue Version erkennen, aktuelle Version melden, Herabstufungen verhindern.
- GitHub-Repository-URL und Git-Clone-URL normalisieren; fremde Hosts,
  Zugangsdaten in URLs, Pfadmanipulationen und ungewöhnliche Ports ablehnen.
- Vorabversionen, fehlende Update-Manifeste, falsche Assetgrößen,
  abweichende Hashangaben und fremde Download-Repositories ablehnen.
- Manifest-Schema, App-ID, Versionscode, APK-Dateiname, Größenlimit und SHA-256 prüfen.
- Vollständige APK herunterladen; beschädigte, zu große oder unvollständige
  Downloads erkennen; Teil-Dateien nach Abbruch oder Fehler entfernen.
- Eine vorhandene vollständige APK bei einem fehlgeschlagenen Download erhalten.
- Fehlende Releases und GitHub-Abfragelimits behandeln.

Die Android-Oberfläche prüft vor der Installation zusätzlich APK-Paketname,
Versionscode, Versionsname und denselben APK-Signaturschlüssel. Diese Prüfungen
und die Übergabe an den Android-Installer sind in dieser Umgebung ausschließlich
kompiliert und am Code/Manifest geprüft, noch nicht auf einem Android-Gerät durchlaufen.

**Update-Quelle:** Noch kein öffentliches APK-Repository für dieses Paket
veröffentlicht. Die APK enthält bewusst keine erfundene Standardadresse.
Einmalige Einrichtung über **☰ → Update**. `UPDATES.md` und der vollständige
GitHub-Release-Workflow sind enthalten. Workflow-Syntax, Python-Skripte und
Versions-/Kanal-Konfiguration wurden lokal mit isolierten Testdaten geprüft;
der Workflow ist noch nicht in einem echten Repository ausgeführt worden.

## Datei- und Bearbeitungsfunktionen

**427 bestandene Prüfungen** für zwölf Pokémon-Formate und acht Spielstandformate.

- PK1, PK2, PK3, PK4, PK5, PK6, PK7, PK8, PB8, PA8, PK9 und PA9.
- Synthetische Spielstände: Rot/Blau, Kristall, Smaragd, Platin, Schwarz 2/Weiß 2,
  X/Y, Sonne/Mond, Strahlender Diamant/Leuchtende Perle.
- Erkennung, Bearbeitung, erneutes Einlesen, Prüfsummen, Originalerhalt,
  Rückgängig, Zurücksetzen, Slot-Import und ungültige Dateien/Wertebereiche.
- EV-Gesamtsumme, Fähigkeits-/PID-Konsistenz in Generation 4/5 und deutsche
  Legalitätsberichte.
- Originales PKHeX.Core unverändert, Commit
  `66ef5983a5d349efa2dd615f2926a1b1ee97306e`.

## Pokémon direkt erstellen

Die Datei- und Bearbeitungsprüfungen decken zusätzlich ab:

- Neues Pokémon in einem leeren Boxplatz ohne PKM-Import vorbereiten.
- Vor dem Übernehmen weder den Spielstand noch den Rückgängig-Verlauf ändern.
- Passendes Format, Level 5, Trainerdaten, gültiges Geschlecht und Level-Attacken.
- Entwurf als Pokémon-Datei exportieren, erneut öffnen und unverändert wiederherstellen.
- Ungültige Daten, Arten, Slotgrenzen und belegte Plätze abweisen; dabei die
  bisherige Sitzung und Originaldatei erhalten.
- Entwurf bearbeiten, in den Spielstand übernehmen, exportieren und erneut lesen.
- Prüfsummen prüfen, die Originaldatei erhalten und den Platz mit Rückgängig leeren.
- In ein leeres Team erstellen und optional importieren, korrekte Teamanzahl und
  geheilte Kampfwerte beim erneuten Einlesen erhalten.
- Team ohne Lücken ergänzen, maximal sechs Pokémon zulassen und per Rückgängig
  die ursprüngliche Teamanzahl wiederherstellen.
- Boxlisten in Generation 1/2 beim Export entsprechend dem Spielformat verdichten.
- Die internationale Gen-3-Testdatei mit dem richtigen Trainer-Sprachmarker anlegen.

Die Android-Oberfläche wurde kompiliert und am Code auf die Wege Erstellen, Übernehmen,
Verwerfen, Importabbruch, Trainerdialogabbruch und Wiederherstellung geprüft.
Das Wiederherstellen neuer Entwürfe und aller Eingabefelder nach einem echten
Android-Prozessneustart sowie diese Dialogwege wurden noch nicht auf einem
Android-Gerät durchlaufen. Die Entwurfs-Serialisierung selbst ist in den
Dateiprüfungen enthalten. Insgesamt **468 bestandene Prüfungen** inklusive Updater.

## Darstellung und Optimierung

- Alle **3258 WebP-Bilder** weiterhin in der APK enthalten.
- Basisbilder für alle **1.025 Arten**, **468 Formzuordnungen** und die
  synthetische Vorschau mit 20 Pokémon sind enthalten.
- Bilddekodierung auf zwei Hintergrundarbeiter verlagert; passend verkleinerte
  Bitmaps und ein 24-MiB-LRU-Cache. Veraltete Bildanforderungen beim Navigieren
  oder Wiederverwenden einer Listenzeile werden abgebrochen.
- Suchlisten-Zeilen wiederverwendet, Sucheingaben kurz zusammengefasst.
- Editor-Eingaben aktualisieren Bilder/Typ-Chips nur bei relevanten Änderungen.
- Kopfzeile und Scroll-Container beim Navigieren wiederverwendet.
- Derselbe Spielstand wird beim Box-/Tab-Wechsel weder vollständig exportiert
  und geschrieben noch erneut auf Prüfsummen geprüft.
- „Über die App“ entfernt, Update als erster Eintrag im ☰-Menü ergänzt.

Eine aktuelle visuelle Android-Bedienungsprüfung und Messungen der Bildrate
konnten in dieser Umgebung nicht durchgeführt werden. Die Verbesserungen sind
am Code nachvollziehbar, ihre Wirkung auf die tatsächliche Flüssigkeit wurde
noch nicht auf einem Gerät gemessen. Es wird keine gemessene Bildrate behauptet.
Die APK bleibt eine inoffizielle Community-Testversion.
