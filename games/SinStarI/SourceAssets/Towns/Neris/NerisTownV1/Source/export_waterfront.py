"""Derive native assets only after accepting the saved Blender waterfront."""
from pathlib import Path
import runpy
import bpy
folder=Path(__file__).resolve().parent
assert Path(bpy.data.filepath).name=='Neris-Town-Waterfront.blend'
for script in ('export_entrances.py','export_native.py','export_layout.py','export_camera.py','export_site.py'):
    print('EXPORT STAGE',script,flush=True)
    runpy.run_path(str(folder/script),run_name='__main__')
