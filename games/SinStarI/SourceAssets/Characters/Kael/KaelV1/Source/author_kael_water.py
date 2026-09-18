"""Original water poses: circular gathering, torso-led whip and two-palm wave release.

Visual references only: Jared Koh 5Lq0vTq6eeo; Krifton QjWQXMB5xNs;
A Bit Of Game Dev 3CcWus6d_B8 at 16:13. No downloaded animation or water assets.
"""
import sys
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from author_kael_earth import bake_casts


def sample(name, t, side, idle):
    # Body faces -Y. Hand paths circle on both sides before a forward release.
    neutral=(0,0,0,0,*idle,0,0,0,0)
    sign=1 if side=='Left' else -1
    reach=(.13*sign,-.58,.66)
    if name=='WaterWhip':
        keys=[neutral,
            (.10,.10,.20,-.08,.40*sign,.08,.42,.8,.5,0,0),
            (.24,.14,.48,-.08,.43*sign,.14,.73,1,.8,0,0),
            (.37,.08,-.15,-.12,.35*sign,-.05,.94,1,.8,0,0),
            (.49,.17,-.52,.18,*reach,1,1.25,0,0),
            (.69,.16,-.38,.13,.18*sign,-.48,.61,1,1.1,0,0)]
        if side=='Right':
            keys[2]=(.24,.14,.48,-.08,-.18,-.38,.50,1,.8,0,0)
            keys[3]=(.37,.08,-.15,-.12,-.31,.03,1.03,1,.8,0,0)
            keys[4]=(.49,.17,-.52,.18,-.38,.12,.61,1,1.25,0,0)
    elif name=='WaterOrbit':
        keys=[neutral,
            (.10,.10,-.26,.05,.44*sign,-.08,.42,1,.6,0,0),
            (.23,.18,-.60,.10,.29*sign,-.34,.64,1,1,0,0),
            (.36,.10,.55,-.13,.30*sign,.04,1.06,1,1,0,0),
            (.50,.21,-.50,.22,*reach,1,1.3,0,0),
            (.69,.18,-.33,.16,.22*sign,-.45,.66,1,1.1,0,0)]
    else:
        keys=[neutral,
            (.11,.21,.12,.20,.34*sign,-.19,.28,1,1,0,0),
            (.25,.14,.26,-.10,.42*sign,.04,.69,1,1,0,0),
            (.38,.07,.06,-.16,.27*sign,-.06,.99,1,.8,0,0),
            (.52,.24,-.18,.27,.18*sign,-.57,.61,1,1.35,0,0),
            (.69,.21,-.13,.19,.19*sign,-.46,.56,1,1.1,0,0)]
    keys += [(.86,.08,0,0,.28*sign,-.10,.52,.6,.6,0,0),(1,*neutral[1:])]
    for a,b in zip(keys,keys[1:]):
        if t<=b[0]:
            w=max(0,min(1,(t-a[0])/(b[0]-a[0]))); w=w*w*(3-2*w)
            return [x+(y-x)*w for x,y in zip(a[1:],b[1:])]
    return list(neutral[1:])


def pose(name,t,idle):
    return sample(name,t,'Left',idle)


def right_hand(name,t,idle):
    return Vector(sample(name,t,'Right',idle)[3:6])


if __name__=='__main__':
    bake_casts(pose, (('WaterWhip',151),('WaterOrbit',151),('WaterSurge',151)),
               'water', 'kael-v1-earth-grounded.blend', False, right_hand)
