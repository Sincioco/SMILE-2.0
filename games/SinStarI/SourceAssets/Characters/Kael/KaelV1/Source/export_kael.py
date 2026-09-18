"""Validate Kael's grounded checkpoint and export its shared native runtime asset."""

import hashlib
import json
import struct
import sys
from pathlib import Path

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from fit_and_ground_kael import action_at

PACKAGE = Path(__file__).resolve().parent.parent
SOURCE = PACKAGE / "Source"
BLENDER = PACKAGE / "Blender"
MODEL = PACKAGE / "kael-v1-animation-checkpoint.glb"
CLIPS = ("Idle", "Walk", "Run", "Attack", "Attack2", "Dodge", "Defend", "Hit", "Death", "Victory")


def bounds(obj):
    evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = evaluated.to_mesh()
    points = [obj.matrix_world @ v.co for v in mesh.vertices]
    result = {"minimum": [min(p[i] for p in points) for i in range(3)],
              "maximum": [max(p[i] for p in points) for i in range(3)]}
    evaluated.to_mesh_clear()
    return result


def topology(obj):
    obj.data.validate(verbose=True, clean_customdata=False)
    uv = obj.data.uv_layers.active
    if not uv:
        raise RuntimeError(f"Missing UVs: {obj.name}")
    uv.name = "UVMap"
    uv.active_render = True
    repaired = 0
    for poly in obj.data.polygons:
        indices = list(poly.loop_indices)
        a, b, c = [uv.data[i].uv.copy() for i in indices]
        if abs((b-a).x*(c-a).y - (b-a).y*(c-a).x) < 1e-13:
            center = (a+b+c)/3
            for index, delta in zip(indices, ((-.00012,-.00012),(.00012,-.00012),(0,.00012))):
                uv.data[index].uv = center + Vector(delta)
            repaired += 1
    obj.data.calc_tangents(uvmap="UVMap")
    invalid = sum(loop.tangent.length < 0.5 for loop in obj.data.loops)
    repaired_normals = 0
    if invalid:
        normals = [loop.normal.copy() for loop in obj.data.loops]
        for poly in obj.data.polygons:
            for index in poly.loop_indices:
                if obj.data.loops[index].tangent.length < 0.5:
                    normals[index] = poly.normal.copy()
                    repaired_normals += 1
        obj.data.normals_split_custom_set(normals)
        obj.data.free_tangents()
        obj.data.update()
        obj.data.calc_tangents(uvmap="UVMap")
        invalid = sum(loop.tangent.length < 0.5 for loop in obj.data.loops)
    moved_vertices = {}
    if invalid:
        # The flattened blade contains one micro-triangle below Blender's tangent threshold.
        for poly in obj.data.polygons:
            if not any(obj.data.loops[i].tangent.length < .5 for i in poly.loop_indices):
                continue
            indices = list(poly.vertices)
            points = [obj.data.vertices[i].co.copy() for i in indices]
            edge = max(((0,1,2),(1,2,0),(2,0,1)), key=lambda e: (points[e[1]]-points[e[0]]).length)
            start, end, opposite = [points[i] for i in edge]
            axis = (end-start).normalized()
            foot = start + axis*(opposite-start).dot(axis)
            altitude = opposite-foot
            if altitude.length < 1e-12:
                raise RuntimeError("Collapsed source triangle")
            index = indices[edge[2]]
            moved_vertices[index] = obj.data.vertices[index].co.copy()
            obj.data.vertices[index].co = foot + altitude.normalized()*(1.08e-6/(end-start).length)
        obj.data.free_tangents()
        obj.data.update()
        obj.data.calc_tangents(uvmap="UVMap")
        invalid = sum(loop.tangent.length < .5 for loop in obj.data.loops)
    if invalid:
        raise RuntimeError(f"Invalid tangents on {obj.name}: {invalid}")
    maximum_weights = max(sum(g.weight > 1e-6 for g in v.groups) for v in obj.data.vertices)
    unweighted = sum(not v.groups for v in obj.data.vertices)
    if maximum_weights > 4 or unweighted:
        raise RuntimeError(f"Invalid skin weights: {obj.name}")
    return {"part": obj.name, "vertices": len(obj.data.vertices),
            "triangles": len(obj.data.polygons), "collapsedUvFacesRepaired": repaired,
            "cornerNormalsRepaired": repaired_normals,
            "microTriangleVerticesRepaired": len(moved_vertices),
            "maximumVertexRepairMeters": max(((obj.data.vertices[i].co-p).length
                for i,p in moved_vertices.items()), default=0),
            "invalidTangents": invalid, "maximumInfluences": maximum_weights,
            "unweightedVertices": unweighted}


