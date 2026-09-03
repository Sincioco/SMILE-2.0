"""Prepare the preserved Red Dragon GLB for deterministic SM3D cooking."""

from __future__ import annotations

import json
import os
import sys

import bmesh
import bpy


# SM3D requires the squared cross-product magnitude to exceed 1e-12. Blender's
# polygon area is half that cross-product magnitude, so use a small safety margin.
DEGENERATE_AREA_EPSILON = 5.1e-7


def script_arguments() -> tuple[str, str, str]:
    separator = sys.argv.index("--") if "--" in sys.argv else len(sys.argv)
    arguments = sys.argv[separator + 1 :]
    if len(arguments) != 3:
        raise RuntimeError(
            "Expected input GLB, output GLB, and JSON report path after --."
        )

    return tuple(os.path.abspath(value) for value in arguments)


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def clean_meshes() -> dict[str, int]:
    mesh_objects = sorted(
        (obj for obj in bpy.context.scene.objects if obj.type == "MESH"),
        key=lambda obj: obj.name,
    )
    source_faces = 0
    removed_faces = 0

    for obj in mesh_objects:
        mesh = obj.data
        source_faces += len(mesh.polygons)
        working = bmesh.new()
        working.from_mesh(mesh)
        degenerate_faces = [
            face
            for face in working.faces
            if len(face.verts) < 3 or face.calc_area() <= DEGENERATE_AREA_EPSILON
        ]
        removed_faces += len(degenerate_faces)
        if degenerate_faces:
            bmesh.ops.delete(working, geom=degenerate_faces, context="FACES_ONLY")
        working.to_mesh(mesh)
        working.free()
        mesh.update()
        if not mesh.validate(verbose=False, clean_customdata=False):
            continue

    output_faces = sum(len(obj.data.polygons) for obj in mesh_objects)
    output_vertices = sum(len(obj.data.vertices) for obj in mesh_objects)
    output_triangles = sum(
        len(polygon.vertices) - 2
        for obj in mesh_objects
        for polygon in obj.data.polygons
    )
    return {
        "meshObjects": len(mesh_objects),
        "sourceFaces": source_faces,
        "removedDegenerateFaces": removed_faces,
        "outputFaces": output_faces,
        "outputVertices": output_vertices,
        "outputTriangles": output_triangles,
    }


def export_scene(output_glb: str) -> None:
    os.makedirs(os.path.dirname(output_glb), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    result = bpy.ops.export_scene.gltf(
        filepath=output_glb,
        export_format="GLB",
        use_selection=True,
        export_materials="EXPORT",
        export_animations=False,
        export_skins=False,
        export_yup=True,
    )
    if "FINISHED" not in result:
        raise RuntimeError(f"Red Dragon GLB export failed: {result}")


def main() -> None:
    input_glb, output_glb, report_path = script_arguments()
    reset_scene()
    bpy.ops.import_scene.gltf(filepath=input_glb)
    report = clean_meshes()
    export_scene(output_glb)
    report["source"] = os.path.basename(input_glb)
    report["output"] = os.path.basename(output_glb)
    report["outputBytes"] = os.path.getsize(output_glb)
    os.makedirs(os.path.dirname(report_path), exist_ok=True)
    with open(report_path, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print("RED_DRAGON_STATIC=" + json.dumps(report, sort_keys=True))


if __name__ == "__main__":
    main()
