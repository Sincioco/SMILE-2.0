"""Local film edit: normalized shots, readable titles, music and exact credits.

Uses the user's existing FFmpeg installation. Rendering remains separate from
editing; neither this script nor queue_film.py cancels other ComfyUI jobs.
"""
from pathlib import Path
import json
import subprocess
import shutil
import sys
import time
from collect_renders import collect

ROOT = Path(__file__).resolve().parents[1]
OUT = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Draft 2 Movies')
EDIT = OUT / 'edit'
ASSETS = ROOT.parents[1] / 'Assets' / 'Music'
FFMPEG = shutil.which('ffmpeg')
FFPROBE = shutil.which('ffprobe')
POSTER = Path(r'D:\Sin - AI Prompt - Contents\2026-09-15 1809 - Sin Star I - Story Board 2\image-103ada11c940727e1b61aa2bcf1065123bb49069fc6ae2b4009fd525e62219e6.png')
CREDIT = 'Created by: Louiery R. Sincioco (Sin)'
FONT = "fontfile='C\\:/Windows/Fonts/georgiab.ttf'"
SANS = "fontfile='C\\:/Windows/Fonts/segoeui.ttf'"


def run(args, name):
    EDIT.mkdir(parents=True, exist_ok=True)
    with (EDIT / f'{name}.log').open('w', encoding='utf-8') as log:
        subprocess.run([FFMPEG, '-hide_banner', '-y', *map(str, args)], cwd=EDIT, stdout=log, stderr=log, check=True)


def duration(path):
    return float(subprocess.check_output([FFPROBE, '-v', 'error', '-show_entries', 'format=duration', '-of', 'default=nw=1:nk=1', str(path)], text=True))


def text_filter(name, text, size, y, *, color='white', serif=False, box=False):
    (EDIT / f'{name}.txt').write_bytes(text.encode('utf-8'))
    return (f'drawtext={FONT if serif else SANS}:textfile={name}.txt:fontsize={size}:'
            f'fontcolor={color}:x=(w-text_w)/2:y={y}:line_spacing=14:shadowcolor=black@0.8:shadowx=2:shadowy=2'
            + (':box=1:boxcolor=black@0.58:boxborderw=20' if box else ''))


def encode_args():
    return ['-c:v', 'libx264', '-preset', 'veryfast', '-crf', '18', '-pix_fmt', 'yuv420p', '-r', '24',
            '-c:a', 'aac', '-b:a', '192k', '-ar', '48000', '-ac', '2', '-movflags', '+faststart']


def normalize(source, output, seconds=None, caption=None, name='normalize'):
    # This silent shot acquired unwanted generated lettering near its lower edge.
    crop = 'crop=iw:ih-140:0:0,' if name == 'film-shot-05' else ''
    vf = crop + 'fps=24,scale=1920:1080:force_original_aspect_ratio=decrease:flags=lanczos,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=0x080e1a,setsar=1'
    if caption:
        vf += ',' + text_filter(name, caption, 72, '(h-text_h)/2', serif=True, box=True)
    args = ['-i', source, '-vf', vf, '-af', 'aresample=48000,apad', '-t', seconds or duration(source), *encode_args(), output]
    run(args, name)


def credit_card(output, seconds=6):
    EDIT.mkdir(parents=True, exist_ok=True)
    filters = [text_filter('credit-title', 'SIN STAR', 120, 150, color='0xe6c784', serif=True),
               text_filter('credit-status', 'IN DEVELOPMENT', 34, 320, color='0xd1d9e6'),
               text_filter('credit-creator', CREDIT, 48, 438),
               text_filter('credit-first', 'The first game project to use the new SMILE', 45, 590),
               text_filter('credit-tools', 'Programming Language, Compiler,', 42, 663, color='0xd1d9e6'),
               text_filter('credit-libraries', 'Libraries and Tool Chain', 42, 733, color='0xd1d9e6'),
               text_filter('credit-open', 'Open-source project', 36, 838, color='0xe6c784'),
               text_filter('credit-url', 'https://github.com/sincioco/smile-2.0', 36, 895, color='0xd1d9e6')]
    run(['-f', 'lavfi', '-i', f'color=c=0x080e1a:s=1920x1080:r=24:d={seconds}',
         '-f', 'lavfi', '-i', 'anullsrc=r=48000:cl=stereo', '-vf', ','.join(filters) + f',fade=t=out:st={seconds-0.5}:d=0.5',
         '-t', seconds, *encode_args(), output], 'credit-card')


def poster_card(output, seconds=2):
    run(['-loop', '1', '-i', POSTER, '-f', 'lavfi', '-i', 'anullsrc=r=48000:cl=stereo',
         '-vf', 'scale=1920:1080:force_original_aspect_ratio=decrease:flags=lanczos,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=0x080e1a,setsar=1,fade=t=in:st=0:d=0.3',
         '-t', seconds, *encode_args(), output], 'poster-card')


def concatenate(paths, output, name):
    listing = EDIT / f'{name}.txt'
    listing.write_text('\n'.join("file '" + str(p).replace('\\', '/').replace("'", "'\\''") + "'" for p in paths), encoding='utf-8')
    run(['-f', 'concat', '-safe', '0', '-i', listing, '-c', 'copy', '-movflags', '+faststart', output], name)


