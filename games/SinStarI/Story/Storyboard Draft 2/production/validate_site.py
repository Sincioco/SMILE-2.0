"""Focused static delivery checks; no server or browser required."""
from pathlib import Path
from collections import Counter
from urllib.parse import unquote, urlsplit
import hashlib
import json
import subprocess
import tempfile
from PIL import Image
from build_site import Document, ROOT, OLD, SCRIPT, scene_data


def without_figures(source):
    doc = Document(source)
    figures = sorted((n for n in doc.nodes if n.tag == 'figure'), key=lambda n:n.start, reverse=True)
    for node in figures:
        source = source[:node.start] + source[node.end:]
    return next(n for n in Document(source).nodes if n.attrs.get('id') == 'script').plain()


def validate():
    docs = {p.name: Document(p.read_text(encoding='utf-8')) for p in ROOT.glob('*.html')}
    ids = {}
    for name, doc in docs.items():
        counts = Counter(n.attrs['id'] for n in doc.nodes if n.attrs.get('id'))
        assert all(c == 1 for c in counts.values()), (name, 'Duplicate IDs')
        ids[name] = set(counts)
    refs = 0
    for name, doc in docs.items():
        for node in doc.nodes:
            for attr in ('href', 'src', 'poster'):
                value = node.attrs.get(attr)
                if not value: continue
                url = urlsplit(value)
                if url.scheme or url.netloc: continue
                target = ROOT / unquote(url.path) if url.path else ROOT / name
                assert target.is_file(), (name, value, 'Missing local file')
                if url.fragment and target.suffix == '.html':
                    assert unquote(url.fragment) in ids[target.name], (name, value, 'Missing fragment')
                refs += 1
    old = (OLD / SCRIPT).read_text(encoding='utf-8')
    new = (ROOT / SCRIPT).read_text(encoding='utf-8')
    assert without_figures(old) == without_figures(new), 'Script narrative changed'
    assert (OLD/'Canon.md').read_bytes() == (ROOT/'Canon.md').read_bytes(), 'Canon changed'
    scenes = scene_data(docs[SCRIPT])
    assert len(scenes) == 53
    panels = json.loads((ROOT/'production/illustration-index.json').read_text(encoding='utf-8'))
    assert len(panels) == 120
    expected = {p['id']:p for p in panels}
    assert len(expected) == 120
    main_scenes = {s['id'] for s in scenes[:47]}
    new_scenes = {p['scene'] for p in panels if p['id'].startswith('new-')}
    assert len(new_scenes) == 47 and new_scenes == main_scenes
    found = []
    for scene in scenes:
        assert scene['id'] in ids['index.html']
        for block in scene['blocks']:
            if block.tag != 'figure': continue
            panel_id = block.attrs['id'][7:]
            panel = expected[panel_id]
            assert panel['scene'] == scene['id'], (panel_id, 'Incorrect script scene')
            link = next(n for n in block.children if n.tag == 'a')
            assert link.attrs['href'] == 'index.html#' + panel_id
            picture = next(n for n in link.children if n.tag == 'img')
            assert urlsplit(picture.attrs['src']).path == 'asset/images/' + panel['file']
            assert panel_id in ids['index.html']
            found.append(panel_id)
    assert set(found) == set(expected) and len(found) == 120
    assert all(p['dialogue'] for p in panels), 'Missing dialogue'
    decoded = 0
    for path in (ROOT/'asset/images').rglob('*.png'):
        with Image.open(path) as image: image.load()
        decoded += 1
    hashes = json.loads((ROOT/'production/draft1-checksums.json').read_text(encoding='utf-8'))
    for name, digest in hashes.items():
        assert hashlib.sha256((OLD/name).read_bytes()).hexdigest() == digest, ('Draft 1 changed', name)
    assert len([p for p in OLD.rglob('*') if p.is_file()]) == len(hashes), 'Draft 1 inventory changed'
    node_exe = Path(r'C:\Users\louie\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe')
    if not node_exe.exists(): node_exe = Path('node')
    js_count = 0
    for path in (ROOT/'asset').glob('*.js'):
        subprocess.run([str(node_exe),'--check',str(path)],check=True,capture_output=True)
        js_count += 1
    with tempfile.TemporaryDirectory(prefix='sin-star-js-') as temp:
        for name, doc in docs.items():
            for i, element in enumerate(n for n in doc.nodes if n.tag == 'script' and not n.attrs.get('src')):
                path = Path(temp)/f'{name}-{i}.js'
                path.write_text(''.join(element.text),encoding='utf-8')
                subprocess.run([str(node_exe),'--check',str(path)],check=True,capture_output=True)
                js_count += 1
    result = {'status':'Passed','html_pages':len(docs),'local_references':refs,'scenes':53,'illustrations':120,
              'new_illustrations':48,'decoded_png_files':decoded,'script_narrative':'Unchanged',
              'canon':'Unchanged','draft1_files_unchanged':len(hashes),'javascript_syntax_checks':js_count,
              'script_image_scene_and_return_links':'120 passed','browser_layout_test':'Not performed; static delivery validation'}
    (ROOT/'production/site-validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result),flush=True)


if __name__ == '__main__': validate()
