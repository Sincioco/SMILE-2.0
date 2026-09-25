"""Cut the moat out of the town slab and give its bed a deeper blue appearance."""
import bpy
from mathutils import Matrix, Vector

MOAT = [(-95,84,-1,92),(-95,161,-1,169),(-95,92,-86,161),(-10,92,-1,161)]


def apply():
    bpy.context.view_layer.update()
    for name in ['Town Garden Ground', 'Town Bedrock Plinth']:
        obj = bpy.data.objects[name]
        if obj.get('neris_moat_cut'):
            continue
        points = [obj.matrix_world @ Vector(v) for v in obj.bound_box]
        left,right = min(p.x for p in points),max(p.x for p in points)
        front,back = min(p.y for p in points),max(p.y for p in points)
        bottom,top = min(p.z for p in points),max(p.z for p in points)
        xs = sorted({left,right,*[x for r in MOAT for x in (r[0],r[2])]})
        ys = sorted({front,back,*[y for r in MOAT for y in (r[1],r[3])]})
        cells = {(i,j) for i in range(len(xs)-1) for j in range(len(ys)-1)
                 if not any(r[0] <= (xs[i]+xs[i+1])/2 <= r[2] and
                            r[1] <= (ys[j]+ys[j+1])/2 <= r[3] for r in MOAT)}
        vertices,faces,lookup = [],[],{}
        def face(points):
            indices=[]
            for p in points:
                if p not in lookup:lookup[p]=len(vertices);vertices.append(p)
                indices.append(lookup[p])
            faces.append(tuple(indices))
        for i,j in sorted(cells):
            x0,x1=xs[i:i+2]; y0,y1=ys[j:j+2]
            face([(x0,y0,top),(x1,y0,top),(x1,y1,top),(x0,y1,top)])
            face([(x0,y1,bottom),(x1,y1,bottom),(x1,y0,bottom),(x0,y0,bottom)])
            if (i-1,j) not in cells:face([(x0,y0,bottom),(x0,y0,top),(x0,y1,top),(x0,y1,bottom)])
            if (i+1,j) not in cells:face([(x1,y1,bottom),(x1,y1,top),(x1,y0,top),(x1,y0,bottom)])
            if (i,j-1) not in cells:face([(x1,y0,bottom),(x1,y0,top),(x0,y0,top),(x0,y0,bottom)])
            if (i,j+1) not in cells:face([(x0,y1,bottom),(x0,y1,top),(x1,y1,top),(x1,y1,bottom)])
        mesh=bpy.data.meshes.new(name+' With Moat');mesh.from_pydata(vertices,[],faces);mesh.update()
        for material in obj.data.materials:mesh.materials.append(material)
        old=obj.data;obj.data=mesh;obj.matrix_world=Matrix.Identity(4)
        if old.users==0:bpy.data.meshes.remove(old)
        obj['neris_moat_cut']=True
    base=bpy.data.materials['Town Water']
    mat=bpy.data.materials.get('Royal Deep Blue Water') or base.copy()
    mat.name='Royal Deep Blue Water'
    mat.diffuse_color=(.012,.055,.14,1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.012,.055,.14,1)
    mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.38
    bed=bpy.data.materials.get('Royal Deep Blue Moat Bed') or bpy.data.materials['Recess Shadow'].copy()
    bed.name='Royal Deep Blue Moat Bed'
    bed.diffuse_color=(.012,.035,.085,1)
    bed.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.012,.035,.085,1)
    for obj in bpy.data.objects:
        if obj.name.startswith('Castle Moat Water'):
            obj.data.materials.clear();obj.data.materials.append(mat)
        elif obj.name.startswith('Castle Moat Lining'):
            obj.location.z=-.65
            obj.data.materials.clear();obj.data.materials.append(bed)
    bpy.context.view_layer.update()
    print('MOAT terrain cut and deeper blue water/bed prepared',flush=True)
