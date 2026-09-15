"""Append individually recoverable LTX 2.5 shots to the existing ComfyUI queue."""
from pathlib import Path
import copy
import json
import shutil
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Room for One - LTX 2.5')
INPUT = Path(r'D:\AI\Mira3D\ComfyUI\input\SinStarI_Draft2')
OUT = Path(r'D:\Sin - AI Prompt - Contents\Sin Star I - Draft 2 Movies')
API = 'http://127.0.0.1:8191'


def request(path, value=None):
    data = None if value is None else json.dumps(value).encode('utf-8')
    req = urllib.request.Request(API + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=60) as response:
        return json.load(response)


def shot_graph(template, shot, index, panel):
    graph = {k: copy.deepcopy(v) for k, v in template.items() if k.startswith('101:')}
    graph['1'] = {'class_type': 'LoadImage', 'inputs': {'image': f'SinStarI_Draft2/{index:02}.png'}, '_meta': {'title': f'{index:02} | {panel["caption"]}'}}
    graph['101:433']['inputs']['input'] = ['1', 0]
    graph['101:423']['inputs']['noise_seed'] = 2026091600 + index
    speech = ' '.join(f'{who} says, "{line}"' for who, line in shot['lines'])
    prompt = (
        f'Sin Star I. One continuous six-second cinematic painted animation shot, based exactly on the supplied individual illustration. '
        f'Preserve its recognizable faces, costume, proportions, setting, composition and detailed graphic-novel artwork. '
        f'{shot["motion"]} '
        f'Audio: {speech if speech else "No speech in this shot."} '
        'Dialogue is clear, natural and emotionally acted, with a short pause between speakers. '
        'Arin has a low young adult male voice; Orin is an older gravelly male; Mira is a warm clear adult woman; '
        'Kael is a calm resonant adult man; Zara is a precise expressive adult female artificial voice; '
        'Milo speaks through his translator in a lively adult male voice, retaining normal canine anatomy. '
        'Use quiet synchronized environmental sound, footsteps, cloth, machinery or magic where appropriate. '
        'No background music, narrator, subtitles, titles or lettering; score and readable captions are added in editing. '
        'Do not introduce characters, cuts, new actions or costume changes beyond the described shot. '
        'Milo remains a four-legged dog. Zara remains a red-and-charcoal robot without human skin or hair. '
        'Arin and Orin remain different men. Wounded Kael remains alive without a miraculous recovery.'
    )
    graph['101:408']['inputs']['text'] = prompt
    graph['901'] = {'class_type': 'SaveVideo', 'inputs': {
        'video': ['101:451', 0], 'filename_prefix': f'SinStarI_Draft2/shot-{index:02}',
        'format': 'mp4', 'format.codec': 'h264', 'format.codec.encoding': 'auto'},
        '_meta': {'title': f'Sin Star I Draft 2 | Shot {index:02} | {panel["caption"]}'}}
    return graph, prompt


def set_take_settings(workflow, seed, strength):
    """Keep promoted UI values and their inner nodes aligned with the API graph."""
    shot = next(n for n in workflow['nodes'] if n['id'] == 101)
    shot['widgets_values'][5] = seed
    shot['widgets_values_named']['noise_seed'] = seed
    for group in workflow.get('definitions', {}).get('subgraphs', []):
        for node in group['nodes']:
            if node['id'] == 423:
                node['widgets_values'] = [seed, 'fixed']
                node['widgets_values_named'] = {'noise_seed': seed, 'control_after_generate': 'fixed'}
            elif node['id'] == 429:
                node['widgets_values'] = [strength, False]
                node['widgets_values_named'] = {'strength': strength, 'bypass': False}


def ui_graph(template, index, panel, prompt, seed=None, strength=0.7):
    workflow = copy.deepcopy(template)
    workflow['id'] = f'sin-star-draft2-shot-{index:02}'
    workflow['nodes'] = [n for n in workflow['nodes'] if n['id'] in (1, 101, 901)]
    load, shot, save = (next(n for n in workflow['nodes'] if n['id'] == key) for key in (1, 101, 901))
    load['widgets_values'] = [f'SinStarI_Draft2/{index:02}.png', 'image']
    load['widgets_values_named'] = {'image': f'SinStarI_Draft2/{index:02}.png', 'upload': 'image'}
    load['title'] = f'{index:02} | {panel["caption"]}'
    load['outputs'][0]['links'] = [1]
    shot['inputs'][0]['link'] = 1
    shot['outputs'][0]['links'] = [2]
    shot['widgets_values'][0] = prompt
    shot['widgets_values'][5] = 2026091600 + index
    shot['widgets_values_named'] = dict(zip(shot['widgets_values_named'], shot['widgets_values']))
    shot['title'] = f'Shot {index:02} | LTX 2.5 | 6 Seconds'
    shot['pos'] = [500, 100]
    save['inputs'][0]['link'] = 2
    save['widgets_values'] = [f'SinStarI_Draft2/shot-{index:02}', 'auto', 'auto']
    save['pos'] = [1150, 100]
    save['title'] = f'Sin Star I Draft 2 | Render {index:02}'
    workflow['links'] = [[1, 1, 0, 101, 0, 'IMAGE'], [2, 101, 0, 901, 0, 'VIDEO']]
    workflow['last_link_id'] = 2
    set_take_settings(workflow, seed if seed is not None else 2026091600 + index, strength)
    return workflow


def main():
    INPUT.mkdir(exist_ok=True)
    (OUT / 'workflows').mkdir(parents=True, exist_ok=True)
    panels = {p['id']: p for p in json.loads((ROOT / 'production/illustration-index.json').read_text(encoding='utf-8'))}
    shots = json.loads((ROOT / 'production/film_shots.json').read_text(encoding='utf-8'))
    api_template = json.loads((SOURCE / 'Room-for-One-API.json').read_text(encoding='utf-8'))
    ui_template = json.loads((SOURCE / 'Sin Star I - Room for One - LTX 2.5 - 24 Seconds.json').read_text(encoding='utf-8'))
    receipts_path = OUT / 'queue-receipts.json'
    receipts = json.loads(receipts_path.read_text()) if receipts_path.exists() else []
    for index, shot in enumerate(shots, 1):
        if any(r['shot'] == index for r in receipts):
            continue
        panel = panels[shot['panel']]
        shutil.copy2(ROOT / 'asset/images' / panel['file'], INPUT / f'{index:02}.png')
        api, prompt = shot_graph(api_template, shot, index, panel)
        workflow = ui_graph(ui_template, index, panel, prompt)
        title = f'Sin Star I Draft 2 | {index:02}/{len(shots)} | {panel["caption"]}'
        payload = {'prompt': api, 'extra_data': {'extra_pnginfo': {'workflow': workflow}, 'workflow_name': title}}
        (OUT / 'workflows' / f'shot-{index:02}.json').write_text(json.dumps(workflow, ensure_ascii=False, indent=2), encoding='utf-8')
        (OUT / 'workflows' / f'shot-{index:02}-api.json').write_text(json.dumps(api, ensure_ascii=False, indent=2), encoding='utf-8')
        receipt = request('/prompt', payload)
        assert not receipt.get('node_errors'), receipt
        receipts.append(dict(receipt, shot=index, panel=shot['panel'], caption=panel['caption'], prompt=prompt))
        receipts_path.write_text(json.dumps(receipts, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps({'shot': index, 'prompt_id': receipt['prompt_id'], 'number': receipt.get('number')}, ensure_ascii=True), flush=True)
    print(f'Queued {len(receipts)} shots. Existing jobs were preserved.', flush=True)


if __name__ == '__main__':
    main()
