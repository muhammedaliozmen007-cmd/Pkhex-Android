# Updates für PKHeX Android

Im ☰-Menü steht **Update**, auch wenn noch keine Datei geöffnet ist.
Seit Version 0.4.2 ist das öffentliche GitHub-Repository dieser Android-App
standardmäßig hinterlegt. Ältere Installationen fragen einmalig nach der Adresse.
Für dieses Projekt die Adresse
`https://github.com/muhammedaliozmen007-cmd/Pkhex-Android` verwenden.
Der enthaltene GitHub-Actions-Workflow veröffentlicht nach erfolgreichem Build
die APK und ihre passenden Update-Dateien als stabilen Release.
Version 0.4.2 und vom Workflow gebaute APKs enthalten diesen Update-Kanal bereits.

Ein neues Release wird über GitHubs `releases/latest` abgefragt. Die App prüft den
Android-Versionscode, zeigt Version und Downloadgröße und lädt die APK nach
„Herunterladen“ mit Fortschrittsanzeige. APK-Größe, SHA-256, Paketname, Version und
der Signaturschlüssel müssen passen. Abgebrochene Downloads werden entfernt;
bereits fertig heruntergeladene Updates bleiben für eine spätere Installation
verfügbar. Netzwerk- und Download-Arbeiten laufen im Hintergrund.

Die Installation erfolgt mit dem Android-Installer. Android 8+ kann zuerst die
Freigabe „Unbekannte Apps installieren“ für PKHeX Android verlangen. Es werden
keine Spielstände hochgeladen. Die APK wird im privaten App-Verzeichnis gespeichert
und nur diese eine APK über eine temporäre Leseberechtigung an den Installer
weitergereicht. Eine allgemeine Speicherberechtigung ist nicht nötig.

## Die dauerhafte Update-Quelle einmalig veröffentlichen

Das Projekt-Repository ist bereits angelegt. Der folgende Ablauf beschreibt
auch die Einrichtung eines eigenen Forks.

1. Ein eigenes **öffentliches** GitHub-Repository für PKHeX Android erstellen.
   `kwsch/PKHeX` ist das ursprüngliche Desktop-Projekt, kein Release-Kanal für
   diese inoffizielle Android-App.
2. Den Inhalt des Quellcode-Ordners `PKHeX.Android` an die Repository-Wurzel
   übernehmen. `App/`, `upstream/`, `scripts/` und `.github/` müssen dort liegen.
3. GitHub Actions für das Repository aktivieren. Der enthaltene Workflow baut
   bei Änderungen auf `main` oder `master` eine neue APK, führt die Datei- und
   Update-Prüfungen aus und veröffentlicht APK, `update.json` und den vollständigen
   Quellcode gemeinsam. Ein Release wird erst nach dem Hochladen aller Dateien
   aus dem Entwurfsstatus veröffentlicht.
   Dafür die unten beschriebenen privaten Signier-Secrets einrichten.
4. In der installierten App unter **☰ → Update** die Repository-URL eingeben.
   Die im Workflow erzeugten APKs enthalten diese Adresse bereits als Standard.
   Die manuell konfigurierte Adresse bleibt bei App-Updates erhalten.

Der Workflow erhöht den Versionscode gegenüber der Basis in `App.csproj` um die
GitHub-Laufnummer; die sichtbare Patch-Version wird ebenfalls erhöht. Deshalb
liefert der erste CI-Lauf auf Basis 0.4.2 eine Version 0.4.3 mit Code 7. Zukünftige
Basisversionen und Versionscodes im Projekt müssen aufsteigend gepflegt werden.

## Bereits gebaute APK manuell veröffentlichen

`scripts/package_release.py` erzeugt aus der signierten APK genau die drei
Release-Assets. `release/update.json` im Quellcode-ZIP beschreibt die ausgelieferte
APK 0.4.2. APK, `update.json` und Quellcode-ZIP als regulären stabilen GitHub-Release
hochladen. Die Manifestdatei gehört zum jeweiligen APK-Build; ihr Hash muss immer
aus der tatsächlichen APK berechnet werden. Kein Windows-Release und keine
Vorabversion wird vom Updater als Android-Update installiert.

## Private Signierung für GitHub Actions

Unter **Settings → Secrets and variables → Actions → New repository secret**
vier Repository-Secrets hinterlegen:

| Secret | Inhalt |
| --- | --- |
| `ANDROID_SIGNING_KEYSTORE_BASE64` | Die Keystore-Datei als Base64-Text |
| `ANDROID_SIGNING_KEY_ALIAS` | Alias des Signierschlüssels |
| `ANDROID_SIGNING_STORE_PASS` | Keystore-Passwort |
| `ANDROID_SIGNING_KEY_PASS` | Schlüssel-Passwort |

Der Workflow stellt die Keystore-Datei ausschließlich im temporären Verzeichnis
des Runners bereit und entfernt sie nach dem Build. Die Passwörter werden über
Umgebungsvariablen eingelesen. Signierschlüssel werden weder im Git-Repository
noch im veröffentlichten Quellcode-ZIP gespeichert. Fehlen Secrets, meldet der
Workflow dies vor dem Herunterladen der Build-Abhängigkeiten.

Damit eine APK über die vorhandene App installiert werden kann, denselben
Signierschlüssel wie bei der bisher installierten APK verwenden. Ein neuer
Schlüssel benötigt einen eigenen Migrationsplan.

## Prüfungen

```bash
dotnet run --project UpdateTests/UpdateTests.csproj -c Release
```

Die Tests simulieren neue/alte Versionen, fehlerhafte Release-Metadaten,
unpassende Paketangaben, manipulierte Download-Adressen, Prüfsummenfehler,
abgebrochene und unvollständige Downloads sowie den Erhalt einer schon
vorhandenen vollständigen APK. Sie benötigen keinen veröffentlichten Release.
