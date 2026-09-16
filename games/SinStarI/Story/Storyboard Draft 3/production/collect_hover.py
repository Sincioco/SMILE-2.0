"""Collect completed LTX renders and make compact, silent browser previews."""
from pathlib import Path
import json
import shutil
import subprocess
import sys
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'
OUT = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Draft 3 Animations')
COMFY = Path(r'D:\Sin - AI Prompt\work\comfy_photo_video_output')


def export_preview(item, source):
    target = ROOT / item['video']
    target.parent.mkdir(parents=True, exist_ok=True)
    args = ['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y']
    if 'start' in item:
        args += ['-ss', str(item['start'])]
    args += ['-i', str(source)]
    if 'duration' in item:
        args += ['-t', str(item['duration'])]
    filters = item.get('crop', '')
    if filters:
        filters += ','
    filters += 'scale=960:-2,setsar=1'
    args += ['-an', '-vf', filters, '-c:v', 'libx264', '-preset', 'fast', '-crf', '23',
             '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(target)]
    subprocess.run(args, check=True, capture_output=True)


def collect():
    items = json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8'))
    receipts = json.loads((PRODUCTION / 'queue-receipts.json').read_text(encoding='utf-8'))
    jobs = {job['id']: job for job in receipts}
    (OUT / 'clips').mkdir(parents=True, exist_ok=True)
    (OUT / 'history').mkdir(exist_ok=True)
    report = []
    for item in items:
        target = ROOT / item['video']
        source = None
        status = 'pending'
        if item['status'] == 'reuse':
            source = Path(item['source'])
            status = 'reused'
        else:
            job = jobs.get(item['id'], {})
            basename = item['id'] + (f'-take{job["take"]}' if job.get('take', 1) > 1 else '')
            source = OUT / 'clips' / (basename + '.mp4')
            history_file = OUT / 'history' / (basename + '.json')
            if source.exists() and history_file.exists():
                status = 'rendered'
            elif item['id'] in jobs:
                job = jobs[item['id']]
                with urllib.request.urlopen('http://127.0.0.1:8191/history/' + job['prompt_id'], timeout=30) as response:
                    history = json.load(response).get(job['prompt_id'])
                if history:
                    state = history['status']
                    history_file.write_text(json.dumps(history, indent=2), encoding='utf-8')
                    if state['completed'] and state['status_str'] == 'success':
                        saved = history['outputs']['901']
                        asset = saved.get('images', saved.get('videos'))[0]
                        shutil.copy2(COMFY / asset.get('subfolder', '') / asset['filename'], source)
                        status = 'rendered'
                    elif state['status_str'] == 'error':
                        status = 'failed'
        force_reuse = '--refresh-reused' in sys.argv and item['status'] == 'reuse'
        newer_source = source and source.exists() and target.exists() and source.stat().st_mtime > target.stat().st_mtime
        if source and source.exists() and (not target.exists() or force_reuse or newer_source):
            export_preview(item, source)
        report.append(dict(id=item['id'], status=status, ready=target.exists(),
                           bytes=target.stat().st_size if target.exists() else 0))
    (PRODUCTION / 'render-status.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({'ready':sum(r['ready'] for r in report), 'total':len(report),
                      'new_rendered':sum(r['status']=='rendered' for r in report),
                      'failed':[r['id'] for r in report if r['status']=='failed']}), flush=True)
    return report


if __name__ == '__main__':
    collect()
