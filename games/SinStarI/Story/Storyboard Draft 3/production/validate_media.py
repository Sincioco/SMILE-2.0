"""Decode new or changed previews once and retain compact validation evidence."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import re
import subprocess
from media_catalog import all_clips

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'


def check(item):
    path = ROOT / item['video']
    probe = subprocess.run(['ffprobe', '-v', 'error', '-show_streams', '-show_format',
                            '-of', 'json', str(path)], check=True, capture_output=True, text=True)
    data = json.loads(probe.stdout)
    videos = [stream for stream in data['streams'] if stream['codec_type'] == 'video']
    audio = [stream for stream in data['streams'] if stream['codec_type'] == 'audio']
    assert len(videos) == 1 and len(audio) == 1, (item['id'], 'one video and one audio track required')
    video = videos[0]
    assert audio[0]['codec_name'] == 'aac'
    assert video['codec_type'] == 'video' and video['codec_name'] == 'h264'
    assert video['pix_fmt'] == 'yuv420p' and video['r_frame_rate'] == '24/1'
    assert video['width'] == 960 and video['height'] > 400
    duration = float(data['format']['duration'])
    assert 1.4 < duration < max(6.2, item.get('render_seconds', 4) + 0.2), (item['id'], duration)
    decoded = subprocess.run(['ffmpeg', '-hide_banner', '-xerror', '-i', str(path),
                              '-af', 'volumedetect', '-f', 'null', '-'], check=True, capture_output=True, text=True)
    peak = re.search(r'max_volume: ([\w.+-]+) dB', decoded.stderr).group(1)
    mean = re.search(r'mean_volume: ([\w.+-]+) dB', decoded.stderr).group(1)
    assert float(peak) > -90, (item['id'], 'audio is silent', peak)
    return dict(id=item['id'], sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                duration=duration, width=video['width'], height=video['height'],
                bytes=path.stat().st_size, status='Passed', full_decode=True, audio_streams=1,
                peak_dbfs=float(peak), mean_dbfs=float(mean), source_audio_present=float(peak) > -90)


def main():
    items = all_clips(json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8')))
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
