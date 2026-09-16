"""Validate links, matching view structure, media placement and preserved text."""
from pathlib import Path
from collections import Counter
from urllib.parse import unquote, urlsplit
import hashlib
import json
import re
import subprocess
import sys
from PIL import Image
from html_document import Document, descendants
from media_catalog import all_clips, clips_for

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'
SCRIPT = 'Sin-Star-I-Game-Script-v0.2.html'


def narrative(source):
    source = re.sub(r'<figure\b.*?</figure>', '', source, flags=re.S)
    source = re.sub(r'<p class="animation-guide">.*?</p>', '', source, flags=re.S)
    return Document(source).by_id('script').plain()


def validate():
    allow_pending = '--allow-pending' in sys.argv
    sources = {path.name: path.read_text(encoding='utf-8') for path in ROOT.glob('*.html')}
    docs = {name:Document(source) for name, source in sources.items()}
    ids, missing = {}, []
    for name, doc in docs.items():
        counts = Counter(node.attrs['id'] for node in doc.nodes if node.attrs.get('id'))
        assert all(count == 1 for count in counts.values()), (name, 'duplicate IDs')
        ids[name] = set(counts)
    references = 0
    for name, doc in docs.items():
        for node in doc.nodes:
            for attribute in ('href', 'src', 'poster', 'data-src'):
                value = node.attrs.get(attribute)
                if not value:
                    continue
                url = urlsplit(value)
                if url.scheme or url.netloc:
                    continue
                target = ROOT / unquote(url.path) if url.path else ROOT / name
                if not target.is_file():
                    assert allow_pending and attribute == 'data-src', (name, value, 'missing file')
                    missing.append(url.path)
                if url.fragment and target.suffix == '.html':
                    assert unquote(url.fragment) in ids[target.name], (name, value, 'missing anchor')
                references += 1
    expected = json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8'))
    assert len(expected) == 129 and len({item['id'] for item in expected}) == 129
    clips = all_clips(expected)
    assert len(clips) == 130 and len({clip['id'] for clip in clips}) == 130
    render_status = json.loads((PRODUCTION / 'render-status.json').read_text(encoding='utf-8'))
    pending_jobs = [item['id'] for item in render_status if item['status'] not in ('rendered', 'reused')]
    assert allow_pending or not pending_jobs, ('unfinished render jobs', pending_jobs)
    for name, count in [('index.html', 121), (SCRIPT, 129)]:
        hosts = [node for node in docs[name].nodes if 'data-media-id' in node.attrs]
        assert len(hosts) == count, (name, len(hosts))
        for host in hosts:
            item = next(item for item in expected if item['id'] == host.attrs['data-media-id'])
            nodes = list(descendants(host))
            video = next(node for node in nodes if node.tag == 'video')
            image = next(node for node in nodes if node.tag == 'img')
            assert urlsplit(video.attrs['data-src']).path == item['video']
            assert urlsplit(image.attrs['src']).path == item['image']
            assert video.attrs.get('preload') == 'none' and 'src' not in video.attrs
            assert all(key in video.attrs for key in ('muted', 'loop', 'playsinline'))
            assert any(node.tag == 'button' for node in nodes)
            variants = [node for node in nodes if 'data-clip' in node.attrs]
            available = clips_for(item)
            if len(available) > 1:
                assert [node.attrs['data-clip'] for node in variants] == [clip['id'] for clip in available]
                assert [urlsplit(node.attrs['data-src']).path for node in variants] == [clip['video'] for clip in available]
                assert any(node.attrs.get('class') == 'remember-clip' for node in nodes)
                assert host.attrs['data-default-clip'] in [clip['id'] for clip in available]
            else:
                assert not variants
            if name == SCRIPT and item['kind'] == 'scene':
                assert next(node for node in nodes if node.tag == 'a').attrs['href'] == 'index.html#' + item['id']
    board_nav = re.search(r'<aside class="site-sidebar">.*?</aside>', sources['index.html'], re.S).group(0)
    script_nav = re.search(r'<aside class="site-sidebar">.*?</aside>', sources[SCRIPT], re.S).group(0)
    assert board_nav == script_nav
    assert board_nav.count('data-scene-link') == 53
    headers = [re.search(r'<header class="site-header">.*?</header>', sources[n], re.S).group(0) for n in ('index.html', SCRIPT)]
    assert headers[0].replace(' aria-current="page"', '') == headers[1].replace(' aria-current="page"', '')
    assert all(docs[name].by_id('site-audio-toggle').tag == 'button' for name in docs)
    assert docs['index.html'].by_id('new-c09-s03-planet').parent.parent.attrs['id'] == 'c09-s03'
    planet_at = sources[SCRIPT].index('id="script-new-c09-s03-planet"')
    assert sources[SCRIPT].index('They succeed in delaying it.') < planet_at < sources[SCRIPT].index("Kael remains at the platform's edge")
    original = (PRODUCTION / 'source-pages' / SCRIPT).read_text(encoding='utf-8')
    assert narrative(original) == narrative(sources[SCRIPT]), 'script prose or dialogue changed'
    assert (ROOT / 'Canon.md').read_bytes() == (ROOT.parent / 'Storyboard Draft 2/Canon.md').read_bytes()
    intro = docs[SCRIPT].by_id('sin-star-i').parent
    assert intro.children[2].tag == 'figure', 'poster is not directly below title/version'
    for name in ('arin', 'mira', 'orin', 'zara', 'milo', 'kael', 'aevos'):
        heading = docs[SCRIPT].by_id(name)
        siblings = heading.parent.children
        assert siblings[siblings.index(heading) + 1].tag == 'figure', name
    policy = next(node.attrs['content'] for node in docs[SCRIPT].nodes if node.attrs.get('http-equiv') == 'Content-Security-Policy')
    for directive in ('style-src', 'script-src', 'media-src'):
        assert re.search(directive + r"[^;]*'self'", policy), directive
    preserved = json.loads((PRODUCTION / 'preserved-drafts-sha256.json').read_text(encoding='utf-8'))
    for name, digest in preserved.items():
        assert hashlib.sha256((ROOT.parent / name).read_bytes()).hexdigest() == digest, name
    decoded = 0
    for path in (ROOT / 'asset/images').rglob('*.png'):
        with Image.open(path) as image:
            image.load()
        decoded += 1
    node_exe = r'C:\Users\louie\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
    scripts = list((ROOT / 'asset').glob('*.js'))
    for path in scripts:
        subprocess.run([node_exe, '--check', str(path)], check=True, capture_output=True)
    result = dict(status='Pending media' if missing or pending_jobs else 'Passed', html_pages=len(docs),
                  local_references=references, scene_previews=121, cast_portraits=7, poster=True,
                  video_clips=len(clips), illustrations_with_variations=sum('variants' in item for item in expected),
                  matching_navigation=True, script_narrative='Unchanged', canon='Unchanged',
                  preserved_draft_files=len(preserved), decoded_png_files=decoded,
                  javascript_syntax_checks=len(scripts), pending_videos=sorted(set(missing)),
                  pending_render_jobs=pending_jobs,
                  browser_test='Not performed; static and isolated behavior checks only')
    (PRODUCTION / 'site-validation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({**result, 'pending_videos':len(set(missing))}), flush=True)


if __name__ == '__main__':
    validate()
