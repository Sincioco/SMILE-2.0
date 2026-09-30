"""Royal Castle entrance and botanical detailing, shared by native and Blender export.

Operate on a freshly opened immutable catalog. Keep the castle root, footprint,
entrance marker and walkable geometry; replace only decorative planting and leaves.
"""
import math
import random
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT.parent / 'NerisBuildingsV1/Source'))
from geometry import Geometry, material

# Remove exterior masonry lines entirely; retain structural carved cornices.
FINE_MASONRY = ('Fortress Mortar Line', 'Fortress Masonry Joint',
                'Side Mortar Course', 'Limestone Course', 'Tower Stone Course',
                'Royal Broad Masonry', 'Royal Broad Palace', 'Royal Broad Tower')


def palette():
    return {
        'stone': bpy.data.materials['Pale Carved Stone'],
        'gold': bpy.data.materials['Polished Gold Edge'],
        'teal': bpy.data.materials['Teal Enamel Roof'],
        'wood': bpy.data.materials['Dark Walnut'],
        'leaf': material('Royal Garden Deep Leaves', (.055, .16, .035), roughness=.84),
        'fresh': material('Royal Garden New Leaves', (.18, .29, .08), roughness=.76),
        'flower': material('Royal Garden Ivory Petals', (.89, .79, .63), roughness=.58),
        'rose': material('Royal Garden Rose Petals', (.49, .075, .17), roughness=.58),
    }


def foliage(g, center, radius, height, seed):
    """A branched evergreen with individual folded sprays instead of a solid cone."""
    rng = random.Random(seed)
    x, y, z = center
    g.beam('Royal Cypress Bark', (x, y, z), (x, y, z + height*.91), radius*.09, 'wood', 8)
    vertices = [[], []]
    faces = [[], []]
    for branch in range(38):
        fraction = .12 + branch / 43
        angle = branch * 2.39996 + rng.uniform(-.25, .25)
        reach = radius * (1 - fraction)**.6 * rng.uniform(.85, 1.13)
        origin = Vector((x, y, z + height*fraction))
        tip = origin + Vector((math.cos(angle)*reach, math.sin(angle)*reach, height*.085))
        g.beam('Royal Cypress Twig', origin, tip, radius*.014, 'wood', 5)
        for leaf in range(18):
            t = .2 + leaf / 22
            p = origin.lerp(tip, t)
            a = angle + (1 if leaf % 2 else -1)*rng.uniform(.65, 1.45)
            length = radius * rng.uniform(.42, .68)
            along = Vector((math.cos(a)*length, math.sin(a)*length, length*.3))
            across = Vector((-math.sin(a), math.cos(a), 0))*length*.40
            mid = p + along*.55
            color = int(rng.random() > .69)
            v, f = vertices[color], faces[color]
            n = len(v)
            v.extend((p, mid+across, p+along, mid-across, mid+Vector((0, 0, length*.12))))
            f.extend(((n, n+1, n+4), (n+1, n+2, n+4), (n+2, n+3, n+4), (n+3, n, n+4)))
    for index, mat in enumerate(('leaf', 'fresh')):
        g.mesh('Royal Cypress Leaf Sprays', vertices[index], faces[index], mat)


def planter(g, center, radius, seed):
    rng = random.Random(seed)
    x, y, z = center
    g.lathe('Carved Royal Garden Urn', [(radius*.58, 0), (radius*.62, .10),
        (radius*.48, .18), (radius*.67, .31), (radius*.96, .60),
        (radius, .70), (radius*.86, .73), (radius*.83, .60)], center, 'stone', 24)
    for zz, rr in ((.12, .62), (.67, 1.0)):
        g.lathe('Gilded Urn Rim', [(radius*rr, zz), (radius*rr+.025, zz+.025),
            (radius*rr, zz+.05)], center, 'gold', 24)
    verts, faces = [], []
    petals = [([], []), ([], [])]
    for index in range(28):
        angle = index*2.39996
        reach = radius*.8*math.sqrt((index+.5)/28)
        p = Vector((x+math.cos(angle)*reach, y+math.sin(angle)*reach, z+.76+rng.uniform(0,.24)))
        for side in (-1, 1):
            a = angle + side*.8
            q = p + Vector((math.cos(a)*radius*.27, math.sin(a)*radius*.27, -.09))
            cross = Vector((-math.sin(a), math.cos(a), 0))*radius*.11
            n = len(verts)
            verts.extend((p, (p+q)*.5+cross, q, (p+q)*.5-cross))
            faces.append((n,n+1,n+2,n+3))
        v, f = petals[index % 2]
        for petal in range(7):
            a = petal*math.tau/7
            tip = p+Vector((math.cos(a)*radius*.17,math.sin(a)*radius*.17,.04))
            cross = Vector((-math.sin(a),math.cos(a),0))*radius*.07
            n = len(v)
            v.extend((p, (p+tip)*.5+cross+Vector((0,0,.045)),tip,(p+tip)*.5-cross+Vector((0,0,.045))))
            f.extend(((n,n+1,n+2),(n,n+2,n+3)))
    g.mesh('Royal Flower Leaves', verts, faces, 'leaf')
    for index, mat in enumerate(('flower','rose')):
        g.mesh('Royal Garden Petals', *petals[index], mat)


