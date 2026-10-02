"""Reproduce the native landform templates in an exported Blender town."""
import sys
from pathlib import Path
import bpy
from mathutils import Matrix


def member_names(template):
    return ['Luma Landform %d-%d' % (template - 35, band) for band in range(3)]


def populate(item, matrix, output):
    root=Path(__file__).resolve().parents[2]/'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1'
    sys.path.insert(0,str(root/'Source'))
    from journey_landforms import geometry
    form=item['template']-35
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
            node.inputs['Base Color'].default_value=tuple(min(.95,c*(.88+.16*band)) for c in colors[form])+(1,)
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


def terrain_style(document, mesh):
    # Reserve explicit Meadow before replacing the map-wide default slots.
    defaults = (mesh.materials[0], mesh.materials[2])
    mesh.materials.append(defaults[0])
    mesh.materials.append(defaults[1])
    for style, rgb in enumerate(((9,26,6),(45,52,41),(127,83,34)), 1):
        for road in (False, True):
            mat=bpy.data.materials.new('Town Terrain Style %d-%d'%(style,road))
            mat.use_nodes=True
            node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            values=tuple((c*f//100 if road else c)/255 for c,f in zip(rgb,(72,64,57)))
            if road and style == 1:
                values=tuple(c/255 for c in (33,22,10))
            node.inputs['Base Color'].default_value=values+(1,)
            node.inputs['Roughness'].default_value=1
            mesh.materials.append(mat)
    style=document.get('terrain_style',0)
    if style:
        mesh.materials[0]=mesh.materials[6+style*2]
        mesh.materials[2]=mesh.materials[7+style*2]