def room_for_one():
    source = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Room for One - LTX 2.5\Sin-Star-I-Room-for-One.mp4')
    EDIT.mkdir(parents=True, exist_ok=True)
    normalized, credits = EDIT / 'room-base.mp4', EDIT / 'room-credits.mp4'
    normalize(source, normalized, name='room-normalize')
    credit_card(credits, 5.75)
    output = OUT / 'Sin-Star-I-Room-for-One-Credited.mp4'
    concatenate([normalized, credits], output, 'room-join')
    assert 20 <= duration(output) <= 30
    print(json.dumps({'movie': str(output), 'seconds': duration(output), 'credit': CREDIT}), flush=True)


def score_film(source, output):
    seconds = duration(source)
    final = seconds - 164
    filters = (
        '[0:a]asplit=2[voice][key];'
        '[1:a]atrim=0:104,asetpts=PTS-STARTPTS,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[march];'
        '[2:a]atrim=0:64,asetpts=PTS-STARTPTS,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[bloom];'
        f'[3:a]atrim=0:{final},asetpts=PTS-STARTPTS,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[ascend];'
        '[march][bloom]acrossfade=d=2:c1=tri:c2=tri[first];'
        f'[first][ascend]acrossfade=d=2:c1=tri:c2=tri,volume=0.28,afade=t=in:st=0:d=0.3,afade=t=out:st={seconds-4}:d=4[music];'
        '[music][key]sidechaincompress=threshold=0.025:ratio=7:attack=20:release=350[ducked];'
        '[voice][ducked]amix=inputs=2:duration=first:normalize=0,loudnorm=I=-16:TP=-1.5:LRA=11[audio]'
    )
    run(['-i', source, '-stream_loop', '-1', '-i', ASSETS / 'Starforge March.mp3',
         '-stream_loop', '-1', '-i', ASSETS / 'Bloom.mp3', '-stream_loop', '-1', '-i', ASSETS / 'Starforge Ascend.mp3',
         '-filter_complex', filters, '-map', '0:v', '-map', '[audio]', '-c:v', 'copy', '-c:a', 'aac',
         '-b:a', '256k', '-t', seconds, '-movflags', '+faststart', output], 'film-score')


def make_trailer():
    # Deliberately nonchronological: danger, attachment, fellowship, resistance.
    sequence = [(32, 'WHAT IF YOUR WORLD\nWAS MARKED FOR ERASURE?'), (35, None),
                (1, 'EVERY LIFE MATTERS.'), (43, 'FIND YOUR PEOPLE.'), (12, None),
                (10, 'STAND TOGETHER.'), (15, None), (29, 'DEFY YOUR DESTINY.'),
                (30, None), (38, 'FIGHT FOR THIS UNIVERSE.'), (34, None)]
    parts = []
    for index, (shot, caption) in enumerate(sequence, 1):
        target = EDIT / f'trailer-{index:02}.mp4'
        normalize(OUT / 'clips' / f'shot-{shot:02}.mp4', target, 2, caption, f'trailer-{index:02}')
        parts.append(target)
    end = EDIT / 'trailer-credit.mp4'
    credit_card(end, 8)
    parts.append(end)
    silent = EDIT / 'trailer-edit.mp4'
    concatenate(parts, silent, 'trailer-join')
    output = OUT / 'Sin-Star-I-A-Universe-Worth-Fighting-For-Trailer.mp4'
    run(['-i', silent, '-ss', 24, '-stream_loop', '-1', '-i', ASSETS / 'Starforge Ascend.mp3',
         '-filter_complex', '[1:a]atrim=0:30,asetpts=PTS-STARTPTS,afade=t=in:st=0:d=0.08,afade=t=out:st=27:d=3,loudnorm=I=-15:TP=-1.5:LRA=9[a]',
         '-map', '0:v', '-map', '[a]', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '256k', '-t', 30,
         '-movflags', '+faststart', output], 'trailer-score')
    assert 29.9 <= duration(output) <= 30.1
    return output


def full_production(watch=False):
    EDIT.mkdir(parents=True, exist_ok=True)
    while True:
        report = collect()
        if any(r['status'] == 'error' for r in report):
            raise RuntimeError('A render failed; inspect render-status.json before continuing.')
        for job in report:
            normalized = EDIT / f'film-shot-{job["shot"]:02}.mp4'
            if job['file'] and not normalized.exists():
                normalize(Path(job['file']), normalized, 145 / 24, name=f'film-shot-{job["shot"]:02}')
                print(f'Prepared shot {job["shot"]:02}/{len(report)}', flush=True)
        if all(r['status'] == 'success' for r in report):
            break
        if not watch:
            return
        time.sleep(20)
    opening, ending = EDIT / 'film-opening.mp4', EDIT / 'film-credits.mp4'
    poster_card(opening, 2)
    credit_card(ending, 10)
    parts = [opening] + [EDIT / f'film-shot-{i:02}.mp4' for i in range(1, 45)] + [ending]
    assembled = EDIT / 'film-assembled.mp4'
    concatenate(parts, assembled, 'film-join')
    film = OUT / 'Sin-Star-I-A-Universe-Worth-Fighting-For.mp4'
    score_film(assembled, film)
    assert 180 <= duration(film) <= 300
    trailer = make_trailer()
    results = {'film': str(film), 'film_seconds': duration(film), 'trailer': str(trailer), 'trailer_seconds': duration(trailer),
               'credit': CREDIT, 'source_project': 'https://github.com/sincioco/smile-2.0',
               'status': 'Rendered; visual, audio and package checks pending'}
    (OUT / 'edit-results.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
    print(json.dumps(results), flush=True)


if __name__ == '__main__':
    if sys.argv[1:] == ['room']:
        room_for_one()
    elif sys.argv[1:] == ['watch']:
        full_production(watch=True)
    elif sys.argv[1:] == ['prepare']:
        full_production()
