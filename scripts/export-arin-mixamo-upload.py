"""Export Arin's repository-owned T-pose GLB as a body-only Mixamo upload FBX."""

from __future__ import annotations

import os
import sys

import bpy


def command_line_arguments() -> tuple[str, str]:
    arguments = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if len(arguments) != 2:
        raise RuntimeError("Expected source GLB and destination FBX after --.")

    return os.path.abspath(arguments[0]), os.path.abspath(arguments[1])


def reset_scene() -> None:
    for item in list(bpy.data.objects):
        bpy.data.objects.remove(item, do_unlink=True)

    for collection in (
        bpy.data.actions,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.meshes,
    ):
        for data_block in list(collection):
            collection.remove(data_block)


def validate_t_pose() -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    armatures = [item for item in bpy.context.scene.objects if item.type == "ARMATURE"]
    meshes = [
        item
        for item in bpy.context.scene.objects
        if item.type == "MESH" and item.name.startswith("tripo_part_")
    ]
    if len(armatures) != 1:
        raise RuntimeError(f"Expected one armature, found {len(armatures)}.")
    if len(meshes) != 30:
        mesh_summary = ", ".join(f"{item.name}:{len(item.data.vertices)}" for item in meshes)
        raise RuntimeError(
            f"Expected 30 body mesh parts, found {len(meshes)}: {mesh_summary}"
        )
    if len(armatures[0].data.bones) != 41:
        raise RuntimeError(
            f"Expected the 41-bone Arin source rig, found {len(armatures[0].data.bones)} bones."
        )
    if bpy.data.actions:
        raise RuntimeError("The Mixamo upload source must not contain animation actions.")

    return armatures[0], meshes


def main() -> None:
    source_glb, destination_fbx = command_line_arguments()
    if not os.path.isfile(source_glb):
        raise RuntimeError(f"Source GLB was not found: {source_glb}")

    os.makedirs(os.path.dirname(destination_fbx), exist_ok=True)
    reset_scene()
    bpy.ops.import_scene.gltf(filepath=source_glb)
    armature, meshes = validate_t_pose()

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for mesh in meshes:
        mesh.select_set(True)
    bpy.context.view_layer.objects.active = armature

    result = bpy.ops.export_scene.fbx(
        filepath=destination_fbx,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=False,
        path_mode="COPY",
        embed_textures=True,
    )
    if "FINISHED" not in result:
        raise RuntimeError(f"FBX export failed: {result}")
    if not os.path.isfile(destination_fbx):
        raise RuntimeError("FBX export reported success but produced no file.")

    print(
        "SMILE_MIXAMO_UPLOAD="
        f"source={source_glb};destination={destination_fbx};"
        f"bones={len(armature.data.bones)};meshes={len(meshes)}"
    )


if __name__ == "__main__":
    main()
