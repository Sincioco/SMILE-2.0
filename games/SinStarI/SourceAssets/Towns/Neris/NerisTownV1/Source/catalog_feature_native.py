"""Generate data-only native accessors for template-local attached features."""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT.parents[5] / 'tools/Character3DViewer/TownCatalogFeatures.smile'


def generate():
    catalog = json.loads((ROOT / 'Authoring/catalog.json').read_text(encoding='utf-8'))
    f = lambda v: f'{v:.6f}'
    lines = ["''' Generated local entrance, collision and water data. No state ownership.",
        'Module Smile.Tools.TownCatalogFeatures', '', 'Option Explicit', '',
        'Import Smile.Simple3D.Precision3D As P',
        'Import Smile.Simple3D.CameraClearance3D As Clearance', '',
        'Public Type Box', '    X0 As Double', '    Z0 As Double',
        '    X1 As Double', '    Z1 As Double', '    Height As Double', 'End Type', '',
        'Public Type Door', '    Position As P.Vector3', '    Width As Double',
        '    Height As Double', '    Yaw As Double', 'End Type', '',
        'Public Type WaterDisk', '    Position As P.Vector3', '    RadiusX As Double',
        '    RadiusZ As Double', 'End Type', '']
    for name, key, typename in [('SolidAt', 'solids', 'Box'), ('StepAt', 'steps', 'Box'),
                                ('DoorAt', 'doors', 'Door'), ('WaterAt', 'water', 'WaterDisk'),
                                ('CameraAt', 'camera', 'Clearance.Bounds')]:
        lines += [f'Public Function {name}(TemplateIndex As Number, Index As Number) As {typename}', '',
                  f'    Dim Result As {typename}', '    Dim Key As Number', '',
                  '    Key = TemplateIndex * 256 + Index', '', '    Select Case Key']
        for template in catalog['templates']:
            assert len(template[key]) <= 256
            for index, value in enumerate(template[key]):
                lines += [f'        Case {template["id"]*256+index}']
                if key == 'camera':
                    x0, z0, y0, x1, z1, y1 = [v*10 for v in value]
                    lines.append(f'            Result = Clearance.Box({f(x0)}, {f(y0)}, {f(z0)}, {f(x1)}, {f(y1)}, {f(z1)})')
                elif key in ('solids', 'steps'):
                    for field, number in zip(('X0', 'Z0', 'X1', 'Z1', 'Height'), value):
                        lines.append(f'            Result.{field} = {f(number*10)}')
                else:
                    if key == 'doors':
                        m = value['matrix']
                        x, y, z = [m[i][3]*10 for i in range(3)]
                        right = math.hypot(m[0][0], m[1][0])
                        up = math.sqrt(sum(m[i][2]**2 for i in range(3)))
                        fields = {'Width': value['width']*right*10,
                                  'Height': value['height']*up*10,
                                  'Yaw': -math.degrees(math.atan2(m[1][0], m[0][0]))}
                    else:
                        x, y, z, rx, rz = [v*10 for v in value]
                        z += .4
                        fields = {'RadiusX': max(.01, rx-.15), 'RadiusZ': max(.01, rz-.15)}
                    lines.append(f'            Result.Position = P.Vector({f(x)}, {f(z)}, {f(y)})')
                    lines.extend(f'            Result.{field} = {f(number)}' for field, number in fields.items())
                lines.append('')
        lines += ['    End Select', '', '    Return Result', '', 'End Function', '']
    lines += ['End Module', '']
    OUTPUT.write_text('\n'.join(lines), encoding='utf-8')


if __name__ == '__main__':
    generate()
