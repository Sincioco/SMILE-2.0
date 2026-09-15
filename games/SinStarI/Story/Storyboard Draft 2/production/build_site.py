"""Build Draft 2 from the preserved Draft 1 and the reviewed illustration manifest.

Production-only tool: uses the already installed Pillow. The delivered site has
no runtime dependencies and works directly from index.html.
"""
from pathlib import Path
from html import escape
from html.parser import HTMLParser
import hashlib
import json
import re
import shutil
from PIL import Image
from movie_page import build_movies

ROOT = Path(__file__).resolve().parents[1]
OLD = ROOT.parent / 'Storyboard Draft 1 - Illustrated Script'
SCRIPT = 'Sin-Star-I-Game-Script-v0.2.html'


class Element:
    def __init__(self, tag, attrs, start, parent):
        self.tag, self.attrs, self.start = tag, dict(attrs), start
        self.parent, self.end, self.children, self.text = parent, None, [], []

    def plain(self):
        return ' '.join(''.join(self.text).split())


class Document(HTMLParser):
    def __init__(self, source):
        super().__init__(convert_charrefs=True)
        self.source, self.nodes, self.stack = source, [], []
        self.lines = [0] + [m.end() for m in re.finditer('\n', source)]
        self.feed(source)

    def position(self):
        row, col = self.getpos()
        return self.lines[row - 1] + col

    def handle_starttag(self, tag, attrs):
        n = Element(tag, attrs, self.position(), self.stack[-1] if self.stack else None)
        if n.parent:
            n.parent.children.append(n)
        self.nodes.append(n)
        if tag in ('area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr'):
            n.end = n.start + len(self.get_starttag_text())
        else:
            self.stack.append(n)

    def handle_endtag(self, tag):
        for i in range(len(self.stack) - 1, -1, -1):
            if self.stack[i].tag == tag:
                for n in self.stack[i:]:
                    n.end = self.position() + len(tag) + 3
                del self.stack[i:]
                break

    def handle_data(self, data):
        for n in self.stack:
            n.text.append(data)


