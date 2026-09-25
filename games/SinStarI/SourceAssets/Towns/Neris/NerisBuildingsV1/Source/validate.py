"""Focused delivery checks: packed references, closed-source reload, GLB round trips."""
import hashlib
import json
import math
from pathlib import Path

import bpy


def main():
    if not bpy.app.background or bpy.data.filepath:
        raise RuntimeError('Run in a fresh background Blender process.')
    package=Path(__file__).resolve().parent.parent
    refs=package.parents[3]/'Assets'/'Towns'/'Neris'
    exports=refs/'ModelsV1'
    rows=[]
    for blend in sorted((package/'Blend').glob('*.blend')):
        bpy.ops.wm.open_mainfile(filepath=str(blend),load_ui=False)
        stats=json.loads((package/'Previews'/(blend.stem+'-validation.json')).read_text())
        reference=refs/stats['reference']
        packed=[image for image in bpy.data.images if image.packed_file]
        assert any(hashlib.sha256(image.packed_file.data).digest()==hashlib.sha256(reference.read_bytes()).digest()
                   for image in packed), 'Packed reference differs from original'
        assert bpy.context.scene.unit_settings.system=='METRIC'
        assert len([o for o in bpy.data.objects if o.type=='MESH'])>10
        glb=exports/(blend.stem+'.glb')
        # A fresh scene establishes that the GLB does not depend on the .blend.
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(glb))
        objects=list(bpy.context.scene.objects)
        assert all(obj.type=='MESH' for obj in objects),'Preview objects leaked into export'
        triangles=0
        bounds=[]
        for obj in objects:
            obj.data.calc_loop_triangles()
            triangles+=len(obj.data.loop_triangles)
            assert obj.data.materials and all(obj.data.materials)
            for vertex in obj.data.vertices:
                point=obj.matrix_world@vertex.co
                assert all(math.isfinite(n) for n in point)
                bounds.append(tuple(point))
        minimum=[min(v[i] for v in bounds) for i in range(3)]
        maximum=[max(v[i] for v in bounds) for i in range(3)]
        assert triangles==stats['triangles'],(triangles,stats['triangles'])
        assert all(abs(a-b)<.001 for a,b in zip(minimum,stats['bounds_min']))
        assert all(abs(a-b)<.001 for a,b in zip(maximum,stats['bounds_max']))
        assert minimum[2]>=-.01,'Unexpected below-ground geometry'
        assert len(objects)==stats['export_material_groups']
        rows.append({'model':blend.stem,'status':'PASS','triangles':triangles,
                     'material_groups':len(objects),'glb_bytes':glb.stat().st_size,
                     'glb_sha256':hashlib.sha256(glb.read_bytes()).hexdigest(),
                     'blend_sha256':hashlib.sha256(blend.read_bytes()).hexdigest(),
                     'reference_sha256':hashlib.sha256(reference.read_bytes()).hexdigest(),
                     'bounds_min_xyz_m':minimum,'bounds_max_xyz_m':maximum,
                     'checks':['Blend reload','Packed reference matches original','Metric units',
                               'GLB independent import','Triangle count preserved',
                               'Bounds preserved within 1 mm','No camera or light exported',
                               'Finite coordinates and assigned materials']})
        print('NERIS_VALIDATION',blend.stem,'PASS',triangles,flush=True)
    assert len(rows)==5,'Expected five buildings'
    (package/'validation.json').write_text(json.dumps({'blender':bpy.app.version_string,'models':rows},indent=2)+'\n')


if __name__=='__main__':
    main()
