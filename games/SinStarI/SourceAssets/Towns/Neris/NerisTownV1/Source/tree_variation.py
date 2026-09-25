"""Stable, individually varied tree proportions; never compound on regeneration."""
import random
import bpy


def apply():
    rng=random.Random(92673)
    trees=sorted((o for o in bpy.data.objects if o.name.startswith('Garden Tree ')),key=lambda o:o.name)
    for index,obj in enumerate(trees):
        base=obj.get('neris_original_scale')
        if base is None:
            base=list(obj.scale);obj['neris_original_scale']=base
        height=rng.uniform(.84,1.23)
        if index%13==2:height=1.44
        elif index%11==3:height=.72
        width=rng.uniform(.90,1.08)
        obj.scale=(base[0]*width,base[1]*width,base[2]*height)
        obj['neris_height_variation']=height
    print('TREES',len(trees),'stable varied heights; a few 144% accents and 72% young trees',flush=True)
