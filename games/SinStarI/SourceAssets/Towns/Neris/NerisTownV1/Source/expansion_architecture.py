"""Editable ivory/teal architecture shared by the new homes and royal precinct."""
import math
from geometry import Geometry, face_point, on_face
from details import crystal, star, lantern, banner


class Architecture(Geometry):
    """Keep ornamental geometry economical enough for the existing native budget."""
    def bevel(self, obj, amount=.035, segments=1):
        return super().bevel(obj, amount, segments)

    def beam(self, name, start, end, radius, mat='gold', steps=6):
        return super().beam(name, start, end, radius, mat, steps)

    def path(self, name, points, radius=.04, mat='gold', cyclic=False):
        obj = super().path(name, points, radius, mat, cyclic)
        obj.data.bevel_resolution = 0
        return obj


def arch(g, side, plane, x, z, width, height, door=False, glass='glass'):
    shoulder = height * .65
    left = [(-width/2 + width/2*(i/8)**2,
             shoulder+(height-shoulder)*i/8) for i in range(9)]
    outline = [(-width/2, 0)] + left + [(-u, v) for u, v in reversed(left[:-1])] + [(width/2, 0)]
    on_face(g.prism('Pointed Portal' if door else 'Amber Gothic Window', outline, .10,
                    'iron' if door else glass), side, plane, x, z, .12)
    on_face(g.path('Ivory Arch Frame', [(u, 0, v) for u, v in outline], .10,
                   'trim', True), side, plane, x, z, .19)
    on_face(g.path('Gold Arch Fillet', [(u, 0, v) for u, v in outline], .026,
                   'gold', True), side, plane, x, z, .25)
    for u in [-width*.26, 0, width*.26]:
        g.beam('Window Mullion', face_point(side, plane, x+u, z+.10, .26),
               face_point(side, plane, x+u, z+height*(.88 if u==0 else .69), .26), .024)
    g.beam('Transom', face_point(side, plane, x-width*.43, z+height*.48, .27),
           face_point(side, plane, x+width*.43, z+height*.48, .27), .025)
    if door:
        star(g, side, plane, x, z+height*.49, width*.19, .30)


def block(g, name, x, y, w, d, h, base=0):
    g.box(name, (x, y, base+h/2), (w, d, h), 'stone', .06)
    for z in [base+.2, base+h*.49, base+h]:
        g.box('Ivory Belt Course', (x,y,z), (w+.26,d+.26,.22), 'trim', .02)
    g.box('Gold Cornice', (x,y,base+h+.14), (w+.32,d+.32,.055), 'gold', 0)
    # Fine horizontal masonry joints read at walking distance without thousands of blocks.
    for row in range(1, math.ceil(h/.9)):
        g.box('Limestone Course', (x,y,base+row*.9), (w+.10,d+.10,.03), 'trim', 0)


def dome(g, x, y, z, radius, rise):
    profile = [(radius*math.cos(i*math.pi/24), rise*math.sin(i*math.pi/24)) for i in range(13)]
    g.lathe('Teal Domed Roof', profile, (x,y,z), 'roof', 32)
    for j in range(12):
        a=j*math.tau/12
        g.path('Gold Dome Rib', [(x+(r+.025)*math.cos(a),y+(r+.025)*math.sin(a),z+h+.025)
                              for r,h in profile], .045)
    g.lathe('Dome Cornice', [(radius+.12,0),(radius+.12,.17)], (x,y,z), 'gold', 32)
    crystal(g, (x,y,z+rise+.15), max(.9,radius*.32), max(.15,radius*.075))


def hip_roof(g, x, y, z, width, depth, rise):
    w,d=width/2,depth/2
    verts=[(x-w,y-d,z),(x+w,y-d,z),(x+w,y+d,z),(x-w,y+d,z),
           (x-w*.32,y,z+rise),(x+w*.32,y,z+rise)]
    g.mesh('Teal Hipped Roof',verts,[(0,1,5,4),(1,2,5),(2,3,4,5),(3,0,4)],'roof')
    for a,b in [(0,1),(1,2),(2,3),(3,0),(0,4),(3,4),(1,5),(2,5),(4,5)]:
        g.beam('Gold Roof Edge',verts[a],verts[b],.045)
    for i in range(-5,6):
        u=i*w/6
        for s in [-1,1]:
            g.beam('Roof Standing Seam',(x+u,y+s*d,z+.025),
                   (x+max(-w*.32,min(w*.32,u)),y,z+rise+.025),.023)


def balcony(g, x, y, z, width, depth=1.1):
    g.box('Balcony Deck',(x,y-depth/2,z),(width,depth,.20),'trim',.04)
    g.beam('Balcony Rail',(x-width/2,y-depth,z+1.0),(x+width/2,y-depth,z+1.0),.065)
    for i in range(math.ceil(width/.45)+1):
        u=x-width/2+i*width/math.ceil(width/.45)
        g.beam('Balcony Baluster',(u,y-depth,z+.12),(u,y-depth,z+1.0),.028,'iron')
    for u in [x-width/2,x+width/2]:
        g.beam('Balcony Return',(u,y,z+1.0),(u,y-depth,z+1.0),.055)


def cypress(g,x,y,z,height=3):
    g.cylinder('Cypress Trunk',(x,y,z+height*.35),.09,height*.7,'wood',8)
    g.lathe('Clipped Cypress',[(.12,0),(.48,height*.15),(.43,height*.55),(.07,height)],
            (x,y,z+.15),'leaf',12)


def planter(g,x,y,z=0,width=1):
    g.box('Garden Planter',(x,y,z+.30),(width,width,.6),'stone',.05)
    g.box('Planter Rim',(x,y,z+.62),(width+.12,width+.12,.12),'trim',.02)
    for i in range(5):
        a=i*2.4
        xx=x+math.cos(a)*width*.24; yy=y+math.sin(a)*width*.24
        g.lathe('Broad Garden Leaves',[(.02,0),(.22,.18),(.20,.40),(0,.65)],
                (xx,yy,z+.65),'leaflight' if i%2 else 'leaf',6)
        g.lathe('Ivory Garden Blossom',[(.025,0),(.13,.055),(.07,.12)],
                (xx,yy,z+1.18),'trim',8)


def buttress(g,x,y,height):
    g.box('Corner Pilaster',(x,y,height/2),(.64,.64,height),'trim',.05)
    for z in [.2,height*.5,height]:
        g.box('Pilaster Capital',(x,y,z),(.94,.94,.22),'trim',.03)
    g.box('Capital Gold Band',(x,y,height+.15),(.98,.98,.06),'gold',0)
    crystal(g,(x,y,height+.25),1.1,.19)


def flag(g,x,y,z,height=13,width=4.6):
    g.cylinder('Flagpole',(x,y,z+height/2),.10,height,'gold',12)
    crystal(g,(x,y,z+height),1.1,.20)
    # Three shallow folds in a real cloth mesh, with the reference's four-point star.
    verts=[]
    for row in range(2):
        for i in range(13):
            u=i*width/12
            verts.append((x+u,y+.18*math.sin(u*2),z+height-.5-row*width*.60))
    g.mesh('Royal Neris Flag',verts,[(i,i+1,14+i,13+i) for i in range(12)],'cloth')
    for row in [0,1]:
        g.path('Flag Gold Border',[(a,b-.025,c) for a,b,c in verts[row*13:(row+1)*13]],.035)
    for i in [0,12]:
        g.beam('Flag Border',verts[i],verts[13+i],.04)
    star(g,0,-y,x+width*.50,z+height-.5-width*.30,width*.21,.30)
