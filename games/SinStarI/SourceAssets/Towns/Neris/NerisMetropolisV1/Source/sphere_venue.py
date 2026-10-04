"""Neris entertainment sphere: a reusable LED shell and accessible entrance podium."""
import math
import bpy
from geometry import Mesh


def screen_material(index):
    name=f'Metropolis Sphere Screen {index:02}'
    mat=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    phase=index*math.tau/12
    color=(.10+.075*math.sin(phase), .40+.28*math.sin(phase+1.7),
           .72+.24*math.sin(phase+.7), 1)
    mat.diffuse_color=color
    mat.use_nodes=True
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=color
    shader.inputs['Metallic'].default_value=.12
    shader.inputs['Roughness'].default_value=.3
    shader.inputs['Emission Color'].default_value=color
    shader.inputs['Emission Strength'].default_value=.8
    return mat


def sphere():
    mesh=Mesh('Neris Sphere')
    screens=[screen_material(i) for i in range(12)]
    radius,center=180,90
    rows,columns=64,160
    limit=math.acos((12-center)/radius)
    def point(a,b):
        return (radius*math.sin(a)*math.cos(b),radius*math.sin(a)*math.sin(b),
                center+radius*math.cos(a))
    for row in range(rows):
        a0=max(.0001,row*limit/rows)
        a1=(row+1)*limit/rows
        for col in range(columns):
            b0=col*math.tau/columns
            b1=(col+1)*math.tau/columns
            # Continuous aurora bands wrap the shell; Studio animates their light.
            phase=(row/rows*1.4+.23*math.sin(b0*3)+.12*math.cos(b0*5+row*.08))%1
            mesh.polygon([point(a0,b0),point(a1,b0),point(a1,b1),point(a0,b1)],
                         screens[int(phase*12)])
    mesh.cylinder(0,0,0,192,4,'Pale Stone',segments=160)
    mesh.cylinder(0,0,4,166,11,'Graphite',segments=160)
    mesh.ring(167,1,14,'White',segments=160)
    for j in range(7):
        x=(j-3)*13
        mesh.box((x,-162,8),(10,4,9),'Window Blue')
        mesh.box((x,-172,14),(12,25,1.4),'Silver')
    mesh.box((0,-182,2),(104,20,4),'Pale Stone')
    mesh.solids.append(((-163,-161,0),(163,161,260)))
    mesh.floors.append(((-192,-192,0),(192,192,4)))
    return mesh
