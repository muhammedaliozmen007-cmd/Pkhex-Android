#!/usr/bin/env python3
"""Produce the APK, checked update manifest and complete GPL source archive."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZIP_DEFLATED, ZIP_STORED, ZipFile

parser = argparse.ArgumentParser()
parser.add_argument('--apk', required=True, type=Path)
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
project = ET.parse(root / 'App/App.csproj')
version = project.findtext('.//ApplicationDisplayVersion')
code = int(project.findtext('.//ApplicationVersion'))
package = project.findtext('.//ApplicationId')
out = args.output.resolve()
if not args.apk.is_file():
    parser.error('Signed APK not found')
out.mkdir(parents=True, exist_ok=True)
name = f'PKHeX-Android-{version}.apk'
apk = out / name
if args.apk.resolve() != apk:
    shutil.copyfile(args.apk, apk)
with apk.open('rb') as stream:
    hash_value = hashlib.file_digest(stream, 'sha256').hexdigest()
manifest = {
    'schema': 1, 'applicationId': package, 'versionCode': code,
    'versionName': version, 'apkFile': name,
    'sizeBytes': apk.stat().st_size, 'sha256': hash_value,
    'notes': 'Fehler in Auswahllisten für Wesen, Items, Fähigkeiten und Attacken behoben; Suche und Auswahl abgesichert.'
}
text = json.dumps(manifest, ensure_ascii=False, indent=2) + '\n'
(out / 'update.json').write_text(text, encoding='utf-8')
source = out / f'PKHeX-Android-{version}-Quellcode.zip'
with ZipFile(source, 'w', compression=ZIP_DEFLATED, compresslevel=6) as archive:
    for file in sorted(root.rglob('*')):
        relative = file.relative_to(root)
        if not file.is_file() or file.is_symlink() or file.is_relative_to(out):
            continue
        if any(part in {'.git', 'bin', 'obj', '__pycache__', 'dist'} for part in relative.parts):
            continue
        if relative.as_posix() == 'release/update.json':
            continue
        if file.suffix in {'.tmp', '.part'}:
            continue
        if file.suffix.lower() in {'.keystore', '.jks', '.p12', '.pfx', '.pem', '.key'}:
            continue
        archive.write(file, 'PKHeX.Android/' + relative.as_posix(),
                      compress_type=ZIP_STORED if file.suffix == '.webp' else ZIP_DEFLATED)
    archive.writestr('PKHeX.Android/release/update.json', text)
notes = f'PKHeX Android {version}\n\n{manifest["notes"]}\n\nInoffizieller GPL-3.0-or-later Community-Port. Vollständiger Quellcode als Release-Asset.\n'
(out / 'release-notes.md').write_text(notes, encoding='utf-8')
print(json.dumps({'apk': str(apk), 'source': str(source), 'sha256': hash_value,
                  'version': version, 'versionCode': code, 'bytes': apk.stat().st_size}))
