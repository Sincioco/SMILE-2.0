"""Final edit using reviewed takes and clean, script-authored captions."""
from pathlib import Path
import json
import shutil
import sys
import textwrap
import time
import urllib.request
from edit_movies import ROOT, OUT, EDIT, ASSETS, run, duration, encode_args, text_filter, credit_card, poster_card, concatenate, score_film

SHOTS=json.loads((ROOT/'production/film_shots.json').read_text(encoding='utf-8'))
TAKES=OUT/'revised'
CLEAN=EDIT/'clean'
CAPTION_LINES={19:[['Kael','I would preserve what I could.']],31:[['Kael','Two voices.']],40:[]}
# Observed generated lettering falls below source row 600. Preserve faces above it.
FRAME='crop=iw:600:0:0,fps=24,scale=1920:1000:flags=lanczos,pad=1920:1080:0:0:color=0x080e1a,setsar=1'
SHOT_SECONDS=145/24


def collect_revisions():
    TAKES.mkdir(exist_ok=True)
    jobs=json.loads((OUT/'revision-receipts.json').read_text(encoding='utf-8'))
    complete=True
    for job in jobs:
        target=TAKES/f'shot-{job["shot"]:02}.mp4'
        if target.exists(): continue
        with urllib.request.urlopen('http://127.0.0.1:8191/history/'+job['prompt_id'],timeout=30) as response:
            history=json.load(response).get(job['prompt_id'])
        if not history:
            complete=False
            continue
        if history['status']['status_str']=='error':
            raise RuntimeError('Revision failed: '+str(job['shot']))
        if not history['status']['completed']:
            complete=False
            continue
        asset=history['outputs']['901'].get('images',history['outputs']['901'].get('videos'))[0]
        source=Path(r'D:\Sin - AI Prompt\work\comfy_photo_video_output')/asset.get('subfolder','')/asset['filename']
        shutil.copy2(source,target)
        (TAKES/f'shot-{job["shot"]:02}.json').write_text(json.dumps(history,indent=2),encoding='utf-8')
        print(f'Collected corrected take {job["shot"]:02}',flush=True)
    return complete


def source_for(index):
    revised=TAKES/f'shot-{index:02}.mp4'
    return revised if revised.exists() else OUT/'clips'/f'shot-{index:02}.mp4'


def clean_shot(index):
    CLEAN.mkdir(exist_ok=True)
    lines=CAPTION_LINES.get(index,SHOTS[index-1]['lines'])
    frame = 'fps=24,scale=1440:960,pad=1920:1080:240:0:color=0x080e1a,setsar=1' if index == 42 else FRAME
    vf=frame+',drawbox=x=0:y=960:w=iw:h=120:color=0x080e1a:t=fill'
    weights=[max(3,len(line.split())) for _,line in lines]
    cursor=0.15
    for n,((who,line),weight) in enumerate(zip(lines,weights),1):
        end=cursor+(SHOT_SECONDS-0.3)*weight/sum(weights)
        caption='\n'.join(textwrap.wrap(who.upper()+': '+line,width=78))
        assert caption.count('\n')<2,(index,caption)
        vf+=','+text_filter(f'sub-{index:02}-{n}',caption,44,'960+(120-text_h)/2')+f":enable='between(t,{cursor:.3f},{end:.3f})'"
        cursor=end
    # Silent actions carry only the supplied score, avoiding invented dialogue.
    af='volume=0,aresample=48000,apad' if index in (5,30,40,42) else 'aresample=48000,apad'
    target=CLEAN/f'shot-{index:02}.mp4'
    run(['-i',source_for(index),'-vf',vf,'-af',af,'-t',SHOT_SECONDS,*encode_args(),target],f'clean-{index:02}')
    return target


def trailer():
    sequence=[(35,1,'DEFY THE END.'),(32,2,None),(1,0.5,'EVERY LIFE MATTERS.'),
              (43,2,'FIND YOUR PEOPLE.'),(12,1,None),(10,1,'STAND TOGETHER.'),
              (15,1,None),(29,1,'CHOOSE YOUR OWN FATE.'),(30,1,None),
              (38,1,'FIGHT FOR THIS UNIVERSE.'),(34,1,None)]
    parts=[]
    for number,(shot,start,caption) in enumerate(sequence,1):
        target=CLEAN/f'trailer-{number:02}.mp4'
        vf=FRAME
        if caption:
            vf+=','+text_filter(f'promo-{number:02}',caption,76,'h-240',serif=True,box=True)
        run(['-ss',start,'-i',source_for(shot),'-vf',vf,'-af','volume=0,aresample=48000,apad','-t',2,*encode_args(),target],f'promo-{number:02}')
        parts.append(target)
    credits=CLEAN/'trailer-credits.mp4'
    credit_card(credits,8)
    parts.append(credits)
    joined=CLEAN/'trailer-joined.mp4'
    concatenate(parts,joined,'clean-trailer-join')
    output=OUT/'Sin-Star-I-A-Universe-Worth-Fighting-For-Trailer.mp4'
    run(['-i',joined,'-ss',24,'-stream_loop','-1','-i',ASSETS/'Starforge Ascend.mp3',
         '-filter_complex','[1:a]atrim=0:30,asetpts=PTS-STARTPTS,afade=t=in:st=0:d=0.08,afade=t=out:st=27:d=3,loudnorm=I=-15:TP=-1.5:LRA=9[a]',
         '-map','0:v','-map','[a]','-c:v','copy','-c:a','aac','-b:a','256k','-t',30,'-movflags','+faststart',output],'clean-trailer-score')
    return output


def main():
    while not collect_revisions():
        print('Waiting for queued correction takes.',flush=True)
        time.sleep(20)
    parts=[]
    for i in range(1,45):
        p=CLEAN/f'shot-{i:02}.mp4'
        if not p.exists(): clean_shot(i)
        parts.append(p)
        print(f'Clean captions / framing: {i}/44',flush=True)
    opening,ending=CLEAN/'opening.mp4',CLEAN/'credits.mp4'
    poster_card(opening,2)
    credit_card(ending,10)
    assembled=CLEAN/'assembled.mp4'
    concatenate([opening,*parts,ending],assembled,'clean-film-join')
    film=OUT/'Sin-Star-I-A-Universe-Worth-Fighting-For.mp4'
    score_film(assembled,film)
    preview=trailer()
    result={'film':str(film),'film_seconds':duration(film),'trailer':str(preview),'trailer_seconds':duration(preview),
            'status':'Corrected edit exported; final technical and visual review pending',
            'corrected_takes':[9,19,28,31,40,42],'caption_source':'Authored script lines, independently typeset',
            'music':['Starforge March','Bloom','Starforge Ascend'],'credit':'Created by: Louiery R. Sincioco (Sin)'}
    assert 180<=result['film_seconds']<=300
    assert 29.9<=result['trailer_seconds']<=30.1
    (OUT/'edit-results.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result),flush=True)

if __name__=='__main__': main()