def door_leaves(g, marker):
    """Normalized split leaves retain the existing native hinge contract."""
    created = []
    for side in (-1, 1):
        before = set(g.collection.objects)
        outline = [(0,0),(side*.5,0),(side*.5,.64)]
        outline += [(side*(.5-.5*(i/16)**2),.64+.36*i/16) for i in range(1,17)]
        g.prism('Royal Door Teal Enamel', outline, .038, 'teal')
        g.path('Royal Door Gilded Arch', [(x,-.034,z) for x,z in outline], .009, 'gold', True)
        for low, high in ((.08,.30),(.37,.62)):
            points = [(side*.065,-.040,low),(side*.415,-.040,low),
                      (side*.415,-.040,high),(side*.065,-.040,high)]
            g.path('Raised Royal Door Panel', points, .008, 'gold', True)
            mid = (low+high)/2
            diamond = [(side*.24,-.052,mid-.067),(side*.29,-.052,mid),
                       (side*.24,-.052,mid+.067),(side*.19,-.052,mid)]
            g.path('Royal Door Rosette', diamond, .006, 'gold', True)
        g.path('Royal Door Gothic Tracery', [(side*.08,-.043,.67),
            (side*.13,-.043,.83),(side*.07,-.043,.91),(0,-.043,.98)], .006, 'gold')
        handle = [(side*.075+math.cos(i*math.tau/20)*.017,-.078,
                   .335+math.sin(i*math.tau/20)*.032) for i in range(20)]
        g.path('Royal Door Ring Handle', handle, .006, 'gold', True)
        for index in range(8):
            obj = g.box('Royal Door Bronze Stud', (side*.45,-.046,.06+index*.075),
                        (.013,.014,.013),'gold',0)
        for obj in set(g.collection.objects)-before:
            obj.parent = marker
            obj.matrix_basis = Matrix.Diagonal((marker['width'], .7, marker['height'], 1)) @ obj.matrix_basis
            obj['neris_door_leaf'] = True
            obj['neris_leaf_side'] = side
            created.append(obj)
    return created


def apply(catalog=None):
    castle = bpy.data.objects['Royal Castle of Neris']
    if castle.get('royal_detail_revision') == 3:
        return castle
    bpy.context.view_layer.update()
    inverse = castle.matrix_world.inverted()
    trees, pots, remove = [], [], []
    for obj in list(castle.children_recursive):
        points = [inverse @ obj.matrix_world @ Vector(p) for p in obj.bound_box]
        low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
        high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
        if obj.name.startswith(('Clipped Cypress','Royal Cypress Foliage')):
            trees.append(((low.x+high.x)/2,(low.y+high.y)/2,low.z,
                          max(high.x-low.x, high.y-low.y)/2,high.z-low.z))
        if obj.name.startswith('Garden Planter'):
            pots.append(((low.x+high.x)/2,(low.y+high.y)/2,low.z,
                         max(high.x-low.x,high.y-low.y)/2))
        if obj.name.startswith(('Clipped Cypress','Royal Cypress Foliage','Cypress Trunk',
            'Broad Garden Leaves','Ivory Garden Blossom','Garden Planter','Planter Rim') + FINE_MASONRY) or obj.get('neris_door_leaf'):
            remove.append(obj)
    for obj in remove:
        bpy.data.objects.remove(obj, do_unlink=True)
    g = Geometry('Royal Castle Detailed Garden', palette())
    for index, (x,y,z,radius,height) in enumerate(trees):
        foliage(g,(x,y,z),radius,height,1709+index)
    for index,(x,y,z,radius) in enumerate(pots):
        planter(g,(x,y,z),radius,1929+index)
    marker = next(o for o in castle.children if o.get('neris_entrance'))
    leaves = door_leaves(g, marker)
    # A layered carved/gilded archivolt and two fluted pilasters enrich the portal.
    for radius, yy in ((.08,.72),(.045,.62)):
        for side in (-1,1):
            points = [(side*3.29,yy,6.5),(side*3.29,yy,11.3)]
            points += [(side*(3.29-3.29*(i/24)**2),yy,11.3+2.8*i/24) for i in range(1,25)]
            g.path('Royal Portal Archivolt',points,radius,'gold')
    for side in (-1,1):
        x=side*3.72
        g.box('Royal Portal Pedestal',(x,.84,6.80),(.68,.75,.6),'stone')
        g.cylinder('Royal Portal Column',(x,.83,9.18),.23,4.2,'stone',20)
        for z in (7.1,11.22):
            g.lathe('Royal Portal Capital',[(.26,0),(.33,.08),(.34,.18),(.28,.24)],(x,.83,z),'gold',20)
        for n in range(10):
            angle=n*math.tau/10
            g.beam('Royal Portal Fluting',(x+math.cos(angle)*.232,.83+math.sin(angle)*.232,7.3),
                   (x+math.cos(angle)*.232,.83+math.sin(angle)*.232,11.08),.018,'gold',6)
    for obj in list(g.collection.objects):
        if obj == g.root or obj in leaves:
            continue
        obj.parent=castle
    bpy.data.objects.remove(g.root,do_unlink=True)
    castle['royal_detail_revision']=3
    bpy.context.view_layer.update()
    if catalog is not None:
        for instance in catalog['instances']:
            if instance['source']==castle.name:
                instance['members']=[castle.name]+[o.name for o in castle.children_recursive]
    print('ROYAL DETAIL',len(trees),'evergreens,',len(pots),'flower urns, gilded operable doors',flush=True)
    return castle
