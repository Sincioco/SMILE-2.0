"""Three reusable residential models, matching the existing Neris material language."""
import math

import bpy

from geometry import Geometry, on_face
from details import opening, dome, column, crystal, planter, canopy, lantern, star


def create_homes(mats):
    result = {}
    for style, width, depth, height in [('Courtyard Cottage', 7.2, 6.2, 3.9),
                                      ('Family House', 8.6, 7.2, 6.3),
                                      ('Garden Pavilion', 7.0, 7.0, 4.5)]:
        g = Geometry(style, mats)
        g.box('House Foundation', (0, 0, .18), (width + .8, depth + .8, .36), 'stone')
        g.box('House Walls', (0, 0, height / 2 + .35), (width, depth, height), 'stone')
        g.masonry(width, depth, height, .35)
        for z in [.45, height + .4]:
            g.box('Continuous Carved Cornice', (0, 0, z), (width + .4, depth + .4, .3), 'trim')
            g.box('Continuous Gold Inlay', (0, 0, z + .15), (width + .45, depth + .45, .04), 'gold')
        for x in [-width / 2, width / 2]:
            for y in [-depth / 2, depth / 2]:
                column(g, x, y, height + .6, .42, has_crystal=False)
        opening(g, 0, depth / 2, 0, .38, 1.45, 2.5, True)
        canopy(g, 0, depth / 2, 0, 3.1, 3.0, .85)
        for side, plane, positions in [(0, depth / 2, [-2.35, 2.35]),
                                       (1, width / 2, [-1.9, 1.9]),
                                       (2, depth / 2, [-2, 2]),
                                       (3, width / 2, [-1.9, 1.9])]:
            for x in positions:
                opening(g, side, plane, x, 1.15, 1.0, 1.8)
                if style == 'Family House':
                    opening(g, side, plane, x, 4.3, 1.0, 1.6)
        if style == 'Garden Pavilion':
            dome(g, (0, 0, height + .7), 3.55, 2.45)
            crystal(g, (0, 0, height + 3.4), 1.0, .23)
        else:
            # Four sloped roof planes around a short horizontal ridge.
            w, d, z = width / 2 + .55, depth / 2 + .55, height + .65
            verts = [(-w, -d, z), (w, -d, z), (w, d, z), (-w, d, z),
                     (-w * .42, 0, z + 2), (w * .42, 0, z + 2)]
            g.mesh('Hipped Teal Roof', verts, [(0, 1, 5, 4), (1, 2, 5),
                                             (2, 3, 4, 5), (3, 0, 4)], 'roof')
            for a, b in [(0, 1), (1, 2), (2, 3), (3, 0), (0, 4),
                         (3, 4), (1, 5), (2, 5), (4, 5)]:
                g.beam('Gold Roof Seam', verts[a], verts[b], .047, 'gold')
            for i in range(-7, 8):
                x = i * w / 8
                ridge_x = max(-w * .42, min(w * .42, x))
                for sign in [-1, 1]:
                    g.beam('Roof Rib', (x, sign * d, z + .025),
                           (ridge_x, 0, z + 2.025), .026, 'gold')
            g.box('Stone Chimney', (-2.6, 1.8, z + 1.25), (.65, .8, 2.4), 'trim')
            g.box('Chimney Cap', (-2.6, 1.8, z + 2.5), (.95, 1.1, .18), 'roof')
        lantern(g, 0, depth / 2, -1.2, 2.4)
        for x in [-2.5, 2.5]:
            planter(g, (x, -depth / 2 - .7, .1), .8)
        if style == 'Family House':
            g.box('Balcony Deck', (0, -depth / 2 - .4, 4), (3.7, 1.3, .22), 'trim')
            g.beam('Balcony Handrail', (-1.7, -depth / 2 - 1, 4.9),
                   (1.7, -depth / 2 - 1, 4.9), .07, 'gold')
            for i in range(9):
                x = -1.7 + i * 3.4 / 8
                g.beam('Balcony Spindle', (x, -depth / 2 - 1, 4.1),
                       (x, -depth / 2 - 1, 4.9), .036, 'iron')
        # Keep templates embedded but unlinked; visible placements are instances.
        bpy.context.scene.collection.children.unlink(g.collection)
        result[style] = g.collection
    return result
