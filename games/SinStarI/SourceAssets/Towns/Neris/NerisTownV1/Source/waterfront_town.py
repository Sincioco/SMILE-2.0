"""Reorganize the saved Blender town first, reusing its authored architecture.

The native export is a separate subsequent step. No live Blender session is saved.
"""
from pathlib import Path
import json
import math
import sys
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(ROOT/'Source'),str(ROOT.parent/'NerisBuildingsV1/Source')]
from expansion_architecture import Architecture
from waterfront_plan import data, CASTLE, TRIPO, MILITARY, HALL, TOWER, TOWER_SCALE, HALL_SCALE, LEGACY_HOMES
from paving_plan import subtract, rect
from align_paving import replace_surface


def move(name, xy, yaw=None):
    obj = bpy.data.objects[name]
    obj.location.x, obj.location.y = xy
    if yaw is not None: obj.rotation_euler.z = math.radians(yaw)
    return obj


def build():
    assert bpy.app.background
    assert not bpy.context.scene.get('neris_waterfront_revision'), 'Already rearranged; reopen prior source to regenerate.'
    layout = data()
    for obj in bpy.data.objects:
        if obj.type=='FONT' and obj.name.startswith('Neris Arrival Sign'):
            obj.data.body='Kingdom of Neris'
        if obj.type=='MESH' and obj.name.startswith('Entrance Threshold'):
            # Repair the older source's 0.4m overhang above the entrance steps.
            obj.data=obj.data.copy()
            front=min(v.co.y for v in obj.data.vertices)
            for vertex in obj.data.vertices:
                if abs(vertex.co.y-front)<.001:vertex.co.y+=.4
            obj.data.update()
    move('Royal Castle of Neris',CASTLE,0)
    move('Neris Tripo Castle Comparison',TRIPO,0)
    headquarters=move('Neris Military Headquarters',MILITARY,0)
    headquarters.scale=(2,2,2)
    hall=move('Neris-City-Hall Editable',HALL,0)
    hall.scale=tuple(value*HALL_SCALE for value in hall.scale)
    tower=move('Neris-Communication-Tower Editable',TOWER,0)
    tower.scale=tuple(value*TOWER_SCALE for value in tower.scale)
    for label,y in [('Weapon',-156),('Item',-208),('Armor',-260)]:
        move(f'Neris-{label}-Store Editable',(125,y),-90)
    for style in ('Large','Medium','Small'):
        items = sorted([o for o in bpy.data.objects if o.name.startswith(style+' Residence ')],key=lambda o:o.name)
        sites = [h for h in layout['homes'] if h['style']==style]
        while len(items)<len(sites):
            extra=items[0].copy()
            extra.name=style+' Residence Waterfront '+str(len(items)+1)
            items[0].users_collection[0].objects.link(extra)
            items.append(extra)
        assert len(items)==len(sites)
        for obj,site in zip(items,sites):move(obj.name,(site['x'],site['y']),site['yaw'])
    # The first eight cottages remain part of the residential districts.
    legacy = LEGACY_HOMES
    for obj,site in zip(sorted([o for o in bpy.data.objects if o.name.startswith('Home ')],key=lambda o:o.name),legacy):
        move(obj.name,site[:2],site[2])
    # Keep the accepted detailed fountain, arrival decoration and street furniture.
    move('03 Plaza and Street Furniture',(0,-160))
    move('04 Trees and Garden Beds',(0,-160))
    # Move the complete original welcome arch, including its lettering, pillars,
    # banners and crystals, just north of the southern crossing (Sin's yellow mark).
    bpy.context.view_layer.update()
    for obj in list(bpy.data.objects['03 Plaza and Street Furniture'].children):
        if obj.type not in {'MESH','CURVE','FONT'}:continue
        points=[obj.matrix_basis @ Vector(p) for p in obj.bound_box]
        # Remove the isolated original crystal circle west of the park avenue,
        # including its rays, pylons and crystals. Other garden circles remain.
        if (points and min(p.x for p in points)>=-27 and max(p.x for p in points)<=-17
                and min(p.y for p in points)>=-27 and max(p.y for p in points)<=-17):
            bpy.data.objects.remove(obj,do_unlink=True)
            continue
        if points and min(p.y for p in points)>=-40.7 and max(p.y for p in points)<=-37.3:
            obj.location.y+=layout['arrivalSign'][1]+199
        if (points and min(p.x for p in points)>=-5.1 and max(p.x for p in points)<=5.1
                and min(p.y for p in points)>=-25.1 and max(p.y for p in points)<=-14.9):
            obj.location.y+=68  # South of City Hall's stairs, with a generous approach.
    # Discard only the obsolete terrain, roads and canal fixtures. Buildings,
    # their planters, all detailed tree templates and street furniture survive.
    prefixes=('Royal Site ','Town Bedrock','Town Garden','Comparison Terrace','Comparison Moat',
        'Comparison Bridge','Castle Moat','Moat Outer','Moat Bank','Outer Town Boundary',
        'Comparison Boundary','Connected ','Unified Expanded','Military Precinct Aligned',
        'Canal Lining','Canal Water','Canal Stone','Canal Footbridge','Bridge Handrail','Bridge Baluster',
        'South Boundary Wall','Neris Lawn Blade Batch','Residential Garden Curb','Clipped Garden Hedge')
    for obj in list(bpy.data.objects):
        if not obj.library and obj.name.startswith(prefixes):bpy.data.objects.remove(obj,do_unlink=True)
    # These detached planters belonged to the previous street layout. Their
    # replacement flower beds are placed and checked with the new gardens.
    old_site=bpy.data.objects.get('05 Expanded District Streets')
    if old_site:
        for obj in list(old_site.children):
            if obj.name.startswith(('Garden Planter','Planter Rim','Broad Garden Leaves','Ivory Garden Blossom')):
                bpy.data.objects.remove(obj,do_unlink=True)
    mats={key:bpy.data.materials[name] for key,name in {
        'stone':'Ivory Limestone','trim':'Pale Carved Stone','gold':'Aged Gold','iron':'Patinated Iron',
        'paving':'Town Paving','pavinglight':'Town Pavinglight','grass':'Town Grass',
        'foundation':'Town Foundation','water':'Royal Deep Blue Water','bed':'Royal Deep Blue Moat Bed'}.items()}
    deep=mats['water'].node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value[:]
    for material in bpy.data.materials:
        if 'Water' in material.name and 'Bed' not in material.name and material.use_nodes:
            shader=material.node_tree.nodes.get('Principled BSDF')
            if shader:
                shader.inputs['Base Color'].default_value=deep
                material.diffuse_color=deep
    # Plain, gently polished slate: no albedo/normal waves in Blender or native.
    for key in ('paving','pavinglight'):
        mat=mats[key];mat['neris_stone_texture']=False
        nodes,links=mat.node_tree.nodes,mat.node_tree.links
        for node in list(nodes):
            if node.type not in {'BSDF_PRINCIPLED','OUTPUT_MATERIAL'}:nodes.remove(node)
        shader=nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value=mat.diffuse_color
        shader.inputs['Metallic'].default_value=0
        shader.inputs['Roughness'].default_value=.48
    site=Architecture('Waterfront Terrain and Bridges',mats)
    land=subtract(layout['land'],layout['waterRectangles'])
    for a,b,c,d in land:
        site.box('Waterfront Foundation',((a+c)/2,(b+d)/2,-1.1),(c-a,d-b,2),'foundation',0)
        site.box('Waterfront Lawn',((a+c)/2,(b+d)/2,-.16),(c-a,d-b,.3),'grass',0)
    for a,b,c,d in layout['waterRectangles']:
        site.box('Waterfront Blue Bed',((a+c)/2,(b+d)/2,-.65),(c-a,d-b,.2),'bed',0)
        surface=site.box('Waterfront Water',((a+c)/2,(b+d)/2,.085),(c-a,d-b,.025),'water',0)
        surface['neris_native_water']=True
    roads=subtract(layout['roadRectangles'],layout['waterRectangles'])
    # Bridges cross only water; all streets and crossings share the same grid.
    paved=roads+layout['bridges']
    for name,height,grout in [('Waterfront Grout',.19,True),('Waterfront Slate Tiles',.212,False)]:
        obj=site.mesh(name,[],[],'paving')
        replace_surface(obj,paved,height,grout)
    for a,b,c,d in layout['bridges']:
        site.box('Waterfront Bridge Deck',((a+c)/2,(b+d)/2,.015),(c-a,d-b,.27),'stone',0)
        # Rails run along the crossing, leaving both ends open.
        rail_a,rail_b,rail_c,rail_d=a,b,c,d
        if d<0:
            # Civic bridges keep their broad landings; rails stop at the banks.
            wet=[(max(a,x0),max(b,y0),min(c,x1),min(d,y1))
                 for x0,y0,x1,y1 in layout['waterRectangles']
                 if min(c,x1)>max(a,x0) and min(d,y1)>max(b,y0)]
            assert wet, 'Civic bridge must cross water'
            rail_a=min(r[0] for r in wet);rail_b=min(r[1] for r in wet)
            rail_c=max(r[2] for r in wet);rail_d=max(r[3] for r in wet)
        if d-b>c-a:
            for x in (a+.3,c-.3):
                site.box('Waterfront Bridge Parapet',(x,(rail_b+rail_d)/2,.75),(.45,rail_d-rail_b,1.05),'trim',0)
                site.box('Waterfront Bridge Bronze',(x,(rail_b+rail_d)/2,1.3),(.48,rail_d-rail_b,.07),'gold',0)
        else:
            for y in (b+.3,d-.3):
                site.box('Waterfront Bridge Parapet',((rail_a+rail_c)/2,y,.75),(rail_c-rail_a,.45,1.05),'trim',0)
                site.box('Waterfront Bridge Bronze',((rail_a+rail_c)/2,y,1.3),(rail_c-rail_a,.48,.07),'gold',0)
    # Quay edging follows actual land/water boundaries. Shared internal rectangle
    # edges never become pale lines across the canals or moats.
    shoreline(site,land,layout['bridges'])
    layout['paving']=[[(a+c)/2,(b+d)/2,c-a,d-b] for a,b,c,d in paved]
    layout['roads']=layout['paving']
    (ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
    from waterfront_landscape import arrange
    arrange(layout)
    (ROOT/'expansion-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
    from detail_grass import apply
    apply()
    for name,center,height in [('Royal Castle Spotlight',CASTLE,180),('Tripo Castle Spotlight',TRIPO,150),
                              ('Military Headquarters Spotlight',MILITARY,100)]:
        lamp=bpy.data.objects.get(name)
        if lamp:
            lamp.location=(center[0]-35,center[1]-65,height)
            lamp.rotation_euler=(Vector((*center,35))-lamp.location).to_track_quat('-Z','Y').to_euler()
    scene=bpy.context.scene
    for name,pos,target,ortho in [('Waterfront Overview',(450,-820,660),(-95,5,15),0),
                                  ('Waterfront Plan',(-115,0,900),(-115,0,0),1060),
                                  ('Waterfront Civic',(90,-270,110),(0,-80,8),0),
                                  ('Waterfront Arrival',(-2,-300,2.812),(-8.5,-254,4.412),0)]:
        obj=bpy.data.objects.new(name,bpy.data.cameras.new(name));scene.collection.objects.link(obj)
        obj.location=pos;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
        obj.data.clip_end=3000;obj.data.lens=45
        if ortho:obj.data.type='ORTHO';obj.data.ortho_scale=ortho
    scene['neris_waterfront_revision']=layout['revision']
    scene.camera=bpy.data.objects['Waterfront Overview']
    scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                area.spaces.active.clip_end=3000
                area.spaces.active.region_3d.view_perspective='CAMERA'
                area.spaces.active.shading.type='MATERIAL'
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blend/Neris-Town-Waterfront.blend'),compress=True)
    print('WATERFRONT SAVED — Blender is the authoritative arrangement',flush=True)
    for name in ('Waterfront Plan','Waterfront Overview','Waterfront Civic','Waterfront Arrival'):
        scene.camera=bpy.data.objects[name];scene.render.filepath=str(ROOT/'Previews'/(name.replace(' ','-')+'.png'))
        bpy.ops.render.render(write_still=True)


def shoreline(g,land,bridges):
    xs=sorted({x for r in land for x in (r[0],r[2])})
    ys=sorted({y for r in land for y in (r[1],r[3])})
    cells={(i,j) for i in range(len(xs)-1) for j in range(len(ys)-1)
           if any(a<=(xs[i]+xs[i+1])/2<=c and b<=(ys[j]+ys[j+1])/2<=d for a,b,c,d in land)}
    for i,j in cells:
        x0,x1=xs[i:i+2];y0,y1=ys[j:j+2]
        for neighbor,r in [((i-1,j),(x0-.16,y0,x0+.16,y1)),((i+1,j),(x1-.16,y0,x1+.16,y1)),
                           ((i,j-1),(x0,y0-.16,x1,y0+.16)),((i,j+1),(x0,y1-.16,x1,y1+.16))]:
            if neighbor in cells:continue
            for a,b,c,d in subtract([r],bridges):
                g.box('Waterfront Quay Edge',((a+c)/2,(b+d)/2,.14),(c-a,d-b,.28),'trim',0)


if __name__=='__main__':build()
