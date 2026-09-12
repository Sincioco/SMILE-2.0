import bpy,math,json,bmesh
from pathlib import Path
from mathutils import Vector
root=Path(r'D:\AI\Mira3D\Mira3');headroot=root/'HeadPaint';headroot.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(root/'mira3-paint-conditions.blend'))
scene=bpy.context.scene;body=bpy.data.objects['Mira3.HunyuanShape']
# A head-only conditioning mesh gives the face most of the model's 512-pixel view.
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<.386 or abs(v.co.x)>.1],context='VERTS')
bm.to_mesh(body.data);bm.free()
coords=[v.co.copy() for v in body.data.vertices]
center=Vector([(min(v[i] for v in coords)+max(v[i] for v in coords))/2 for i in range(3)])
diameter=max((v-center).length for v in coords)*2;factor=1.15/diameter
for v in body.data.vertices:v.co=(v.co-center)*factor
material=body.data.materials[0];nodes=material.node_tree.nodes;links=material.node_tree.links
geom=next(n for n in nodes if n.type=='NEW_GEOMETRY');scale=next(n for n in nodes if n.type=='VECT_MATH' and n.operation=='SCALE')
camera=scene.camera
views=[(0,0),(0,90),(0,180),(0,270),(60,0),(-60,180)];records=[]
for index,(elev,azim) in enumerate(views):
    e=math.radians(-elev);a=math.radians(azim+90)
    camera.location=(1.45*math.cos(e)*math.cos(a),1.45*math.cos(e)*math.sin(a),1.45*math.sin(e))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    for kind,socket,mult in [('normal','Normal',.5),('position','Position',-1/1.15)]:
        links.new(geom.outputs[socket],scale.inputs[0]);scale.inputs[3].default_value=mult
        scene.render.filepath=str(headroot/f'{kind}-{index}.png');bpy.ops.render.render(write_still=True)
    records.append({'index':index,'elev':elev,'azim':azim,'camera_matrix':[list(r) for r in camera.matrix_world]})
(headroot/'paint-views.json').write_text(json.dumps({'views':records,'center':list(center),'scale':factor,'cutoff_z':.386,'ortho_scale':1.2},indent=2),encoding='utf-8')
print('HEAD_CONDITIONS_READY',flush=True)
