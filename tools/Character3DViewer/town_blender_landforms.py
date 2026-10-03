"""Reproduce the native landform templates in an exported Blender town."""
import sys
import math
from pathlib import Path
import bpy
from mathutils import Matrix


def member_names(template):
    if template == 39:
        return ['Wilderness Campfire %d' % band for band in range(4)]
    return ['Luma Landform %d-%d' % (template - 35, band) for band in range(3)]


def populate(item, matrix, output):
    root=Path(__file__).resolve().parents[2]/'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
    sys.path.insert(0,str(root/'Source'))
    from journey_landforms import geometry
    form=item['template']-35
    campfire=item['template']==39
    if campfire:
        from wilderness_campfire import geometry as fire_geometry, COLORS
        groups=fire_geometry()
    else:
        groups,_,_=geometry(form)
    colors=((.15,.18,.20),(.40,.22,.10),(.54,.34,.13),(.18,.22,.23))
    anchor=bpy.data.objects.new('Town Assembly %d'%item['identity'],None)
    output.objects.link(anchor)
    anchor.matrix_world=matrix
    anchor['town_assembly']=item['identity']
    anchor['town_template']=item['template']
    for band,triangles in enumerate(groups):
        key=member_names(item['template'])[band]
        mesh=bpy.data.meshes.get(key)
        if mesh is None:
            vertices=[(x,-z,y) for points,_ in triangles for x,y,z in points]
            mesh=bpy.data.meshes.new(key)
            mesh.from_pydata(vertices,[],[(i,i+1,i+2) for i in range(0,len(vertices),3)])
            mat=bpy.data.materials.new(key)
            mat.use_nodes=True
            node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            rgb=COLORS[band] if campfire else tuple(min(.95,c*(.88+.16*band)) for c in colors[form])
            node.inputs['Base Color'].default_value=tuple(rgb)+(1,)
            if campfire and band>=2:
                node.inputs['Emission Color'].default_value=tuple(rgb)+(1,)
                node.inputs['Emission Strength'].default_value=1
            node.inputs['Roughness'].default_value=.96
            mesh.materials.append(mat)
        obj=bpy.data.objects.new(key,mesh)
        output.objects.link(obj)
        obj.parent=anchor
        obj.matrix_parent_inverse=Matrix.Identity(4)
        obj.matrix_basis=Matrix.Identity(4)
        obj['town_identity']=item['identity']
        obj['town_member']=key
        obj['town_local_matrix']=[n for row in Matrix.Identity(4) for n in row]
    if campfire:
        light=bpy.data.lights.new('Campfire Warm Light','POINT')
        light.color=(1,.36,.08)
        light.energy=180
        light.shadow_soft_size=.6
        obj=bpy.data.objects.new('Campfire Warm Light',light)
        output.objects.link(obj)
        obj.parent=anchor
        obj.location=(0,0,1.2)


def terrain_style(document, mesh):
    # Reserve explicit Meadow before replacing the map-wide default slots.
    defaults = (mesh.materials[0], mesh.materials[2])
    mesh.materials.append(defaults[0])
    mesh.materials.append(defaults[1])
    for style, rgb in enumerate(((9,26,6),(45,52,41),(127,83,34),(200,215,230)), 1):
        for road in (False, True):
            mat=bpy.data.materials.new('Town Terrain Style %d-%d'%(style,road))
            mat.use_nodes=True
            node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            values=tuple((c*f//100 if road else c)/255 for c,f in zip(rgb,(72,64,57)))
            if road and style == 1:
                values=tuple(c/255 for c in (33,22,10))
            if road and style == 4:
                values=tuple(c/255 for c in (92,104,108))
            if not road and style in (1,2,4):
                values=tuple(c/255 for c in ((200,215,230) if style == 4 else (110,115,102)))
                names={1:'Green-Slope-Tile.png',2:'Greyglass-Strata.png',4:'Snow-Slope-Tile.png'}
                folder=Path(__file__).resolve().parents[2]/'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Textures'
                image=bpy.data.images.load(str(folder/names[style]),check_existing=True)
                image.pack()
                texture=mat.node_tree.nodes.new('ShaderNodeTexImage')
                texture.image=image
                multiply=mat.node_tree.nodes.new('ShaderNodeMixRGB')
                multiply.blend_type='MULTIPLY'
                multiply.inputs[0].default_value=1
                multiply.inputs[2].default_value=values+(1,)
                mat.node_tree.links.new(texture.outputs['Color'],multiply.inputs[1])
                mat.node_tree.links.new(multiply.outputs[0],node.inputs['Base Color'])
            node.inputs['Base Color'].default_value=values+(1,)
            node.inputs['Roughness'].default_value=1
            mesh.materials.append(mat)
    style=document.get('terrain_style',0)
    if style:
        mesh.materials[0]=mesh.materials[6+style*2]
        mesh.materials[2]=mesh.materials[7+style*2]
    terrain_coordinates(document,mesh)


def terrain_coordinates(document, mesh):
    """Match TownTerrainStyles and its sampled slope without changing mesh/collision."""
    source=Path(__file__).resolve().parents[2]/'games/SinStarI/SourceAssets/Towns/Neris/StoryTownsV1/Source'
    sys.path.insert(0,str(source))
    from town_design import terrain_offset
    uv=mesh.uv_layers.active.data
    normal_cache={}
    for poly in mesh.polygons:
        slot=poly.material_index
        style=document.get('terrain_style',0) if slot == 0 else (slot-6)//2 if slot >= 6 and slot % 2 == 0 else 0
        if style not in (1,2,4):
            continue
        poly.use_smooth=True
        for loop in poly.loop_indices:
            p=mesh.vertices[mesh.loops[loop].vertex_index].co
            x,y,z=p.x*10,p.z*10+21,p.y*10
            key=(x,z)
            if key not in normal_cache:
                normal_y=1
                if document.get('heights'):
                    step=document['cell_size']
                    low_x,high_x=max(document['xs'][0],x-step),min(document['xs'][-1],x+step)
                    low_z,high_z=max(document['zs'][0],z-step),min(document['zs'][-1],z+step)
                    gx=(terrain_offset(document,high_x,z)-terrain_offset(document,low_x,z))/max(.001,high_x-low_x)
                    gz=(terrain_offset(document,x,high_z)-terrain_offset(document,x,low_z))/max(.001,high_z-low_z)
                    normal_y=1/math.sqrt(gx*gx+1+gz*gz)
                normal_cache[key]=normal_y
            threshold, strength = (.006,8) if style == 2 else (.045,3)
            exposure=max(0,min(1,(1-normal_cache[key]-threshold)*strength))
            if style == 2:
                exposure=exposure*exposure*(3-2*exposure)
            repeat=(z+y*.65+60*math.sin(x/570))/(180 if style == 2 else 160)
            repeat=abs(2*(repeat-math.floor(repeat))-1)
            uv[loop].uv=((x+55*math.sin(z/430))/300,1-(.04+exposure*.64+repeat*.27))
