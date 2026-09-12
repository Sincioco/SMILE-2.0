"""Replace the reduced staff with closed low-poly surfaces and the preserved palette."""
import bpy,bmesh,math,json
from mathutils import Vector
from pathlib import Path
rig=bpy.data.objects['Mira3.Rig'];old=bpy.data.objects['Mira3.Staff'];scene=bpy.context.scene;scene.frame_set(1);bpy.context.view_layer.update()
hand=rig.pose.bones['mixamorig:RightHand'];grip=rig.matrix_world @ hand.matrix @ Vector((0,.073,.035));vertices=[];faces=[];colors=[]
def tube(centers,radii,sides,color,closed=False):
    offset=len(vertices)
    for i,c in enumerate(centers):
        tangent=(centers[(i+1)%len(centers)]-centers[i-1]).normalized() if closed else ((centers[min(i+1,len(centers)-1)]-centers[max(0,i-1)]).normalized())
        axis=Vector((0,1,0));u=tangent.cross(axis).normalized();v=tangent.cross(u).normalized()
        for j in range(sides): vertices.append(c+radii[i]*(u*math.cos(j*math.tau/sides)+v*math.sin(j*math.tau/sides)))
    for i in range(len(centers) if closed else len(centers)-1):
        n=(i+1)%len(centers)
        for j in range(sides):
            k=(j+1)%sides;faces.extend([(offset+i*sides+j,offset+n*sides+j,offset+n*sides+k),(offset+i*sides+j,offset+n*sides+k,offset+i*sides+k)]);colors.extend([color,color])
    if not closed:
        for index,reverse in ((0,True),(len(centers)-1,False)):
            center=len(vertices);vertices.append(centers[index])
            for j in range(sides):
                k=(j+1)%sides;face=(center,offset+index*sides+j,offset+index*sides+k);faces.append(face[::-1] if reverse else face);colors.append(color)
def rod(z1,z2,radius,color,sides=12): tube([Vector((grip.x,grip.y,z1)),Vector((grip.x,grip.y,z2))],[radius,radius],sides,color)
rod(.1,1.63,.014,0)
rod(.88,1.18,.0148,1)
for z in [.18,.87,1.18,1.55,1.62]:rod(z,z+.019,.019,2)
tube([Vector((grip.x,grip.y,.075)),Vector((grip.x,grip.y,.18))],[.004,.014],12,2)
centers=[];radii=[]
for i in range(37):
    t=math.radians(38+284*i/36);centers.append(Vector((grip.x+.12*math.sin(t),grip.y,1.825+.19*math.cos(t))));radii.append(.003+.013*math.sin(math.pi*i/36))
tube(centers,radii,8,2)
tube([p+Vector((0,-.012,0)) for p in centers],[max(.0011,r*.33) for r in radii],4,3)
# Two gold supports cradle the central water crystal.
for direction in [-1,1]:
    points=[Vector((grip.x+direction*.065*math.sin(math.pi*i/10),grip.y,1.57+.2*i/10)) for i in range(11)]
    tube(points,[.007]*11,6,2)
# Closed faceted crystal.
o=len(vertices);vertices.extend([Vector((grip.x,grip.y,1.925)),Vector((grip.x,grip.y,1.725))])
for i in range(8):vertices.append(Vector((grip.x+.037*math.cos(i*math.tau/8),grip.y+.037*math.sin(i*math.tau/8),1.815)))
for i in range(8):faces.extend([(o,o+2+i,o+2+(i+1)%8),(o+1,o+2+(i+1)%8,o+2+i)]);colors.extend([3,3])
inverse=(rig.matrix_world @ hand.matrix @ hand.bone.matrix_local.inverted()).inverted();mesh=bpy.data.meshes.new('Mira3.ClosedStaff');mesh.from_pydata([inverse@p for p in vertices],[],faces);mesh.materials.append(old.data.materials[0]);uv=mesh.uv_layers.new(name='Palette')
for face,color in zip(mesh.polygons,colors):
    center=(color+.5)/4
    for loop,point in zip(face.loop_indices,[(center-.002,.498),(center+.002,.498),(center,.502)]):uv.data[loop].uv=point
bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));boundary=sum(e.is_boundary for e in bm.edges);nonmanifold=sum(not e.is_manifold for e in bm.edges);bm.to_mesh(mesh);bm.free()
staff=bpy.data.objects.new('Mira3.NewStaff',mesh);scene.collection.objects.link(staff);staff.parent=rig;group=staff.vertex_groups.new(name=hand.name);group.add(list(range(len(vertices))),1,'REPLACE');modifier=staff.modifiers.new('Mira3 Own Rig','ARMATURE');modifier.object=rig
bpy.data.objects.remove(old,do_unlink=True);staff.name='Mira3.Staff';bpy.context.view_layer.update()
result={'staffTriangles':len(faces),'boundaryEdges':boundary,'nonManifoldEdges':nonmanifold,'equippedTriangles':18100+len(faces)}
Path(r'D:\AI\Mira3D\Mira3\mira3-closed-staff-report.json').write_text(json.dumps(result,indent=2))
