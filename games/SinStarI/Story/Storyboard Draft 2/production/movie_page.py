"""Build the static movie gallery from verified local exports and upload receipts."""
from html import escape
import json


def build_movies(root, version):
    movies = json.loads((root / 'production/movies.json').read_text(encoding='utf-8'))
    entries = []
    for movie in movies:
        file = 'asset/videos/' + movie['file']
        poster = 'asset/images/' + movie['poster']
        entries.append(f'''<section class="movie" id="{movie['id']}" aria-labelledby="{movie['id']}-title">
<div class="movie-heading"><p class="eyebrow">{escape(movie['kind'])} · {escape(movie['duration'])}</p>
<h2 id="{movie['id']}-title">{escape(movie['name'])}</h2><p>{escape(movie['description'])}</p></div>
<video controls playsinline preload="none" poster="{poster}?v={version(root / poster)}" aria-label="{escape(movie['name'], quote=True)}">
<source src="{file}?v={version(root / file)}" type="video/mp4">Your browser cannot play this video. Use the MP4 link below.</video>
<div class="movie-links"><a href="{file}" download>Download MP4</a><a href="{escape(movie['url'], quote=True)}" target="_blank" rel="noopener">Watch on YouTube ↗</a><a href="index.html#{movie['scene']}">Explore the Storyboard →</a></div></section>''')
    page = f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="theme-color" content="#0c1422"><meta name="description" content="Sin Star I cinematic story film, trailer and Room for One preview.">
<title>Sin Star I · Films &amp; Trailer</title><link rel="icon" href="asset/star.svg">
<link rel="stylesheet" href="asset/storyboard.css?v={version(root / 'asset/storyboard.css')}"></head>
<body id="top" class="movie-page"><a class="skip" href="#movies">Skip to the movies</a>
<header class="bar"><div class="bar-inner"><a class="brand" href="index.html"><span aria-hidden="true">✦</span> SIN STAR I</a>
<nav class="navigation" aria-label="Reading navigation"><a href="index.html">Visual Storyboard</a><a href="Sin-Star-I-Game-Script-v0.2.html">Full Script</a></nav></div></header>
<main id="movies"><div class="intro"><div><p class="eyebrow">A Game in Development</p><h1>A universe worth fighting for.</h1></div>
<p>Meet your people. Choose your fate.<br>Begin with the trailer, then discover their story.</p></div>
<nav class="movie-jumps" aria-label="Choose a movie"><a href="#trailer">30-Second Trailer</a><a href="#film">Cinematic Story Film</a><a href="#room-for-one">Room for One</a></nav>
{''.join(entries)}
<section class="movie-credits" id="credits"><p class="eyebrow">Sin Star I</p><h2>Created by: Louiery R. Sincioco (Sin)</h2>
<p>Sin Star is the first game project to use the new SMILE Programming Language, Compiler, Libraries and Tool Chain.</p>
<p>Open-source project · <a href="https://github.com/sincioco/smile-2.0" target="_blank" rel="noopener">Explore SMILE 2.0 on GitHub ↗</a></p>
<p class="movie-note">Cinematic concept previews animated from storyboard artwork with LTX 2.5. The story film contains major spoilers. Music in the story film: Starforge March, Bloom and Starforge Ascend.</p>
</section></main><footer><p>Sin Star I · Storyboard Draft 2</p><a href="index.html">Return to the Visual Storyboard ↑</a></footer></body></html>'''
    (root / 'movies.html').write_text(page, encoding='utf-8')
