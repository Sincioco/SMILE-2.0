"""Collect finished ComfyUI shots and record actual per-shot status."""
from pathlib import Path
import json
import shutil
import urllib.request

OUT = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Draft 2 Movies')
COMFY = Path(r'D:\Sin - AI Prompt\work\comfy_photo_video_output')


def collect():
    receipts = json.loads((OUT / 'queue-receipts.json').read_text(encoding='utf-8'))
    (OUT / 'clips').mkdir(exist_ok=True)
    (OUT / 'history').mkdir(exist_ok=True)
    report = []
    for job in receipts:
        target = OUT / 'clips' / f'shot-{job["shot"]:02}.mp4'
        history_file = OUT / 'history' / f'shot-{job["shot"]:02}.json'
        if target.exists() and history_file.exists():
            state = {'completed': True, 'status_str': 'success'}
        else:
            with urllib.request.urlopen('http://127.0.0.1:8191/history/' + job['prompt_id'], timeout=30) as response:
                history = json.load(response).get(job['prompt_id'])
            state = history['status'] if history else {'completed': False, 'status_str': 'queued_or_running'}
            if state['completed'] and state['status_str'] == 'success':
                saved = history['outputs']['901']
                asset = saved.get('images', saved.get('videos'))[0]
                shutil.copy2(COMFY / asset.get('subfolder', '') / asset['filename'], target)
                history_file.write_text(json.dumps(history, ensure_ascii=False, indent=2), encoding='utf-8')
            elif state['status_str'] == 'error':
                history_file.write_text(json.dumps(history, ensure_ascii=False, indent=2), encoding='utf-8')
        report.append({'shot': job['shot'], 'prompt_id': job['prompt_id'], 'status': state['status_str'], 'file': str(target) if target.exists() else None})
    (OUT / 'render-status.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps({'completed': sum(r['status'] == 'success' for r in report), 'failed': [r['shot'] for r in report if r['status'] == 'error'], 'total': len(report)}))
    return report


if __name__ == '__main__':
    collect()
