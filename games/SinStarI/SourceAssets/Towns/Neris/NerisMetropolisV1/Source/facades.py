"""Distinct blue-glass skins, shared by Blender and the native GLB publisher."""
import math
from pathlib import Path
import bpy

# Pane proportions and framing differ; this is not one grid recolored per tower.
STYLES = {
    'Glacier': ((.23,.49,.66), 8, 4, .26, .07),
    'Azure': ((.065,.28,.48), 5, 4, .58, .12),
    'Sapphire': ((.025,.14,.30), 4, 6, .68, .16),
    'Cerulean': ((.11,.39,.53), 8, 3, .31, .06),
    'SilverBlue': ((.32,.49,.60), 4, 4, .48, .09),
    'Midnight': ((.035,.09,.19), 6, 4, .62, .16),
}

ASSIGNMENTS = {
    'Burj Khalifa':'SilverBlue', 'Shanghai Tower':'Glacier',
    'Petronas Twin Towers':'SilverBlue', 'Marina Bay Sands':'Cerulean',
    'Jeddah Tower':'Azure', 'Shanghai World Financial Center':'Sapphire',
    'Merdeka 118':'Glacier', 'Makkah Royal Clock Tower':'Azure', 'Ping An Finance Center':'SilverBlue',
    'Lotte World Tower':'Glacier', 'One World Trade Center':'Azure',
    'Guangzhou CTF Finance Centre':'Cerulean', 'Tianjin CTF Finance Centre':'Glacier',
    'CITIC Tower':'Azure', 'Taipei 101':'Cerulean', 'Jin Mao Tower':'SilverBlue',
    'Oriental Pearl Tower':'Sapphire', 'Luma Crown Spire':'Midnight',
    'Neris Tide Arcology':'Cerulean', 'Starweave Civic Hall':'Azure',
    'Aether Gate Observatory':'Glacier', 'Cloud Forest':'Glacier', 'Flower Dome':'Glacier',
}


def generate():
    folder = Path(__file__).resolve().parents[1] / 'Textures'
    folder.mkdir(exist_ok=True)
    for name, (base, columns, rows, metal, rough) in STYLES.items():
        image = bpy.data.images.get('Metropolis ' + name)
        if not image:
            image = bpy.data.images.new('Metropolis ' + name, width=256, height=256)
        pixels = []
        for y in range(256):
            for x in range(256):
                u, v = x*columns/256, y*rows/256
                column, row = int(u), int(v)
                seam = u-column < .045 or v-row < .055
                tint = .92 + .07*math.sin(column*3+row*7)
                # Broad reflection bands avoid noisy mottling and wavy grids.
                shine = .045 * math.cos(x/256*math.tau) + .03*(v-row)
                rgb = tuple(c*.52 for c in base) if seam else tuple(max(0,c*tint+shine) for c in base)
                pixels.extend((*rgb,1))
        image.pixels.foreach_set(pixels)
        image.filepath_raw = str(folder / (name + '-Glass.png'))
        image.file_format = 'PNG'
        image.save()


def skin(mesh):
    style = ASSIGNMENTS.get(mesh.name, 'Azure')
    mesh.colors = [('Glass ' + style) if color == 'Glass' else color for color in mesh.colors]
    return mesh
