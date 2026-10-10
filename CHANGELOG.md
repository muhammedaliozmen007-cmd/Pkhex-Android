# 0.4.2

- Gespeicherte Sprachauswahl Deutsch/Englisch über „☰ → Sprache / Language“.
- Oberfläche, Validierung und Update-Dialoge sowie Core-Namen und Legalitätsberichte übersetzen.
- Offene Sitzung, neue Pokémon-Entwürfe und noch nicht übernommene Eingaben beim Sprachwechsel erhalten.
- README auf Englisch und Update-Repository standardmäßig hinterlegen.
- Versionscode 6; Paketkennung und Signaturschlüssel beibehalten.

# 0.4.1

- `Java.Lang.UnsupportedOperationException` in Wesen-, Item-, Fähigkeits- und
  allen vier Attacken-Auswahllisten behoben.
- Den gemeinsamen ArrayAdapter mit dem leeren Konstruktor anlegen; dieser besitzt
  eine veränderbare Java-Liste statt des festen Arrays der bisherigen Überladung.
- Wiederholtes Filtern einschließlich leerer Suchergebnisse bleibt möglich.
- Nach verzögerter Suche unerwartete Fehler über den App-Fehlerdialog behandeln
  und verspätete Klicks außerhalb der aktuellen Trefferliste abweisen.
- Alle eigenen Listenadapter und Menü-/Box-Auswahlen auf diese Fehlerursache geprüft.
- Versionscode auf 5 erhöht; Paketkennung und Signaturschlüssel beibehalten.

# 0.4.0

- „Pokémon erstellen“ als Hauptaktion in freien Plätzen; Import bleibt optional.
- Verfügbare Pokémon-Art wählen und direkt im bestehenden Editor bearbeiten.
- Level 5, Level-Attacken, Trainerdaten und Werte passend zum Spielstand vorbereiten.
- Erstellung als Entwurf: erst beim Übernehmen schreiben, beim Verlassen verwerfen
  oder übernehmen, Entwurf und Eingaben nach einem Neustart wiederherstellen.
- Nächsten freien Teamplatz anzeigen, auch bei einem vollständig leeren Team.
- Teamplätze ohne Lücken ergänzen und die Grenze von sechs Pokémon einhalten.
- Erstellen, Exportieren und Rückgängig mit dem ursprünglichen PKHeX.Core.
- Artenauswahl auf im geöffneten Spiel verfügbare Basisformen beschränken.
- Oberfläche nach verworfenen oder übernommenen Entwürfen vor Import- und
  Trainerdialogen aktualisieren, auch wenn der anschließende Dialog abgebrochen wird.
- Paketkennung und Entwicklungsschlüssel unverändert; Versionscode auf 4 erhöht.

# 0.3.0

- Neuer Update-Eintrag oben im ☰-Menü, auch auf der Startseite.
- GitHub-Release-Quelle einmalig einrichten; neuere Android-Versionen mit
  Fortschrittsanzeige herunterladen und den Android-Installer öffnen.
- Größe, SHA-256, Paketname, Versionscode und APK-Signaturschlüssel prüfen.
- Fertige Downloads für später behalten, abgebrochene Teil-Dateien entfernen.
- „Über die App“ entfernt; Lizenzen und Herkunft bleiben im Quellpaket erhalten.
- Bilder auf zwei Hintergrundarbeitern dekodieren, kleine Bitmaps passend skalieren
  und einen 24-MiB-LRU-Cache verwenden.
- Suchlisten-Zeilen wiederverwenden, schnelle Such-Eingaben zusammenfassen.
- Überholte Bildanforderungen beim schnellen Scrollen verwerfen.
- Bildvorschau und Typ-Chips nur bei betreffenden Änderungen aktualisieren.
- Kopfzeile und Scroll-Container wiederverwenden, unveränderte Spielstände bei
  der Navigation nicht erneut exportieren und schreiben.
- Prüfsummen desselben Spielstands beim Box-Wechsel nicht erneut berechnen.
- Release-Workflow für künftige APKs samt Quellcode und Update-Manifest vorbereitet.

# 0.2.0

- Offline verfügbare Pokémon-Bilder in Boxen, Suchliste und Editor.
- Große Live-Vorschau mit Typanzeige; Art und Shiny aktualisieren das Bild.
- Neue Startseite, Karten und App-Symbol.
- Editorbereiche Basis, Attacken, Werte und Infos.
- Kompakte IV-/EV-Tabelle mit EV-Gesamtsumme.
- Feste Aktionen zum Übernehmen und Exportieren.
- Klar gekennzeichnete Pokémon-Vorschau mit Beispieldaten.
- Dieselbe Paketkennung und Signatur wie 0.1.0; höherer Versionscode.
