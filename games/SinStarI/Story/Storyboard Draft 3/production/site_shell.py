"""One reading shell shared by Script, Storyboard, and the existing film gallery."""
import re

SCRIPT = 'Sin-Star-I-Game-Script-v0.2.html'


def navigation(source):
    sidebar = re.search(r'<aside class="story-contents">(.*?)</aside>', source, re.S).group(1)
    groups = re.findall(r'<details open><summary><a href="[^"]+">(.*?)</a></summary><ul>(.*?)</ul></details>', sidebar, re.S)
    assert len(groups) == 12
    sections = []
    for label, links in groups:
        first = re.search(r'href="#([^"]+)"', links).group(1)
        links = links.replace('<a href=', '<a data-scene-link href=')
        sections.append(f'<details class="scene-nav-group" open><summary><a href="#{first}">{label}</a></summary><ul>{links}</ul></details>')
    return ('<aside class="site-sidebar"><details class="scene-index" id="scene-index" open>'
            '<summary>Chapters &amp; Scenes</summary><div class="scene-index-body">'
            '<div class="scene-filter-tools" hidden><label for="scene-filter">Find a Scene</label>'
            '<input id="scene-filter" type="search" placeholder="Chapter, scene or title…" autocomplete="off">'
            '<p id="scene-filter-status" role="status" aria-live="polite"></p></div>'
            '<nav aria-label="Chapters and scenes">' + ''.join(sections) + '</nav>'
            f'<nav class="reference-links" aria-label="Story references"><a href="{SCRIPT}#sin-star-i">Script Overview</a>'
            f'<a href="{SCRIPT}#3-cast-and-performance-notes">Cast &amp; Performance Notes</a>'
            '<a href="index.html#canon">Canon &amp; Notes</a></nav></div></details></aside>')


def apply_shell(page, name, sidebar, version):
    view = 'script' if name == SCRIPT else 'storyboard' if name == 'index.html' else 'films'
    tabs = ''.join(f'<a data-view="{key}" href="{path}"' + (' aria-current="page"' if view == key else '') + f'>{label}</a>'
                   for key, path, label in [('script', SCRIPT, 'Script'), ('storyboard', 'index.html', 'Storyboard')])
    header = ('<header class="site-header"><div class="site-header-inner">'
              '<a class="site-brand" href="index.html"><span aria-hidden="true">✦</span> SIN STAR I'
              '<small>Storyboard Draft 3</small></a>'
              f'<nav class="view-tabs" aria-label="Reading view">{tabs}</nav>'
              '<div class="site-tools"><a href="movies.html">Films &amp; Trailer</a>'
              '<button id="site-audio-toggle" type="button" aria-pressed="false" hidden>Audio Off</button>'
              '<button id="site-theme-toggle" type="button" aria-pressed="false" hidden>Dark Theme</button>'
              '<button id="site-print" type="button" hidden>Print</button></div></div></header>')
    header_pattern = r'<header class="(?:bar|topbar)"[^>]*>.*?</header>'
    if re.search(header_pattern, page, re.S):
        page = re.sub(header_pattern, lambda _: header, page, count=1, flags=re.S)
    else:
        # The film gallery has a compact standalone header in its original edition.
        page = re.sub(r'<header>.*?</header>', lambda _: header, page, count=1, flags=re.S)
    if view != 'films':
        page = re.sub(r'<aside class="(?:story-contents|contents)"[^>]*>.*?</aside>', lambda _: sidebar, page, count=1, flags=re.S)
        page = page.replace('<div class="story-layout">', '<div class="site-layout">').replace('<div class="layout">', '<div class="site-layout">')
    page = re.sub(r'<html\b[^>]*>', '<html lang="en" data-theme="dark">', page, count=1)
    page = re.sub(r'<body([^>]*)>', lambda m: f'<body{m.group(1)} data-view="{view}">', page, count=1)
    page = page.replace("style-src 'unsafe-inline'", "style-src 'self' 'unsafe-inline'")
    page = page.replace("script-src 'unsafe-inline'", "script-src 'self' 'unsafe-inline'; media-src 'self'")
    page = re.sub(r'<script>.*?</script>', '', page, flags=re.S)
    page = re.sub(r'<script src="asset/reading.js[^\"]*"></script>', '', page)
    page = page.replace('</head>', f'<script src="asset/site-theme.js?v={version("asset/site-theme.js")}"></script>'
                        f'<link rel="stylesheet" href="asset/site-shell.css?v={version("asset/site-shell.css")}"></head>')
    page = page.replace('</body>', f'<script src="asset/site-shell.js?v={version("asset/site-shell.js")}"></script>'
                        f'<script src="asset/media-preferences.js?v={version("asset/media-preferences.js")}"></script></body>')
    return page