def main():
    bpy.ops.wm.open_mainfile(filepath=str(BLENDER / "kael-v1-grounded.blend"))
    rig = bpy.data.objects["Kael.Rig"]
    body = bpy.data.objects["Kael.Body"]
    sword = bpy.data.objects["Kael.Sword"]
    if {a.name for a in bpy.data.actions} != set(CLIPS):
        raise RuntimeError("Unexpected animation set")
    parts = [topology(body), topology(sword)]
    rig.data.pose_position = "REST"
    bpy.context.view_layer.update()
    bind = {"body": bounds(body), "sword": bounds(sword)}
    rig.data.pose_position = "POSE"
    clips = []
    for name in CLIPS:
        action = action_at(rig, name, 1)
        samples = []
        roots = []
        for frame in range(1, int(action.frame_range[1])+1):
            action_at(rig, name, frame)
            body_bounds, sword_bounds = bounds(body), bounds(sword)
            sample = {"frame": frame-1, "bodyMinimumMeters": body_bounds["minimum"][2],
                      "swordMinimumMeters": sword_bounds["minimum"][2]}
            if min(sample["bodyMinimumMeters"], sample["swordMinimumMeters"]) < -.0001:
                raise RuntimeError((name, "floor penetration", sample))
            samples.append(sample)
            roots.append(rig.pose.bones["mixamorig:Hips"].matrix.translation.copy())
        spans = [max(p[i] for p in roots)-min(p[i] for p in roots) for i in (0,1)]
        if max(spans) > .0001 or abs(samples[0]["bodyMinimumMeters"]-.001) > .002:
            raise RuntimeError((name, "root motion or frame-zero floor mismatch", spans, samples[0]))
        row = {"clip": name, "framesChecked": len(samples), "rootXYSpanMeters": spans,
               "minimumBodyMeters": min(s["bodyMinimumMeters"] for s in samples),
               "minimumSwordMeters": min(s["swordMinimumMeters"] for s in samples),
               "samples": [samples[0], samples[(len(samples)-1)//2], samples[-1]]}
        clips.append(row)
        print("KAEL_CLIP_VALID " + json.dumps(row), flush=True)
    action_at(rig, "Idle", 1)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in (rig, body, sword):
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    for action in bpy.data.actions:
        action.use_fake_user = True
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLENDER / "kael-v1-rigged-animation-checkpoint.blend"), compress=True)
    bpy.ops.export_scene.gltf(filepath=str(MODEL), export_format="GLB", use_selection=True,
        export_animations=True, export_animation_mode="ACTIONS", export_skins=True,
        export_influence_nb=4, export_all_influences=False, export_tangents=True, export_yup=True)
    data = MODEL.read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20+length])
    primitive_counts = [gltf["accessors"][p["attributes"]["POSITION"]]["count"]
                        for m in gltf["meshes"] for p in m["primitives"]]
    if max(primitive_counts) > 65535 or len(rig.data.bones) > 128:
        raise RuntimeError("Runtime primitive or skeleton budget exceeded")
    report = {"modelSha256": hashlib.sha256(data).hexdigest(), "modelBytes": len(data),
              "units": "meters; Blender Z becomes glTF/SM3D Y", "bindBounds": bind,
              "bindToIdleFrameZeroFloorDeltaMeters": abs(bind["body"]["minimum"][2]-clips[0]["samples"][0]["bodyMinimumMeters"]),
              "clips": clips, "topology": parts, "rigBones": len(rig.data.bones),
              "mixamoBones": 41, "authoredBones": ["KaelSword"],
              "exportedPrimitiveVertexCounts": primitive_counts,
              "bodyTextureSize": [4096,4096], "swordTextureSize": [2048,2048]}
    (SOURCE / "export-validation.json").write_text(json.dumps(report, indent=2), encoding="utf-8", newline="\n")
    sockets = {name: {"node": "mixamorig:"+bone} for name,bone in
               (("Root","Hips"),("Head","Head"),("Chest","Spine2"),("HandRight","RightHand"),
                ("HandLeft","LeftHand"),("FootLeft","LeftFoot"),("FootRight","RightFoot"))}
    sockets.update({"SwordGrip": {"node":"KaelSword"}, "SwordBase":{"node":"KaelSword"},
                    "SwordTip":{"node":"KaelSword","translation":[0,0,.6084]}})
    descriptor = {"version":1,"sampleRate":30,"clips":{n:{"loop":n in CLIPS[:3]} for n in CLIPS},
                  "sockets":sockets}
    (PACKAGE / "KaelV1.sm3d.json").write_text(json.dumps(descriptor, indent=2), encoding="utf-8", newline="\n")
    print("KAEL_EXPORT_READY " + report["modelSha256"], flush=True)


if __name__ == "__main__":
    main()
