"""Rebuild streets once from a connected plan, never from object bounds."""
from pathlib import Path
import json
import sys
import bpy
from mathutils import Matrix

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'Source'))
from paving_grid import geometry, tiles
from paving_plan import plan, subtract, CANALS, BRIDGES, COMPARISON_MOAT, COMPARISON_BRIDGE, ROYAL_MOAT, ROYAL_BRIDGE


def replace_surface(obj, rectangles, height, grout=False):
    if grout:
        vertices, faces, tones = [], [], []
        for (a,b,c,d), tone in tiles(rectangles, gap=0):
            n=len(vertices)
            vertices.extend([(a,b,height),(c,b,height),(c,d,height),(a,d,height)])
            faces.append((n,n+1,n+2,n+3)); tones.append(1)
    else:
        vertices, faces, tones = geometry(rectangles, height)
    mesh = bpy.data.meshes.new(obj.name+' Connected')
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(bpy.data.materials['Town Pavinglight'])
    mesh.materials.append(bpy.data.materials['Town Paving'])
    for face, tone in zip(mesh.polygons, tones): face.material_index = tone
    mesh.update()
    old = obj.data
    obj.data = mesh; obj.parent = None; obj.matrix_world = Matrix.Identity(4)
    obj['neris_paving_surface'] = True
    if old.users == 0: bpy.data.meshes.remove(old)


def apply(layout=None):
    if layout is None:
        layout=json.loads((ROOT/'expansion-layout.json').read_text())
    roads=plan(layout)
    footprint=subtract(roads, CANALS+BRIDGES)
    if 'comparisonCastle' in layout:
        footprint=subtract(footprint,COMPARISON_MOAT)+[COMPARISON_BRIDGE]
    if 'royalRebuild' in layout:
        footprint=subtract(footprint,ROYAL_MOAT+[ROYAL_BRIDGE])
    bpy.context.view_layer.update()
    # Remove obsolete slabs and tops, including their coplanar overlaps.
    prefixes=('Central Promenade','Civic Forecourt','District Walk','Cross Street',
              'Building Courtyard','North Home Court','South Home Court',
              'Neighborhood Market Court','Canal Footbridge','Unified Expanded',
              'Connected Town','Connected Canal','Connected Royal','Royal Bridge Deck Aligned Tiles')
    for obj in list(bpy.data.objects):
        if obj.type=='MESH' and obj.name.startswith(prefixes):
            bpy.data.objects.remove(obj,do_unlink=True)
    collection=bpy.data.collections.get('05 Expanded District Streets') or bpy.context.scene.collection
    for name,regions,height,grout in [
        ('Connected Town Grout',footprint,.19,True),
        ('Connected Town Tiles',footprint,.212,False),
        ('Connected Canal Bridge Grout',BRIDGES,.29,True),
        ('Connected Canal Bridge Tiles',BRIDGES,.312,False),
        ('Connected Royal Bridge Grout',[ROYAL_BRIDGE],.19,True),
        ('Connected Royal Bridge Tiles',[ROYAL_BRIDGE],.212,False)]:
        obj=bpy.data.objects.new(name,bpy.data.meshes.new(name));collection.objects.link(obj)
        replace_surface(obj,regions,height,grout)
    # Support tops stay below grout, never coplanar with its visible grid lines.
    for name in ['Royal Bridge Deck','Comparison Bridge Deck']:
        base=bpy.data.objects[name]
        top=max((base.matrix_world@v.co).z for v in base.data.vertices)
        move=Matrix.Translation((0,0,.15-top))
        base.matrix_world=move@base.matrix_world
    for name in ['Military Precinct']:
        base=bpy.data.objects[name]
        points=[base.matrix_world@v.co for v in base.data.vertices]
        bounds=[min(p.x for p in points),min(p.y for p in points),max(p.x for p in points),max(p.y for p in points)]
        label=name+' Aligned Tiles'
        obj=bpy.data.objects.get(label)
        if obj is None:
            obj=bpy.data.objects.new(label,bpy.data.meshes.new(label));collection.objects.link(obj)
        replace_surface(obj,[bounds],max(p.z for p in points)+.012)
    layout['roads']=[[(a+c)/2,(b+d)/2,c-a,d-b] for a,b,c,d in roads]
    layout['paving']=[[(a+c)/2,(b+d)/2,c-a,d-b] for a,b,c,d in footprint]
    layout['pavingPolicy']='Connected district loops; civic hub; one grid; canal exclusions; raised bridges'
    (ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
    bpy.context.scene['Neris Paving Grid']='Unified 2m grid, connected road union, explicit canal openings'
    bpy.context.view_layer.update()
    print('PAVING connected streets, three neighborhood loops and preserved canals',flush=True)


if __name__ == '__main__':
    assert bpy.app.background and Path(bpy.data.filepath).name == 'Neris-Town-Expanded.blend'
    apply()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Expanded.blend'),compress=True)
