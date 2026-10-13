"""Package the built plugin and check that metadata, DLL and checksum agree."""
from pathlib import Path
import hashlib
import json
import xml.etree.ElementTree as ET
import zipfile

root = Path(__file__).resolve().parents[1]
built = root / 'bin/Release'
dist = root / 'dist'
dist.mkdir(exist_ok=True)
release = dist / 'XLToolbar.zip'
with zipfile.ZipFile(built / 'XLToolbar/latest.zip') as incoming, zipfile.ZipFile(release, 'w', zipfile.ZIP_DEFLATED) as outgoing:
    for name in incoming.namelist():
        assert '/' not in name and '\\' not in name and name not in ('.', '..')
        outgoing.writestr(name, incoming.read(name))
    for name in ['README.md', 'NOTICE.md']:
        outgoing.write(root / name, name)
with zipfile.ZipFile(release) as archive:
    assert archive.testzip() is None
    manifest = json.loads(archive.read('XLToolbar.json'))
    assert manifest['AssemblyVersion'] == ET.parse(root / 'XLToolbar.csproj').getroot().findtext('.//Version') + '.0'
    assert manifest['InternalName'] == manifest['Name'] == 'XLToolbar'
    assert manifest['DalamudApiLevel'] == 15
    assert archive.read('XLToolbar.dll') == (built / 'XLToolbar.dll').read_bytes()
(dist / 'SHA256SUMS.txt').write_text(hashlib.sha256(release.read_bytes()).hexdigest() + '  XLToolbar.zip\n')
print('Verified XLToolbar.zip:', release.stat().st_size, 'bytes; DLL, manifest, and checksum')
