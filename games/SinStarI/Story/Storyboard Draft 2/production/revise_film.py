"""Queue focused second takes for observed identity drift; preserve first takes."""
from pathlib import Path
import json
import sys
from queue_film import request, ui_graph, SOURCE, OUT
REVISIONS = {
42: 'Keep every figure in the supplied illustration exactly as drawn. The party gently advances the stretcher a few inches. The wounded long-silver-haired young man remains weak but breathing on the stretcher. The brown-haired healer checks his breathing; the two differently aged armored men support transport. The red mechanical robot remains entirely robotic. The golden-white dog walks on four paws beside them with its head fully visible. Fixed camera, very small restrained motion, natural cloth and footstep sound. This shot is completely silent of voices; no person speaks or moves their lips. The image contains only the original figures and architecture, unobstructed by any graphics.',
40: 'The original composition stays fixed. The red robot in the center remains completely artificial with the same red angular METAL helmet, mechanical joints and glowing sensor eyes throughout. She is never a human woman. The golden-white dog at left runs only one step forward on four paws. Broad dark-haired Orin and brown-bun Mira retain their own positions and costumes. Wounded long-silver-haired Kael remains supported beside young brown-haired Arin at right. A few black-violet control strands detach from the colossal Aevos behind them, who breaks into distant gold particles. The blue sustaining rings remain whole. Only environmental magical sound, no speech. Locked wide camera, tiny controlled motion, all character identities remain fixed.',
9: 'Exactly three main figures from the image: brown-haired young Arin and brown-bun Mira in white/navy/gold armor, and the black-and-white four-legged canine port administrator in teal harness at the low terminal. The cyan civilian robot in the background stays cyan and fully mechanical. The dog taps the terminal once with a paw; Arin politely steps aside. Locked composition, tiny lateral camera drift. Quiet port ambience. One short male electronic translator voice says: "Marked path, please."',
19: 'Exactly the people visible in the reference: brown-bun Mira at left, golden-white quadrupedal dog Milo, red mechanical robot Zara, broad rugged dark-haired Orin behind, and very long silver-haired youthful Kael at right. NO new figures enter. Kael keeps his face and long silver hair, raises his eyes slowly from the low luminous cosmic model and looks uncertain. Everyone else watches in silence. Camera remains locked. Kael in a calm male voice says: "I would preserve what I could."',
28: 'Preserve the exact six distinct figures and staging from the image. Silver-haired Kael at left maintains a small violet force. Young brown-haired Arin is pushed back only a few inches, and older broad stubbled Orin holds his shoulder steady behind the huge rectangular shield. Brown-bun Mira at right moves a narrow water arc. The red crouching robot remains fully mechanical with red metal face and no human skin. Golden-white dog remains on four paws near the low blue marker. Locked camera and very small body motion. Orin says firmly in a low male voice: "Not that arrangement."',
31: 'Exactly TWO men, nobody else: youthful brown-haired Arin in white/navy/gold armor on the LEFT, and youthful pale very long silver-haired Kael in black/silver armor on the RIGHT. Both remain in the same position and keep their faces. Their separate cyan and violet currents gently brighten while the distant doorway between them opens a few inches. They look alarmed. Locked close two-shot. Kael says quietly in an adult male voice: "Two voices." No other speech or figures.'
}
def main():
    dest=OUT/'revision-receipts.json'
    receipts=json.loads(dest.read_text()) if dest.exists() else []
    template=json.loads((SOURCE/'Sin Star I - Room for One - LTX 2.5 - 24 Seconds.json').read_text(encoding='utf-8'))
    for index,detail in REVISIONS.items():
        if any(j['shot']==index for j in receipts): continue
        g=json.loads((OUT/'workflows'/f'shot-{index:02}-api.json').read_text(encoding='utf-8'))
        prompt='A six-second continuous cinematic painted animation based faithfully on this image. '+detail+' Keep the image unobstructed and every costume unchanged. Natural synchronized environmental audio only, no score. Preserve composition and anatomy throughout.'
        g['101:408']['inputs']['text']=prompt
        g['101:423']['inputs']['noise_seed']=20260916100+index
        g['101:429']['inputs']['strength']=1.0
        g['901']['inputs']['filename_prefix']=f'SinStarI_Draft2/shot-{index:02}-take2'
        ui=ui_graph(template,index,{'caption':'Identity correction, take 2'},prompt,seed=20260916100+index,strength=1.0)
        ui['nodes'][-1]['widgets_values']=[f'SinStarI_Draft2/shot-{index:02}-take2','auto','auto']
        r=request('/prompt',{'prompt':g,'extra_data':{'extra_pnginfo':{'workflow':ui},'workflow_name':f'Sin Star I | Shot {index:02} | Identity correction'}})
        assert not r.get('node_errors'),r
        receipts.append(dict(r,shot=index,prompt=prompt,reason='Observed extra figures or robot identity drift'))
        dest.write_text(json.dumps(receipts,indent=2),encoding='utf-8')
        (OUT/'workflows'/f'shot-{index:02}-take2-api.json').write_text(json.dumps(g,indent=2),encoding='utf-8')
        (OUT/'workflows'/f'shot-{index:02}-take2.json').write_text(json.dumps(ui,indent=2),encoding='utf-8')
        print(json.dumps({'shot':index,'prompt_id':r['prompt_id'],'number':r.get('number')}),flush=True)
if __name__=='__main__': main()
