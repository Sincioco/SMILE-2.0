"""Original fire forms inspired by Jared Koh and Arcomade's visual references.

bm35JrFHqPQ; jwwwfwCQl_A 0:24-0:54; T5vdPy7nbRQ 0:37-0:54.
Chambered punches, a grounded sweeping kick, and a two-palm lunge.
No downloaded motion or effect assets. Preserve all sixteen accepted clips.
"""
import math
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_kael_earth import bake_casts


def sample(name, t, side, idle):
    neutral=(0,0,0,0,*idle,0,0,0,0)
    s=1 if side=='Left' else -1
    if name=='FirePunch':
        keys=[neutral,
            (.12,.12,.48,-.06,.24*s,.12,.66,1,.8,.14,0),
            (.28,.25,-.48,.26,.12*s,-.62,.67,1,1.2,0,0),
            (.40,.15,-.42,-.05,.24*s,.10,.66,1,.8,0,0),
            (.56,.26,.50,.27,.13*s,-.62,.73,1,1.3,0,0),
            (.69,.24,.38,.20,.18*s,-.48,.66,1,1.2,0,0)]
        if side=='Right':
            keys[2]=(.28,.25,-.48,.26,-.27,.10,.69,1,1.2,0,0)
        else:
            keys[4]=(.56,.26,.50,.27,.28,.10,.69,1,1.3,0,0)
    elif name=='FireSweep':
        keys=[neutral,
            (.14,.14,.52,-.08,.30*s,.18,.22,.8,.5,.22,0),
            (.29,.19,.66,-.10,.42*s,.06,.87,.7,.4,.30,0),
            (.46,.21,-.65,.08,.42*s,-.14,.90,.5,.3,.42,0),
            (.62,.23,-.48,.15,.16*s,-.61,.67,1,1.1,0,0),
            (.73,.23,-.30,.14,.22*s,-.25,.54,1,1,0,0)]
        if side=='Right':
            keys[3]=(.46,.21,-.65,.08,.38,-.28,.77,.5,.3,.42,0)
    else:
        keys=[neutral,
            (.13,.24,.10,.17,.34*s,.06,.35,1,1,0,0),
            (.29,.09,.05,-.15,.30*s,.05,1.07,1,.8,0,0),
            (.39,.18,.15,-.08,.28*s,.09,.72,1,1,0,0),
            (.53,.28,-.20,.30,.17*s,-.60,.67,1,1.4,0,0),
            (.69,.27,-.16,.24,.19*s,-.56,.65,1,1.4,0,0)]
    keys += [(.88,.08,0,0,.28*s,-.10,.53,.6,.6,0,0),(1,*neutral[1:])]
    for a,b in zip(keys,keys[1:]):
        if t<=b[0]:
            w=max(0,min(1,(t-a[0])/(b[0]-a[0]))); w=w*w*(3-2*w)
            return [x+(y-x)*w for x,y in zip(a[1:],b[1:])]
    return list(neutral[1:])


def pose(name,t,idle):
    return sample(name,t,'Left',idle)


def right_hand(name,t,idle):
    return Vector(sample(name,t,'Right',idle)[3:6])


def foot(name,t,side,p):
    if name=='FireSweep' and side=='Left' and .20<t<.61:
        phase=(t-.20)/.41
        reach=math.sin(math.pi*phase)
        p.x += .40*math.cos(math.pi*phase)*reach
        p.y -= .48*reach
    return p


if __name__=='__main__':
    bake_casts(pose, (('FirePunch',151),('FireSweep',151),('FireBlast',151)),
               'fire', 'kael-v1-water-grounded.blend', False, right_hand, foot)
