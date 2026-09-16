"""Add Draft 3 media to preserved Draft 2 pages without rewriting the script."""
from pathlib import Path
import hashlib
import html
import json
import re
from urllib.parse import urlsplit
from PIL import Image
from site_shell import apply_shell, navigation
from media_catalog import clips_for

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'
SCRIPT = 'Sin-Star-I-Game-Script-v0.2.html'
CAST_SCENES = dict(arin='c00-s02', mira='c01-s01', orin='c02-s02',
                   zara='c03-s01', milo='c03-s03', kael='c00-s03', aevos='c09-s01')


def version(path):
    file = ROOT / path
    return hashlib.sha256(file.read_bytes()).hexdigest()[:10] if file.exists() else 'draft3-pending'


def escape(value):
    return html.escape(value, quote=True)


def picture(link, item):
    caption = ''
    image_end = link.index('>', link.index('<img')) + 1
    tail = link[image_end:link.rindex('</a>')].strip()
    if tail:
        caption = link[:link.index('>') + 1] + tail + '</a>'
        link = link[:image_end] + '</a>'
    src = item['video']
    video = (f'<video data-src="{src}?v={version(src)}" muted loop playsinline '
             'preload="none" aria-hidden="true" tabindex="-1"></video>')
    link = link.replace('</a>', video + '</a>')
    clips = clips_for(item)
    choices = ''
    if len(clips) > 1:
        buttons = ''.join(f'<button type="button" data-clip="{clip["id"]}" '
                          f'data-src="{clip["video"]}?v={version(clip["video"])}" aria-pressed="false">Clip {i + 1}</button>'
                          for i, clip in enumerate(clips))
        choices = ('<div class="clip-choices" role="group" aria-label="Video clip variations" hidden>' + buttons
                   + '<label><input class="remember-clip" type="checkbox"> Remember</label>'
                   '<span class="clip-save-status" role="status"></span></div>')
    return (f'<div class="animated-picture" data-media-id="{item["id"]}" '
            f'data-default-clip="{item.get("default_clip", item["id"])}"><div class="picture-stage">{link}'
            f'<button class="animation-toggle" type="button" data-caption="{escape(item["caption"])}" aria-label="Play Preview: {escape(item["caption"])}" '
            'aria-pressed="false" hidden>Play Preview</button>'
            '<span class="animation-status" role="status" aria-live="polite"></span></div>' + choices + '</div>' + caption)


def portrait(item):
    width, height = Image.open(ROOT / item['image']).size
    is_poster = item['kind'] == 'poster'
    anchor = 'top' if is_poster else CAST_SCENES[item['id'].removeprefix('cast-')]
    label = 'Return to Visual Storyboard' if is_poster else 'See ' + item['name'] + ' in the Visual Storyboard'
    link = (f'<a href="index.html#{anchor}" aria-label="{escape(label)}">'
            f'<img src="{item["image"]}?v={version(item["image"])}" '
            f'alt="{escape(item["caption"])}" width="{width}" height="{height}" '
            f'loading="{"eager" if is_poster else "lazy"}" decoding="async"></a>')
    return (f'<figure class="profile-portrait {"poster-portrait" if is_poster else "cast-portrait"}">'
            + picture(link, item) + f'<figcaption>{escape(label)} — select the image.</figcaption></figure>')


def planet_figure(item, script):
    width, height = Image.open(ROOT / item['image']).size
    image = (f'<img src="{item["image"]}?v={version(item["image"])}" alt="{escape(item["art"])}" '
             f'width="{width}" height="{height}" loading="lazy" decoding="async">')
    dialogue = ''.join(f'<p><strong>{escape(who)}</strong> {escape(line)}</p>'
                       for who, line in item['dialogue'])
    if script:
        link = f'<a class="script-art-link" href="index.html#{item["id"]}" aria-label="Return to Visual Storyboard">{image}</a>'
        opening = f'<figure class="script-art" id="script-{item["id"]}">'
    else:
        link = f'<a class="picture" href="{item["image"]}" target="_blank" rel="noopener">{image}</a>'
        opening = f'<figure id="{item["id"]}">'
    return (opening + link + '<figcaption><p class="scene-label">'
            f'<span>Final Battle · A World Hurled</span><a href="{SCRIPT}#{item["scene"]}">C09-S03</a></p>'
            f'<p class="caption">{escape(item["caption"])}</p><div class="dialogue">{dialogue}</div>'
            '</figcaption></figure>')