def version(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()[:10]


def split_sheets():
    archive = ROOT / 'asset/images/archive/draft-1-sheets'
    archive.mkdir(parents=True, exist_ok=True)
    records = []
    # Measured white dividers; the source sheets do not all have equal quadrants.
    dividers = [(767,768,509,511),(768,770,508,510),(767,770,508,510),
                (767,767,509,511),(767,768,510,511),(767,769,508,510),
                (767,768,511,512),(767,768,509,510),(768,769,491,492),
                (767,769,509,512),(764,765,509,510),(767,769,508,509),
                (767,769,508,510),(767,770,510,512),(766,768,508,510),
                (767,769,509,511),(766,770,508,510),(768,769,509,510)]
    for page in range(1, 19):
        name = f'page-{page:02}.png'
        source = OLD / 'asset/images' / name
        shutil.copy2(source, archive / name)
        with Image.open(source) as sheet:
            width, height = sheet.size
            x0, x1, y0, y1 = dividers[page - 1]
            boxes = ((0, 0, x0 - 1, y0 - 1), (x1 + 2, 0, width, y0 - 1),
                     (0, y1 + 2, x0 - 1, height), (x1 + 2, y1 + 2, width, height))
            if page == 13:
                boxes = ((0,0,765,507),(769,0,width,507),
                         (0,512,762,height),(768,512,width,height))
            for q, box in enumerate(boxes, 1):
                filename = f'panel-{page:02}-{q}.png'
                output = ROOT / 'asset/images' / filename
                sheet.crop(box).save(output)
                records.append({'id': f'panel-{page:02}-{q}', 'source': name, 'box': box, 'file': filename, 'sha256': version(output)})
        redundant = ROOT / 'asset/images' / name
        if redundant.exists():
            assert version(redundant) == version(archive / name)
            redundant.unlink()
    (ROOT / 'production/crops.json').write_text(json.dumps(records, indent=2), encoding='utf-8')


def image_tag(filename, alt, eager=False):
    path = ROOT / 'asset/images' / filename
    with Image.open(path) as im:
        width, height = im.size
    return f'<img src="asset/images/{filename}?v={version(path)}" alt="{escape(alt, quote=True)}" width="{width}" height="{height}" loading="{"eager" if eager else "lazy"}" decoding="async">'


def script_figure(panel):
    title = panel.get('caption', panel['shot'])
    return f'''<figure class="script-art" id="script-{panel['id']}">
<a class="script-art-link" href="index.html#{panel['id']}" aria-label="Return to Visual Storyboard: {escape(title, quote=True)}">
{image_tag(panel['file'], panel['art'])}
<span class="script-art-caption">{escape(title)}<span aria-hidden="true">↩ Storyboard</span></span></a></figure>'''


def scene_data(document):
    scenes = []
    for n in document.nodes:
        if n.tag == 'h3' and 'scene-heading' in n.attrs.get('class', ''):
            siblings = n.parent.children
            blocks = []
            for b in siblings[siblings.index(n) + 1:]:
                if b.tag in ('h2', 'h3'):
                    break
                blocks.append(b)
            heading = next(b for b in siblings if b.tag == 'h2')
            scenes.append({'id': n.attrs['id'], 'title': n.plain(), 'chapter': heading.attrs['id'], 'chapter_title': heading.plain(), 'blocks': blocks})
    return scenes


def build():
    split_sheets()
    base = (OLD / SCRIPT).read_text(encoding='utf-8')
    doc = Document(base)
    scenes = scene_data(doc)
    pages = json.loads((OLD / 'Storyboard-Draft-1/storyboard.json').read_text(encoding='utf-8'))['pages']
    panels = []
    for p, page in enumerate(pages, 1):
        for q, panel in enumerate(page['panels'], 1):
            panels.append(dict(panel, id=f'panel-{p:02}-{q}', file=f'panel-{p:02}-{q}.png', label=f'{p:02}.{q}', page=p, scene=panel['scene'].lower()))
    overrides = ROOT / 'production/image-overrides.json'
    if overrides.exists():
        for revision in json.loads(overrides.read_text(encoding='utf-8')):
            next(p for p in panels if p['id'] == revision['id'])['file'] = revision['file']
    manifest = ROOT / 'production/new-illustrations.json'
    additions = json.loads(manifest.read_text(encoding='utf-8')) if manifest.exists() else []
    panels.extend(p for p in additions if (ROOT / 'asset/images' / p['file']).exists())
    by_id = {p['id']: p for p in panels}
    changes = []
    for n in doc.nodes:
        if n.tag == 'figure' and n.attrs.get('id', '').startswith('script-panel-'):
            changes.append((n.start, n.end, script_figure(by_id[n.attrs['id'][7:]])))
    for panel in panels[72:]:
        scene = next(s for s in scenes if s['id'] == panel['scene'])
        match = next((b for b in scene['blocks'] if panel['after'] in b.plain()), None)
        assert match is not None, (panel['id'], panel['after'])
        changes.append((match.end, match.end, '\n' + script_figure(panel)))
    for start, end, value in sorted(changes, reverse=True):
        base = base[:start] + value + base[end:]
    base = base.replace('/* Full-script illustrations reuse the unchanged storyboard art sheets. */', '/* Draft 2 uses individual illustrations; each returns to its storyboard panel. */')
    base = base.replace('.script-art-crop img {display:block; position:absolute; width:200%; height:200%; max-width:none}', '.script-art-link > img {display:block; width:100%; height:auto}')
    base = base.replace('GAME SCRIPT · v0.2</small>', 'GAME SCRIPT · v0.2 · STORYBOARD DRAFT 2</small>')
    base = base.replace('<a class="control storyboard-return" href="index.html">← Visual Storyboard</a>', '<nav class="edition-links" aria-label="Story editions"><a class="control storyboard-return" href="index.html">← Visual Storyboard</a> <a class="control" href="movies.html">Films &amp; Trailer</a></nav>')
    base = base.replace('</style>', '.edition-links {display:flex; flex-wrap:wrap; gap:8px}\n</style>', 1)
    (ROOT / SCRIPT).write_text(base, encoding='utf-8')
    ordered = {s['id']: [] for s in scenes}
    # Recover script placement order so both reading views tell the same sequence.
    updated = Document(base)
    for scene in scene_data(updated):
        for b in scene['blocks']:
            if b.tag == 'figure' and b.attrs.get('id', '').startswith('script-'):
                ordered[scene['id']].append(by_id[b.attrs['id'][7:]])
    nav, content, chapter = [], [], None
    for scene in scenes:
        if scene['chapter'] != chapter:
            if chapter is not None:
                nav.append('</ul></details>')
                content.append('</section>')
            chapter = scene['chapter']
            nav.append(f'<details open><summary><a href="#{chapter}">{escape(scene["chapter_title"])}</a></summary><ul>')
            content.append(f'<section class="board" id="{chapter}"><header class="board-head"><div><p class="eyebrow">Sin Star I · Draft 2</p><h2>{escape(scene["chapter_title"])}</h2></div></header>')
        nav.append(f'<li><a href="#{scene["id"]}">{escape(scene["title"])}</a></li>')
        content.append(f'<section class="scene" id="{scene["id"]}"><h3><a href="{SCRIPT}#{scene["id"]}">{escape(scene["title"])}</a></h3><div class="panels">')
        for panel in ordered[scene['id']]:
            alias = f'<span id="page-{panel["page"]:02}"></span>' if panel.get('label', '').endswith('.1') else ''
            lines = ''.join(f'<p><strong>{escape(who)}</strong> {escape(line)}</p>' for who, line in panel['dialogue'])
            content.append(f'''{alias}<figure id="{panel['id']}"><a class="picture" href="asset/images/{panel['file']}" target="_blank" rel="noopener" aria-label="Open individual illustration: {escape(panel['caption'], quote=True)}">{image_tag(panel['file'], panel['art'], panel['id'] == 'panel-01-1')}</a><figcaption><p class="scene-label"><span>{escape(panel.get('label', 'New'))} · {escape(panel['shot'])}</span><a href="{SCRIPT}#{scene['id']}">{scene['id'].upper()}</a></p><p class="caption">{escape(panel['caption'])}</p><div class="dialogue">{lines}</div></figcaption></figure>''')
        content.append('</div></section>')
    nav.append('</ul></details>')
    content.append('</section>')
    old_index = (OLD / 'index.html').read_text(encoding='utf-8')
    appendix = re.search(r'<details class="appendix".*?</details>', old_index, re.S).group(0)
    appendix = appendix.replace('Each four-panel sheet is shown one panel at a time beside editable HTML dialogue; selecting an image opens the full sheet.', 'Each illustration is a separate image beside editable HTML dialogue. Selecting a storyboard image opens that individual image. Original sheets are preserved in asset/images/archive/draft-1-sheets.')
    index = f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="theme-color" content="#0c1422"><meta name="description" content="Sin Star I Storyboard Draft 2: illustrated scenes, dialogue and full script."><title>Sin Star I · Storyboard Draft 2</title><link rel="icon" href="asset/star.svg"><link rel="stylesheet" href="asset/storyboard.css?v={version(ROOT / 'asset/storyboard.css')}"></head>
<body id="top"><a class="skip" href="#story">Skip to the story</a><header class="bar"><div class="bar-inner"><a class="brand" href="#top"><span aria-hidden="true">✦</span> SIN STAR I</a><nav class="navigation" aria-label="Reading navigation"><a href="{SCRIPT}">Full Script</a><a href="movies.html">Films &amp; Trailer</a><a href="#canon">Canon &amp; Notes</a></nav></div></header>
<div class="story-layout"><aside class="story-contents"><details class="contents-shell" open><summary>Contents &amp; Scene Index</summary><nav aria-label="Chapters and scenes">{''.join(nav)}</nav></details></aside>
<main><div class="intro"><div><p class="eyebrow">Visual Storyboard · Draft 2</p><h1>A place among the stars.</h1></div><p>{len(panels)} individual illustrations · 53 scenes<br>Read the dialogue beneath each image. Select any scene title to enter the Full Script.</p></div><div id="story">{''.join(content)}</div>{appendix}</main></div>
<footer><p>Sin Star I · Storyboard Draft 2<br>Draft 1 is preserved separately.</p><p><a href="#top">Return to the Beginning ↑</a></p></footer><script src="asset/reading.js?v={version(ROOT / 'asset/reading.js')}"></script></body></html>'''
    (ROOT / 'index.html').write_text(index, encoding='utf-8')
    build_movies(ROOT, version)
    (ROOT / 'production/illustration-index.json').write_text(json.dumps(panels, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({'scenes': len(scenes), 'individual_images': len(panels), 'new_images': len(panels) - 72}))


if __name__ == '__main__':
    build()
