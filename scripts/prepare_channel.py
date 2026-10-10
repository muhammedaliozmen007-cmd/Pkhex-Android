#!/usr/bin/env python3
"""Configure a real public release repository and monotonically number CI builds."""
import argparse
import json
import re
from pathlib import Path
from xml.etree import ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument('--repository', required=True)
parser.add_argument('--run-number', type=int)
args = parser.parse_args()
if not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]{1,100}', args.repository):
    parser.error('Expected owner/repository')
root = Path(__file__).resolve().parent.parent
project = root / 'App/App.csproj'
tree = ET.parse(project)
if args.run_number is not None:
    if args.run_number < 1:
        parser.error('run-number must be positive')
    code = tree.find('.//ApplicationVersion')
    version = tree.find('.//ApplicationDisplayVersion')
    major, minor, patch = map(int, version.text.split('.'))
    code.text = str(int(code.text) + args.run_number)
    version.text = f'{major}.{minor}.{patch + args.run_number}'
    ET.indent(tree)
    tree.write(project, encoding='unicode')
(root / 'App/Assets/update-channel.json').write_text(json.dumps({'repository': args.repository}) + '\n')
print('Update repository: ' + args.repository)
