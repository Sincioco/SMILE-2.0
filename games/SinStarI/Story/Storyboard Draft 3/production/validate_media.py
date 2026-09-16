"""Decode new or changed previews once and retain compact validation evidence."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'


def check(item):
    path = ROOT / item['video']
    probe = subprocess.run(['ffprobe', '-v', 'error', '-show_streams', '-show_format',
                            '-of', 'json', str(path)], check=True, capture_output=True, text=True)
    data = json.loads(probe.stdout)
    assert len(data['streams']) == 1, (item['id'], 'preview must be silent video only')
    video = data['streams'][0]
    assert video['codec_type'] == 'video' and video['codec_name'] == 'h264'
    assert video['pix_fmt'] == 'yuv420p' and video['r_frame_rate'] == '24/1'
    assert video['width'] == 960 and video['height'] > 400
    duration = float(data['format']['duration'])
    assert 1.4 < duration < max(6.2, item.get('render_seconds', 4) + 0.2), (item['id'], duration)
    decoded = subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-i', str(path),
                              '-f', 'null', '-'], check=True, capture_output=True, text=True)
    assert not decoded.stderr.strip(), (item['id'], decoded.stderr)
    return dict(id=item['id'], sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                duration=duration, width=video['width'], height=video['height'],
                bytes=path.stat().st_size, status='Passed', full_decode=True, audio_streams=0)


def main():
    items = json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8'))
    receipt = PRODUCTION / 'media-validation.json'
    old = json.loads(receipt.read_text(encoding='utf-8')) if receipt.exists() else []
    results = {item['id']:item for item in old}
    pending = []
    for item in items:
        path = ROOT / item['video']
        if path.exists() and (item['id'] not in results or results[item['id']]['sha256'] != hashlib.sha256(path.read_bytes()).hexdigest()):
            pending.append(item)
    with ThreadPoolExecutor(max_workers=4) as workers:
        for result in workers.map(check, pending):
            results[result['id']] = result
            receipt.write_text(json.dumps(list(results.values()), indent=2), encoding='utf-8')
    print(json.dumps({'validated':len(results), 'checked_this_run':len(pending), 'total':len(items)}), flush=True)


if __name__ == '__main__':
    main()
