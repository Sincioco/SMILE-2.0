"""Replace the original canopy placeholders in a separately saved town revision.

Run in background Blender with Blend/Neris-Town-V1.blend loaded. Real mesh leaves,
branched wood and deterministic variation need no textures or external add-ons.
"""
from pathlib import Path
import json
import math
import random
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
assert bpy.app.background, "Use a background process; preserve the interactive scene."
assert Path(bpy.data.filepath).name == "Neris-Town-V1.blend"


def material(name, color, roughness):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1)
    value.use_nodes = True
    node = value.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = (*color, 1)
    node.inputs["Roughness"].default_value = roughness
    return value


MATERIALS = [material("Neris Ridged Chestnut Bark", (.18, .075, .026), .86),
             material("Neris Mature Jade Leaves", (.12, .28, .055), .63),
             material("Neris Young Olive Leaves", (.29, .43, .095), .57),
             material("Neris Sunlit Leaf Tips", (.40, .52, .14), .60)]


class Tree:
    def __init__(self, seed):
        self.rng = random.Random(seed)
        self.vertices = []
        self.faces = []
        self.materials = []
        self.leaves = 0
        self.branches = 0

    def face(self, corners, mat):
        self.faces.append(corners)
        self.materials.append(mat)

    def branch(self, points, radii, sides=10):
        """Tapered bent tubes with subtle longitudinal bark ridges."""
        self.branches += 1
        start = len(self.vertices)
        for i, point in enumerate(points):
            tangent = (points[min(i+1, len(points)-1)] - points[max(0, i-1)]).normalized()
            side = tangent.cross(Vector((0, 1, .1))).normalized()
            other = tangent.cross(side).normalized()
            for j in range(sides):
                angle = j * math.tau / sides
                radius = radii[i] * (1 + .09 * math.sin(j * 2.7))
                self.vertices.append(point + radius * (math.cos(angle)*side + math.sin(angle)*other))
        for i in range(len(points)-1):
            for j in range(sides):
                a = start + i*sides+j
                b = start + i*sides+(j+1)%sides
                self.face((a, b, b+sides, a+sides), 0)
        self.face(tuple(start + (len(points)-1)*sides+j for j in range(sides)), 0)

    def leaf(self, stem, direction, length):
        """Six-vertex pointed leaf with a raised central vein, not a billboard."""
        direction.normalize()
        side = direction.cross(Vector((0, 0, 1)))
        if side.length < .1:
            side = Vector((1, 0, 0))
        side.normalize()
        normal = side.cross(direction).normalized()
        width = length * self.rng.uniform(.24, .34)
        start = len(self.vertices)
        self.vertices.extend([stem, stem+direction*length*.42+side*width,
            stem+direction*length*.49+normal*length*.10,
            stem+direction*length*.42-side*width,
            stem+direction*length*.78+normal*length*.075,
            stem+direction*length])
        mat = self.rng.choices([1, 2, 3], [5, 3, 1])[0]
        for face in [(0, 1, 2), (0, 2, 3), (1, 5, 4, 2), (3, 2, 4, 5)]:
            self.face(tuple(start+i for i in face), mat)
        self.leaves += 1

    def build(self, variant):
        self.branch([Vector((0, 0, 0)), Vector((.04, -.03, .5)),
                     Vector((-.09, .07, 1.55)), Vector((.10, .03, 2.6)),
                     Vector((.03, .08, 3.7)), Vector((.18, .1, 4.7))],
                    [.40, .26, .21, .16, .09, .02], 20)
        for i in range(7):
            a = i*math.tau/7
            self.branch([Vector((0, 0, .55)), Vector((math.cos(a)*.36, math.sin(a)*.36, .14)),
                         Vector((math.cos(a)*.73, math.sin(a)*.73, .025))], [.13, .10, .018], 10)
        # Uneven whorls leave gaps through which the branch structure can be seen.
        for i in range(15):
            a = i*2.39996 + variant*.3
            height = 1.9 + i*.14
            reach = 2.0 if i < 10 else 1.45
            outward = Vector((math.cos(a), math.sin(a), 0))
            origin = Vector((.06, .04, height))
            elbow = origin + outward*reach*.53 + Vector((0, 0, .7))
            end = origin + outward*reach + Vector((0, 0, 1.3))
            self.branch([origin, elbow, end], [.105, .062, .013], 12)
            for j in range(9):
                t = .25 + j*.083
                root = elbow.lerp(end, t)
                turn = a + (1 if j%2 else -1)*self.rng.uniform(.45, 1.45)
                twig_direction = Vector((math.cos(turn), math.sin(turn), self.rng.uniform(.2, .8)))
                tip = root + twig_direction*self.rng.uniform(.55, 1.05)
                mid = root.lerp(tip, .5) + Vector((0, 0, .1))
                self.branch([root, mid, tip], [.027, .015, .0025], 6)
                for k in range(22):
                    f = .08 + k*.041
                    stem = root.lerp(tip, f) + Vector((0, 0, math.sin(f*math.pi)*.10))
                    angle = turn + (1 if k%2 else -1)*self.rng.uniform(.7, 1.75)
                    direction = Vector((math.cos(angle), math.sin(angle), self.rng.uniform(-.30, .65)))
                    self.leaf(stem, direction, self.rng.uniform(.22, .40))
        mesh = bpy.data.meshes.new(f"Neris Detailed Tree {variant+1}")
        mesh.from_pydata(self.vertices, [], self.faces)
        for mat in MATERIALS:
            mesh.materials.append(mat)
        for poly, mat in zip(mesh.polygons, self.materials):
            poly.material_index = mat
            poly.use_smooth = True
        mesh.update()
        collection = bpy.data.collections.new(f"Neris Detailed Tree {variant+1}")
        obj = bpy.data.objects.new(collection.name, mesh)
        collection.objects.link(obj)
        return collection


builders = [Tree(9103), Tree(3719)]
templates = [tree.build(i) for i, tree in enumerate(builders)]
placements = sorted([obj for obj in bpy.data.objects if obj.name.startswith("Garden Tree ")], key=lambda o:o.name)
assert len(placements) == 27
for i, obj in enumerate(placements):
    obj.instance_collection = templates[i % len(templates)]
    obj["Foliage"] = "2970 individually modeled leaves; tapered branching trunk"

# This derived revision retains the original town layout and original source file.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / "Blend/Neris-Town-Detailed.blend"))
(ROOT / "tree-details.json").write_text(json.dumps({
    "source": "Source/detail_trees.py", "seed": [9103, 3719], "instances": len(placements),
    "variants": [{"leaves": t.leaves, "branches": t.branches, "vertices": len(t.vertices),
                  "faces": len(t.faces)} for t in builders]}, indent=2)+"\n")
print("NERIS detailed trees saved", flush=True)
