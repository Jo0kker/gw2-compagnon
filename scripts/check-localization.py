"""Validate resource parity and static WPF labels without third-party dependencies."""
import re
from pathlib import Path
from xml.etree import ElementTree as ET

root = Path(__file__).resolve().parent.parent
folder = root / 'src/Companion.Core/Localization'
def read(name):
    items = ET.parse(folder / name).findall('data')
    result = {item.attrib['name']: item.findtext('value') for item in items}
    assert len(result) == len(items), f'Duplicate keys in {name}'
    assert all(result.values()), f'Empty translations in {name}'
    return result
english, french = read('Strings.resx'), read('Strings.fr.resx')
assert english.keys() == french.keys(), 'Translation keys differ'
for key in english:
    assert sorted(re.findall(r'\{\d+[^}]*\}', english[key])) == sorted(re.findall(r'\{\d+[^}]*\}', french[key])), key
for folder in ('src', 'tools'):
    for path in (root / folder).rglob('*'):
        if 'obj' in path.parts or 'bin' in path.parts:
            continue
        if path.suffix in ('.cs', '.xaml'):
            for key in re.findall(r'(?:\bT\("|\{local:Tr )([A-Za-z0-9]+)', path.read_text()):
                assert key in english, f'{path}: missing {key}'
        if path.suffix == '.xaml':
            for element in ET.parse(path).iter():
                for attribute, value in element.attrib.items():
                    if attribute in ('Text', 'Content', 'Header', 'Title', 'ToolTip', 'AutomationProperties.Name'):
                        assert not value or value.startswith('{'), f'{path}: untranslated {attribute}={value}'
print(f'PASS {len(english)} English/French resource pairs and static UI labels')
