# PKHeX Android 0.4.2 — unofficial community port

A native Android interface built around the unmodified **PKHeX.Core** from
[kwsch/PKHeX](https://github.com/kwsch/PKHeX), revision
`66ef5983a5d349efa2dd615f2926a1b1ee97306e`.

This community port provides basic editors, a phone-friendly interface and offline
Pokémon artwork. It is not an official release by Kaphotics and does not yet
include every feature of the Windows application.

## Installation and use

- Requires **Android 8.0 or newer**. One APK supports ARM64, ARM32 and x86-64.
- Download the APK from Releases, open it and allow the download/file app to

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