def main():
    items = json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8'))
    by_image = {item['image']: item for item in items}
    sidebar = navigation((PRODUCTION / 'source-pages/index.html').read_text(encoding='utf-8'))
    for name in ['index.html', SCRIPT, 'movies.html']:
        page = (PRODUCTION / 'source-pages' / name).read_text(encoding='utf-8')
        page = page.replace('Storyboard Draft 2', 'Storyboard Draft 3')
        page = page.replace('Visual Storyboard · Draft 2', 'Visual Storyboard · Draft 3')
        page = page.replace('Sin Star I · Draft 2', 'Sin Star I · Draft 3')
        page = page.replace('Draft 1 is preserved separately.', 'Drafts 1 and 2 are preserved separately.')
        page = page.replace('represented in 72 panels', 'represented in 121 individual illustrations')
        page = page.replace('in 72 panels', 'in 121 individual illustrations')
        if name != 'movies.html':
            planet = next(item for item in items if item['id'] == 'new-c09-s03-planet')
            if name == SCRIPT:
                beat = '<p>They succeed in delaying it. Aevos responds with a stronger intervention, establishing both that their actions matter and that they are presently outmatched.</p>'
                assert page.count(beat) == 1
                page = page.replace(beat, beat + planet_figure(planet, True))
            else:
                beat = '<figure id="new-c09-s03">'
                assert page.count(beat) == 1
                page = page.replace(beat, planet_figure(planet, False) + beat)
            def animate(match):
                link = match.group(0)
                src = re.search(r'<img\b[^>]*\bsrc="([^"]+)"', link).group(1)
                item = by_image.get(urlsplit(html.unescape(src)).path)
                return picture(link, item) if item else link
            page = re.sub(r'<a\b[^>]*>\s*<img\b[^>]*>.*?</a>', animate, page, flags=re.S)
            guide = ('<p class="animation-guide">Hover over an illustration to bring it to life, or select '
                     '<strong>Play Preview</strong>. Turn <strong>Audio On</strong> in the header for sound. '
                     'Where available, choose a clip and check <strong>Remember</strong> to keep it in both views. '
                     'Select the image to follow its link.</p>')
            if name == SCRIPT:
                poster = next(item for item in items if item['kind'] == 'poster')
                header = '<h2 id="draft-game-script-version-0-1">Draft Game Script — Version 0.2: Canon Update</h2>'
                assert page.count(header) == 1
                page = page.replace(header, header + portrait(poster) + guide)
                for item in items:
                    if item['kind'] != 'cast':
                        continue
                    key = item['id'].removeprefix('cast-')
                    heading = f'<h3 id="{key}">{item["name"]}</h3>'
                    assert page.count(heading) == 1, key
                    page = page.replace(heading, heading + portrait(item))
            else:
                page = page.replace('<div id="story">', guide + '<div id="story">', 1)
            page = page.replace('</head>', f'<link rel="stylesheet" href="asset/animated-pictures.css?v={version("asset/animated-pictures.css")}"></head>')
        page = apply_shell(page, name, sidebar, version)
        if name != 'movies.html':
            page = page.replace('</body>', f'<script src="asset/animated-pictures.js?v={version("asset/animated-pictures.js")}"></script></body>')
        (ROOT / name).write_text(page, encoding='utf-8')
    print(json.dumps({'scene_previews':sum(i['kind']=='scene' for i in items),
                      'portraits':sum(i['kind']=='cast' for i in items), 'poster':True}))


if __name__ == '__main__':
    main()
