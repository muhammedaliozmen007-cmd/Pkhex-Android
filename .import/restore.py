import base64, hashlib, json, os, re, subprocess
from pathlib import Path
manifest = json.loads(Path('.import/payloads.json').read_text())
target = Path(os.environ['RUNNER_TEMP']) / 'pkhex-import'
target.mkdir(parents=True, exist_ok=True)
for payload in manifest['payloads']:
    assert payload['name'] in {'project.bundle', 'PKHeX-Android-0.4.1.apk'}
    digest = hashlib.sha256()
    length = 0
    with (target / payload['name']).open('wb') as stream:
        for chunk in payload['chunks']:
            assert re.fullmatch('[0-9a-f]{40}', chunk['sha'])
            response = subprocess.run(['gh', 'api', f"repos/{os.environ['GITHUB_REPOSITORY']}/git/blobs/{chunk['sha']}"], check=True, capture_output=True, text=True)
            blob = json.loads(response.stdout)
            assert blob['encoding'] == 'base64' and blob['sha'] == chunk['sha']
            data = base64.b64decode(''.join(blob['content'].split()), validate=True)
            assert len(data) == chunk['size']
            assert hashlib.sha256(data).hexdigest() == chunk['sha256']
            assert hashlib.sha1(f'blob {len(data)}\0'.encode() + data).hexdigest() == chunk['sha']
            stream.write(data); digest.update(data); length += len(data)
    assert length == payload['size'] and digest.hexdigest() == payload['sha256']
    print(f'Verified {payload["name"]}: {length} bytes')
for field in ['source_commit', 'source_tree']:
    assert re.fullmatch('[0-9a-f]{40}', manifest[field])
with open(os.environ['GITHUB_ENV'], 'a') as stream:
    stream.write(f'PKHEX_SOURCE_COMMIT={manifest["source_commit"]}\n')
    stream.write(f'PKHEX_SOURCE_TREE={manifest["source_tree"]}\n')
