"""Append missing image animations to ComfyUI; preserve every existing job."""
from pathlib import Path
import copy
import json
import shutil
import urllib.request
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / 'production'
SOURCE = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Room for One - LTX 2.5')
INPUT = Path(r'D:\AI\Mira3D\ComfyUI\input\SinStarI_Draft3')
OUT = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Draft 3 Animations')
API = 'http://127.0.0.1:8191'


def request(path, value=None):
    data = None if value is None else json.dumps(value).encode('utf-8')
    req = urllib.request.Request(API + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=60) as response:
        return json.load(response)


def build_graph(item, index, api_template, ui_template):
    graph = {k: copy.deepcopy(v) for k, v in api_template.items() if k.startswith('101:')}
    image_path = f'SinStarI_Draft3/{item["id"]}.png'
    prefix = f'SinStarI_Draft3/{item["id"]}'
    width, height = Image.open(ROOT / item['image']).size
    render_width, render_height = (1152, 640) if width / height > 1.7 else (1152, 768)
    seed = 20260916300 + index
    prompt = (
        'One continuous four-second cinematic painted animation of exactly the supplied illustration. '
        'Keep the camera locked and retain the same composition, people, faces, anatomy, clothing and props. '
        f'The visible scene is: {item["art"]} '
        'Bring only the existing scene gently to life: subtle breathing, one natural blink or small glance, '
        'slight cloth or hair movement, and slow motion in existing water, fire, dust or light. '
        'Do not act out a new event or introduce a new person. Keep every subject in the same location. '
        'All dogs remain quadrupedal with normal canine paws and faces. Any red robot remains entirely metal '
        'with mechanical facial plates; any long silver-haired man retains his own face and hair. '
        'Very restrained motion, faithful painted detail, stable elegant cinematic light. '
        'Nobody speaks or moves their lips. No dialogue, voice, music, captions or new lettering. '
        'Preserve any existing lettering exactly. Quiet environmental sound only.'
    )
    prompt = item.get('motion_prompt', prompt)
    seconds = item.get('render_seconds', 4)
    frames = seconds * 24 + 1
    graph['1'] = {'class_type': 'LoadImage', 'inputs': {'image': image_path}, '_meta': {'title': item['caption']}}
    graph['101:433']['inputs']['input'] = ['1', 0]
    graph['101:408']['inputs']['text'] = prompt
    graph['101:423']['inputs']['noise_seed'] = seed
    graph['101:429']['inputs']['strength'] = 1.0
    graph['101:437']['inputs'].update(width=render_width // 2, height=render_height // 2, length=frames)
    graph['101:430']['inputs'].update(frames_number=frames, frame_rate=24)
    graph['901'] = {'class_type': 'SaveVideo', 'inputs': {
        'video': ['101:451', 0], 'filename_prefix': prefix, 'format': 'mp4',
        'format.codec': 'h264', 'format.codec.encoding': 'auto'},
        '_meta': {'title': f'Draft 3 Hover | {item["caption"]}'}}
    ui = copy.deepcopy(ui_template)
    ui['id'] = f'sin-star-draft3-{item["id"]}'
    ui['nodes'] = [n for n in ui['nodes'] if n['id'] in (1, 101, 901)]
    load, shot, save = (next(n for n in ui['nodes'] if n['id'] == key) for key in (1, 101, 901))
    load.update(widgets_values=[image_path, 'image'], widgets_values_named={'image': image_path, 'upload': 'image'}, title=item['caption'])
    load['outputs'][0]['links'] = [1]
    shot['inputs'][0]['link'] = 1
    shot['outputs'][0]['links'] = [2]
    shot['widgets_values'][:7] = [prompt, False, seconds, render_width, render_height, seed, 24]
    shot['widgets_values_named'] = dict(zip(shot['widgets_values_named'], shot['widgets_values']))
    shot.update(title=f'Draft 3 | {item["id"]} | {seconds} Seconds', pos=[500, 100])
    save['inputs'][0]['link'] = 2
    save.update(widgets_values=[prefix, 'auto', 'auto'], title=f'Animated Illustration | {item["id"]}', pos=[1150, 100])
    ui['links'] = [[1, 1, 0, 101, 0, 'IMAGE'], [2, 101, 0, 901, 0, 'VIDEO']]
    ui['last_link_id'] = 2
    for group in ui.get('definitions', {}).get('subgraphs', []):
        for node in group['nodes']:
            if node['id'] == 423:
                node.update(widgets_values=[seed, 'fixed'], widgets_values_named={'noise_seed': seed, 'control_after_generate': 'fixed'})
            elif node['id'] == 429:
                node.update(widgets_values=[1.0, False], widgets_values_named={'strength': 1.0, 'bypass': False})
    return graph, ui, prompt


def main():
    INPUT.mkdir(exist_ok=True)
    OUT.mkdir(exist_ok=True)
    workflows = PRODUCTION / 'workflows'
    workflows.mkdir(exist_ok=True)
    items = json.loads((PRODUCTION / 'hover-media.json').read_text(encoding='utf-8'))
    api_template = json.loads((SOURCE / 'Room-for-One-API.json').read_text(encoding='utf-8'))
    ui_template = json.loads((SOURCE / 'Sin Star I - Room for One - LTX 2.5 - 24 Seconds.json').read_text(encoding='utf-8'))
    receipt_path = PRODUCTION / 'queue-receipts.json'
    receipts = json.loads(receipt_path.read_text(encoding='utf-8')) if receipt_path.exists() else []
    for index, item in enumerate(items):
        if item['status'] == 'reuse' or any(r['id'] == item['id'] for r in receipts):
            continue
        shutil.copy2(ROOT / item['image'], INPUT / f'{item["id"]}.png')
        api, ui, prompt = build_graph(item, index, api_template, ui_template)
        for suffix, value in [('-api', api), ('', ui)]:
            (workflows / (item['id'] + suffix + '.json')).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
        result = request('/prompt', {'prompt': api, 'extra_data': {
            'extra_pnginfo': {'workflow': ui}, 'workflow_name': f'Sin Star I Draft 3 | {item["id"]} | {item["caption"]}'}})
        assert not result.get('node_errors'), result
        receipts.append(dict(result, id=item['id'], prompt=prompt))
        receipt_path.write_text(json.dumps(receipts, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'id': item['id'], 'number': result.get('number')}), flush=True)
    print(f'{len(receipts)} animations submitted; prior queue entries preserved.', flush=True)


if __name__ == '__main__':
    main()
