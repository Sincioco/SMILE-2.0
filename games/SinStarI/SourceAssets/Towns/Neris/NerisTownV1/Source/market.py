"""Small lived-in details for the service courts and residential gardens."""
import math

from geometry import Geometry
from details import bottle, barrel, crate, crystal
from props import instance


def dress_town(mats, stock, landscape, street):
    stall = Geometry('Market Stall Template', mats)
    for x in [-1.8, 1.8]:
        for y in [-.95, .95]:
            stall.box('Stall Corner Post', (x, y, 1.4), (.12, .12, 2.8), 'wood')
    stall.box('Market Counter', (0, -.7, .92), (3.8, .72, .2), 'woodalt')
    stall.box('Counter Front', (0, -.95, .50), (3.7, .1, .75), 'wood')
    for i in range(8):
        x = -2 + i * .5
        stall.mesh('Striped Fabric Awning', [(x, -1.25, 2.55), (x + .5, -1.25, 2.55),
                   (x + .5, 0, 3.15), (x, 0, 3.15),
                   (x, 1.25, 2.55), (x + .5, 1.25, 2.55)],
                   [(0, 1, 2, 3), (3, 2, 5, 4)], 'cloth' if i % 2 else 'trim')
    for x in [-2, 2]:
        stall.beam('Awning Edge', (x, -1.25, 2.55), (x, 0, 3.15), .04, 'gold')
        stall.beam('Awning Edge', (x, 0, 3.15), (x, 1.25, 2.55), .04, 'gold')
    for i in range(10):
        bottle(stall, (-1.3 + i * .28, -.70, 1.03), blue=i % 2 == 0)
    crate(stall, (1, .3, 0))
    barrel(stall, (-1, .3, 0), .8)
    import bpy
    bpy.context.scene.collection.children.unlink(stall.collection)
    for i, (x, y) in enumerate([(-28, -3), (-40, -4)]):
        instance(stall.collection, street.collection, f'Neighborhood Market Stall {i+1}',
                 (x, y, .20), 0)
    # Low private garden boundaries leave each house's street-facing entrance open.
    for y in [-33, -14, 6, 25]:
        for yy in [y - 6.7, y + 6.7]:
            landscape.box('Residential Garden Curb', (-35, yy, .33), (12, .3, .48), 'trim')
            landscape.box('Clipped Garden Hedge', (-35, yy, .75), (11.5, .55, .62), 'leaf', .12)
        for x in [-40, -28]:
            instance(stock['Flower Bed'], landscape.collection, 'Residential Flower Bed',
                     (x, y - 4.8, .12), scale=.65)
    for x, y in [(-26, 14), (25, 14), (25, -10)]:
        street.cylinder('Wayfinding Post', (x, y, 1.5), .10, 3, 'gold', 12)
        street.box('Wayfinding Board', (x, y, 2.6), (2.6, .14, .55), 'cloth')
        street.text('Wayfinding Lettering', 'CIVIC PLAZA' if y > 0 else 'MARKET WALK',
                    (x, y - .10, 2.6), .20)
    # Crystal pylons identify the arrival/safe-point circle without inventing gameplay.
    for i in range(3):
        a = math.pi / 2 + i * math.tau / 3
        x, y = -22 + 3.35 * math.cos(a), -22 + 3.35 * math.sin(a)
        street.lathe('Wayfarer Pylon', [(.45, 0), (.30, 1.2), (.22, 2.0)], (x, y, .4), 'trim', 8)
        crystal(street, (x, y, 2.4), 1.05, .21)
