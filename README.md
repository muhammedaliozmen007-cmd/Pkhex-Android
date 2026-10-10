# PKHeX Android 0.4.2 — unofficial community port

A native Android interface built around the unmodified **PKHeX.Core** from
[kwsch/PKHeX](https://github.com/kwsch/PKHeX), revision
`66ef5983a5d349efa2dd615f2926a1b1ee97306e`.

This community port provides basic editors, a phone-friendly interface and offline
Pokémon artwork. It is not an official release by Kaphotics and does not yet
include every feature of the Windows application.

## Repository and APK downloads

The complete project is maintained on `main` in
[muhammedaliozmen007-cmd/Pkhex-Android](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android).
It includes `App/`, `Editor/`, `Localization/`, `Updates/`, `Tests/`, `UpdateTests/`,
`upstream/`, Pokémon artwork and build scripts.

APKs, update manifests and complete source archives are published under
[Releases](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android/releases).
The [Android build workflow](https://github.com/muhammedaliozmen007-cmd/Pkhex-Android/actions/workflows/android-release.yml)
requires the four private signing secrets described in [UPDATES.md](UPDATES.md).
Signing keys must remain outside this public repository.

Use **☰ → Update** to check for a newer APK. Version 0.4.2 includes this repository
as its default update source. Older installations may need the repository URL
entered once. The update source can also be changed in the update dialog.

## Installation and use

- Requires **Android 8.0 or newer**. One APK supports ARM64, ARM32 and x86-64.
- Download the APK from Releases, open it and allow the download/file app to
  install unknown apps if Android asks.
- Tap **Open** and select a decrypted save file or an individual Pokémon file.
  Android's document picker handles file access; broad storage permissions are
  not required.
- In a save file, select a box or the party, then select a slot.
- Empty slots offer **Create Pokémon** and optional **Import Pokémon**. Choose an
  available species, edit its fields and tap **Apply**. An empty party also shows
  its next available slot, up to six Pokémon.
- Tap **Apply** to commit edits, then **Export** and choose a new output file.
- **Back up original** exports an exact copy of the imported file. **Restore
  original** discards changes. The three most recent committed edits can be undone
  during the current session.
- Import the exported file back into your emulator or console using its usual
  tools. The app cannot extract encrypted console backups or access a running
  game or emulator directly.

## Language selection

Open **☰ → Sprache / Language** and choose **Deutsch** or **English**.
The interface updates immediately and remembers your choice after restarting.
On first launch, German device languages select German; other languages use English.

The selected language applies to menus, dialogs, editor labels, Pokémon species,
items, abilities, moves, natures, types, forms and legality reports. Switching
languages preserves the open file, selected slot, editor tab and uncommitted
inputs, including new Pokémon drafts. It does not change the language stored in
an existing Pokémon, its nickname or its trainer name. System file pickers and
the Android installer follow the device's system language; release notes use the
language provided by the release publisher.

## Changes in 0.4.2

- Persistent German/English language selector available from the menu, including
  before opening a file.
- Localized interface, editor/update validation messages and PKHeX.Core names and
  legality reports.
- Preserve uncommitted input when changing language without applying or discarding it.
- English README and a preconfigured update source.
- Version code increased to 6, with the same package ID and signing key for upgrades.

## Features introduced in earlier versions

### 0.4.1: selection list fix

Fixed `Java.Lang.UnsupportedOperationException` in nature, item, ability and all
four move selection lists. The shared list adapter now owns mutable data.
Repeated searches and empty results are supported; delayed taps outside the
current result list are ignored.

### 0.4.0: create Pokémon in the app

Create Pokémon directly in empty box or party slots without importing a Pokémon
file. The species picker shows base forms available in the current game. New
Pokémon begin as editable level-5 drafts with trainer data from the save and
appropriate level-up moves.

Only **Apply** writes a draft into the save. When leaving the editor, you can
apply it, discard it or keep editing. Drafts and entered values are restored after
restarting the app. Creation supports undo; party members are added without gaps.
Individual Pokémon import and export remain available.

### 0.3.0: updates and performance

The **Update** action checks public GitHub releases, downloads the newest APK
with progress and cancellation, verifies it and opens Android's installer.
See [UPDATES.md](UPDATES.md) for publishing instructions. The former About action
was removed from the menu.

Artwork loads off the main thread, is scaled for its target view and uses a bounded
LRU cache. Species rows are reused, rapid searches are debounced and obsolete
image requests are cancelled. Unchanged previews do not reload their images.
Navigation retains the header and scroll container, and does not repeatedly write
or checksum an unchanged save file.

### 0.2.0: artwork and editor layout

Offline HOME artwork covers all 1,025 species, with available shiny and female
variants. Box thumbnails and large editor previews update when the species or
shiny selection changes. Four editor tabs organize basic fields, moves, stats and
information. A compact IV/EV table shows the EV total; Apply and Export remain
accessible in the fixed footer.

A clearly marked preview contains 20 example Pokémon. These synthetic examples
are not a complete playable save file.

## Included functionality

- Import and format detection using the original PKHeX.Core library.
- Box and party browsing, Pokémon creation and optional slot import.
- Open and export individual Pokémon files.
- Edit species, nickname, level, held item, nature, ability, shiny status, four
  moves, IVs/DVs and EVs.
- Edit trainer name and money where the game supports these properties.
- German and English Pokémon names and legality reports from PKHeX.Core.
- Validate value ranges, EV totals, protected slots and game availability.
- Write the appropriate formats and checksums using Core's original functions.
- Restore the session and uncommitted editor fields locally.
- No account required. Save files stay on the device. Only an explicitly started
  update check or APK download accesses the internet.

## Limitations

- This is not a complete desktop clone. Specialized event, inventory, Pokédex,
  ribbon, encounter and game-specific editors are not included yet.
- There are 468 artwork form mappings. Other forms use base artwork and display
  a notice in the editor. Not every special variant has its own image. Artwork
  sources are documented in [third-party/POKEMON-ARTWORK.md](third-party/POKEMON-ARTWORK.md).
- Existing forms are retained; changing the species resets the form to 0.
- There is no automatic conversion between Pokémon generations. Importing into
  a save requires the matching Pokémon format and an available species/form.
- Editing or creating a Pokémon does not automatically make it legal. Check its
  legality report.
- In Generations 1 and 2, HP DVs and Special stats are linked. In Generation 2,
  shiny status depends on DVs and may change their values.
- In Generations 1 and 2, box Pokémon are stored as contiguous lists; PKHeX.Core
  removes empty gaps when exporting.
- Imports are limited to 32 MiB. The original file is kept in private app storage
  for the most recently opened session. Use **Back up original** for a permanent
  backup. Uninstalling the app or clearing its data removes the local session.
- The APK is a personal test build signed with a development key. For your own
  public/store release, generate a private signing key and change the signing
  configuration. A build signed with a different key cannot update this test
  build in place and may require uninstalling it first.

## Source and license

`upstream/PKHeX.Core/` contains the complete Core used by the app, including its
embedded resources. `upstream/Directory.Build.props`, `upstream/COMMIT.txt` and
`upstream/LICENSE` record its version and origin.

PKHeX.Core is by Kaphotics and the PKHeX contributors. The Android interface and
editing logic are also distributed under **GPL-3.0-or-later**; see [LICENSE](LICENSE).
You may modify and redistribute the source and APK under that license. When
redistributing a build, provide the corresponding source code and license as well.

## Build locally

Requirements: .NET SDK 10.0, the .NET Android workload, JDK 21, and Android SDK
platform 36 with Build-Tools 36.0.0.

```bash
dotnet workload install android
dotnet run --project Tests/Tests.csproj -c Release
dotnet run --project UpdateTests/UpdateTests.csproj -c Release
export ANDROID_SDK_ROOT=/path/to/android-sdk
export JAVA_HOME=/path/to/jdk-21
export ANDROID_SIGNING_KEYSTORE=/private/path/to/release.keystore
export ANDROID_SIGNING_KEY_ALIAS=pkhex
# Set ANDROID_SIGNING_STORE_PASS and ANDROID_SIGNING_KEY_PASS privately.
bash scripts/build.sh
```

The signed APK is written to
`App/bin/Release/net10.0-android/org.pkhex.android.community-Signed.apk`.

Tests generate synthetic Pokémon and save files to check format handling. They
are not complete playable saves. See [VALIDATION.md](VALIDATION.md) for the checks
performed on the distributed build.
