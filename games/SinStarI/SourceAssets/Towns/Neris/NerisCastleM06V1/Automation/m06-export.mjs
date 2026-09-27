// Disposable evaluated candidate; original source collections remain editable.
export const build = String.raw`
import bpy,json,math
from mathutils import Vector, Matrix
s=bpy.context.scene
assert s.get('run_owner')=='neris-castle-2026-09-28-M01-r001'
assert not bpy.data.collections.get('NC.ExportCandidate.M06-r002')
candidate=bpy.data.collections.new('NC.ExportCandidate.M06-r002');s.collection.children.link(candidate)
dg=bpy.context.evaluated_depsgraph_get();groups={};dropped=0;uv_fallbacks=0
pivot_src=bpy.data.objects['NC.Bridge.Pivot'];pivot_src.rotation_euler.x=0
bpy.context.view_layer.update()
for owner in ['Site','Fortifications','Gatehouse','Bridge','Palace','Courtyard']:
    for o in bpy.data.collections['NC.'+owner].objects:
        if o.type not in ['MESH','CURVE']:continue
        e=o.evaluated_get(dg);m=e.to_mesh();m.calc_loop_triangles()
        matrix=e.matrix_world
        if owner=='Bridge':matrix=pivot_src.matrix_world.inverted()@matrix
        normal_matrix=matrix.to_3x3().inverted().transposed()
        uv=m.uv_layers.active
        for tri in m.loop_triangles:
            coords=[matrix@m.vertices[v].co for v in tri.vertices]
            cross=(coords[1]-coords[0]).cross(coords[2]-coords[0])
            if cross.length_squared<1e-14:dropped+=1;continue
            mat=m.materials[tri.material_index] if tri.material_index<len(m.materials) else None
            if not mat:raise RuntimeError('Missing material: '+o.name)
            key=(owner,mat.name);g=groups.setdefault(key,{'verts':[],'faces':[],'normals':[],'uv':[]})
            uvs=[tuple(uv.data[li].uv) for li in tri.loops] if uv else [(0,0)]*3
            area=(uvs[1][0]-uvs[0][0])*(uvs[2][1]-uvs[0][1])-(uvs[2][0]-uvs[0][0])*(uvs[1][1]-uvs[0][1])
            if abs(area)<1e-12:
                axis=max(range(3),key=lambda i:abs(cross[i]))
                axes=[i for i in range(3) if i!=axis]
                uvs=[(co[axes[0]]*.1,co[axes[1]]*.1) for co in coords];uv_fallbacks+=1
            n=len(g['verts']);g['verts'].extend(coords);g['faces'].append((n,n+1,n+2));g['uv'].extend(uvs)
            g['normals'].extend([(normal_matrix@m.corner_normals[li].vector).normalized() for li in tri.loops])
        e.to_mesh_clear()
pivot=bpy.data.objects.new('NC.Export.Bridge.Pivot',None);candidate.objects.link(pivot);pivot.location=pivot_src.location
# Native cooker accepts at most 65,535 vertices per primitive. Split at complete
# triangles without changing corner normals, UVs, materials, or source geometry.
chunks=[]
for (owner,mat),g in sorted(groups.items()):
    count=(len(g['verts'])+59999)//60000
    for part,start in enumerate(range(0,len(g['verts']),60000)):
        end=min(start+60000,len(g['verts']))
        chunk={key:g[key][start:end] for key in ['verts','normals','uv']}
        chunk['faces']=[(i,i+1,i+2) for i in range(0,end-start,3)]
        name='NC.Export.'+owner+'.'+mat.replace('NC.MAT.','')
        if count>1:name+='.%02d'%part
        chunks.append((owner,mat,name,chunk))
objects=[]
for owner,mat,name,g in chunks:
    d=bpy.data.meshes.new(name);d.from_pydata(g['verts'],[],g['faces']);d.update()
    d.materials.append(bpy.data.materials[mat])
    for p in d.polygons:p.use_smooth=True
    d.normals_split_custom_set(g['normals'])
    uv=d.uv_layers.new(name='UVMap')
    for i,co in enumerate(g['uv']):uv.data[i].uv=co
    o=bpy.data.objects.new(name,d);candidate.objects.link(o);o['owner']=owner.lower();o['stage']='M06 candidate'
    if owner=='Bridge':o.parent=pivot
    objects.append(o)
def export(filename,chosen):
    bpy.ops.object.select_all(action='DESELECT')
    for o in chosen:o.select_set(True)
    bpy.context.view_layer.objects.active=chosen[0]
    bpy.ops.export_scene.gltf(filepath='D:/Projects/Sin-Star-I-Assets/Neris-Castle/exports/M06-r002/'+filename,
        export_format='GLB',use_selection=True,export_yup=True,export_apply=False,
        export_materials='EXPORT',export_texcoords=True,export_normals=True,
        export_animations=False,export_extras=False,export_cameras=False,export_lights=False,export_image_format='AUTO')
export('neris-castle.glb',objects+[pivot])
static=[o for o in objects if o.get('owner')!='bridge']
moving=[o for o in objects if o.get('owner')=='bridge']
export('castle.glb',static)
pivot.location=(0,0,0);bpy.context.view_layer.update()
export('drawbridge.glb',moving+[pivot])
pivot.location=pivot_src.location
result={'source_triangles':251729,'export_triangles':sum(len(g['faces']) for g in groups.values()),'removed_zero_area_triangles':dropped,'uv_fallback_triangles':uv_fallbacks,'static_parts':[o.name for o in static],'moving_parts':[o.name for o in moving],'axis_convention':'Blender (x,y,z) -> glTF (x,z,-y), export_yup true','bridge_hinge_blender':list(pivot_src.location),'source_modified':False,'texture_embedded':True}
s.collection.children.unlink(candidate)
s.camera=bpy.data.objects['NC.CAM.HeroThreeQuarter']
`;
