#!/usr/bin/env python3
"""Read-only, standard-library campaign checks. Does not replace Unity validation."""
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / 'unfunctional'
errors = []

def check(condition, message):
    print(('PASS' if condition else 'FAIL') + ': ' + message)
    if not condition:
        errors.append(message)

def read(path):
    return path.read_text(encoding='utf-8-sig')

settings = read(UNITY / 'ProjectSettings/EditorBuildSettings.asset')
entries = re.findall(r'- enabled: (\d)\s+path: ([^\n]+)\s+guid: (\w+)', settings)
enabled = [(path, guid) for flag, path, guid in entries if flag == '1']
check(bool(enabled) and enabled[0][0] == 'Assets/Scenes/GLOBAL.unity', 'GLOBAL is the build entry point')
global_scene = read(UNITY / 'Assets/Scenes/GLOBAL.unity')
match = re.search(r'  levelSceneNames:\n((?:  - [^\n]+\n)+)', global_scene)
campaign = re.findall(r'  - (\S+)', match[1]) if match else []
check(len(campaign) == len(set(campaign)) and bool(campaign), 'Campaign names are unique')
check(campaign == [Path(path).stem for path, _ in enabled[1:]], 'Serialized campaign and enabled build order agree')
check(campaign[-3:] == ['LEVEL_2PS', 'LEVEL_CRAFTING', 'LEVEL12'], 'Shooter leads into mandatory crafting and the achievements finale')
manager = read(UNITY / 'Assets/Scripts/Core/GameManager.cs')
initializer = manager.split('public string[] levelSceneNames', 1)[1].split('};', 1)[0]
check(re.findall(r'"(LEVEL[^"\n]+)"', initializer) == campaign, 'New GameManager defaults match GLOBAL')
registration = read(UNITY / 'Assets/Scripts/Editor/SceneSetupEditor.cs').split('string[] scenePaths', 1)[1].split('};', 1)[0]
check(re.findall(r'"(Assets/Scenes/[^"\n]+)"', registration) == [path for path, _ in enabled], 'Scene registration preserves the full campaign')
for path, guid in enabled:
    scene = UNITY / path
    meta = scene.with_suffix(scene.suffix + '.meta')
    check(scene.is_file() and meta.is_file(), 'Scene and metadata exist: ' + scene.stem)
    if meta.is_file():
        found = re.search(r'^guid: (\w+)', read(meta), re.M)
        check(found is not None and found[1] == guid, 'Build GUID matches: ' + scene.stem)

missing_meta = [str(p.relative_to(ROOT)) for p in (UNITY / 'Assets/Scripts').rglob('*.cs')
                if not p.with_suffix('.cs.meta').is_file()]
check(not missing_meta, 'First-party scripts have matching metadata' + (': ' + ', '.join(missing_meta) if missing_meta else ''))
pointers = []
for path in (UNITY / 'Assets').rglob('*'):
    if path.is_file() and path.suffix != '.meta':
        with path.open('rb') as stream:
            if stream.read(43).startswith(b'version https://git-lfs.github.com/spec/v1'):
                pointers.append(str(path.relative_to(ROOT)))
check(not pointers, 'No unresolved Git LFS pointers in Assets')
for pointer in pointers[:10]:
    print('  ' + pointer)

version = read(UNITY / 'ProjectSettings/ProjectVersion.txt').splitlines()[0].split(': ', 1)[1]
manifest = json.loads(read(UNITY / 'Packages/manifest.json'))
print(f'Unity {version}; URP {manifest["dependencies"]["com.unity.render-pipelines.universal"]}; {len(campaign)} campaign stages.')
print(f'{len(errors)} failure(s). Run the Unity validator and play-mode checks next.')
sys.exit(bool(errors))
