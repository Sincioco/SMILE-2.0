"""Export the Spaceport source with its existing chunking, normals and authored UVs."""

import bpy,json
from mathutils import Vector

def scene_measure(scene):
    pts=[ob.matrix_world@v.co for ob in scene.objects if ob.type=='MESH' for v in ob.data.vertices]
    lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
    meshes=[ob for ob in scene.objects if ob.type=='MESH'];tri=0
    for ob in meshes:ob.data.calc_loop_triangles();tri+=len(ob.data.loop_triangles)
    return {'min':lo,'max':hi,'dimensions':[hi[i]-lo[i] for i in range(3)],'mesh_objects':len(meshes),'triangles':tri,'material_count':len({m.as_pointer() for ob in meshes for m in ob.data.materials if m})}

def chunk_for(ob,center):
    name=ob.name;owner=ob.get('owner');kind=ob.get('kind')
    if kind=='door-panel':return name,tuple(ob.location),'door-panel'
    if kind=='lift-cabin':return ob.parent.name,tuple(ob.parent.location),'lift-cabin'
    if kind=='entrance-door':return ob.parent.name,tuple(ob.parent.location),'entrance-door'
    if name.startswith(('NSP01.Spire.','NSP01.Tower.')):
        tokens=name.split('.');n=3 if tokens[2]=='Central' else 5
        return '.'.join(tokens[:n]),(0,0,0),'static'
    if owner=='Docks' or '.Dock.' in name:
        return 'Dock.'+('E' if center.x>0 else 'W')+'.'+('HIGH' if center.y>100 else 'LOW'),(0,0,0),'static'
    if owner=='Hangars' or name.startswith(('NSP01.HangarFacade','NSP01.HangarRear','NSP01.Hangar.')):
        return 'Hangar.'+('E' if center.x>0 else 'W'),(0,0,0),'static'
    if owner=='Hull' or name.startswith('NSP01.Hull.'):
        return ('Underside' if center.z<-18 else 'Hull'),(0,0,0),'static'
    if owner=='InteriorRoute':return 'Interior.'+('Center' if abs(center.x)<54 else 'East' if center.x>0 else 'West'),(0,0,0),'static'
    if name.startswith(('NSP01.Approach.','NSP01.Apron.','NSP01.Compass.Plaza')):return 'Approach',(0,0,0),'static'
    if name.startswith('NSP01.Facade.'):
        return 'Facade.'+('Front' if center.y<0 else 'Rear' if center.y>165 else 'East' if center.x>0 else 'West'),(0,0,0),'static'
    return 'Terminal',(0,0,0),'static'

def chunked_export(source):
    deps=bpy.context.evaluated_depsgraph_get();groups={};dropped=0;allowlist=[]
    for ob in source.objects:
        if not ob.get('export_eligible') or ob.get('asset_id')!='NSP01' or ob.type not in ('MESH','CURVE'):continue
        assert not ob.hide_render and not any(col.hide_render for col in ob.users_collection)
        allowlist.append(ob.name);ev=ob.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles()
        vertices=[ev.matrix_world@v.co for v in me.vertices];center=sum(vertices,Vector())/max(1,len(vertices))
        chunk,origin,kind=chunk_for(ob,center)
        for tri in me.loop_triangles:
            ia,ib,ic=tri.vertices
            if (vertices[ib]-vertices[ia]).cross(vertices[ic]-vertices[ia]).length<2e-10:dropped+=1;continue
            mat=me.materials[me.polygons[tri.polygon_index].material_index] if me.materials else bpy.data.materials['NSP01.MAT.Ivory']
            key=chunk if kind!='static' else chunk+'|'+mat.name
            group=groups.setdefault(key,{'chunk':chunk,'kind':kind,'origin':origin,'vertices':[],'faces':[],'lookup':{},'materials':[],'mat_indices':[],'smooth':[],'parts':set(),'uvs':[]})
            if mat not in group['materials']:group['materials'].append(mat)
            indices=[]
            for index in tri.vertices:
                vkey=(ob.name,index)
                if vkey not in group['lookup']:
                    group['lookup'][vkey]=len(group['vertices']);group['vertices'].append(tuple(vertices[index]-Vector(origin)))
                indices.append(group['lookup'][vkey])
            group['faces'].append(indices);group['mat_indices'].append(group['materials'].index(mat));group['smooth'].append(me.polygons[tri.polygon_index].use_smooth);group['parts'].add(ob.name)
            group['uvs'].extend([tuple(me.uv_layers.active.data[li].uv) if me.uv_layers.active else (0,0) for li in tri.loops])
        ev.to_mesh_clear()
    target=bpy.data.scenes.new('NSP01.Export.L0');target['asset_id']='NSP01';target['owner']='Export';target.unit_settings.system='METRIC';target.unit_settings.scale_length=1
    root=bpy.data.objects.new('NSP01',None);target.collection.objects.link(root);root['asset_id']='NSP01';root['design_version']='1.0.0'
    manifest=[]
    for i,(key,g) in enumerate(groups.items()):
        name='NSP01.L0.'+g['chunk']+'.'+str(i);me=bpy.data.meshes.new(name);me.from_pydata(g['vertices'],[],g['faces']);me.update()
        for mat in g['materials']:me.materials.append(mat)
        for f,idx,smooth in zip(me.polygons,g['mat_indices'],g['smooth']):f.material_index=idx;f.use_smooth=smooth
        uv=me.uv_layers.new(name='RoyalStoneMeters')
        for li,co in enumerate(g['uvs']):uv.data[li].uv=co
        ob=bpy.data.objects.new(name,me);target.collection.objects.link(ob);ob.parent=root;ob.location=g['origin'];ob['asset_id']='NSP01';ob['chunk_id']=g['chunk'];ob['kind']=g['kind'];ob['part_ids_json']=json.dumps(sorted(g['parts']))
        manifest.append({'node':name,'chunk':g['chunk'],'kind':g['kind'],'origin_blender_m':g['origin'],'triangles':len(g['faces']),'materials':[m.name for m in g['materials']],'source_parts':sorted(g['parts'])})
    with bpy.context.temp_override(scene=target,view_layer=target.view_layers[0]):bpy.context.view_layer.update()
    return target,{'allowlist':allowlist,'chunks':manifest,'removed_degenerate_triangles':dropped}
