#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${ANDROID_SDK_ROOT:?ANDROID_SDK_ROOT muss auf das Android SDK zeigen}"
: "${JAVA_HOME:?JAVA_HOME muss auf JDK 21 zeigen}"
: "${ANDROID_SIGNING_KEYSTORE:?ANDROID_SIGNING_KEYSTORE muss auf einen privaten Signierschlüssel zeigen}"
: "${ANDROID_SIGNING_KEY_ALIAS:?ANDROID_SIGNING_KEY_ALIAS fehlt}"
: "${ANDROID_SIGNING_STORE_PASS:?ANDROID_SIGNING_STORE_PASS fehlt}"
: "${ANDROID_SIGNING_KEY_PASS:?ANDROID_SIGNING_KEY_PASS fehlt}"
test -f "$ANDROID_SIGNING_KEYSTORE"
dotnet build App/App.csproj -c Release -m:1 \
  -p:BuildInParallel=false -p:UseSharedCompilation=false \
  -p:SourceRevisionId=66ef5983a5d3 \
  -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" \
  -p:JavaSdkDirectory="$JAVA_HOME" \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore="$ANDROID_SIGNING_KEYSTORE" \
  -p:AndroidSigningKeyAlias="$ANDROID_SIGNING_KEY_ALIAS" \
  -p:AndroidSigningStorePass=env:ANDROID_SIGNING_STORE_PASS \
  -p:AndroidSigningKeyPass=env:ANDROID_SIGNING_KEY_PASS
