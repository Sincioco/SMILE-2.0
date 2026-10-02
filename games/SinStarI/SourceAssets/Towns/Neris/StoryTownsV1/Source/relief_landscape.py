"""Neris Relief Quarter: level civic town with mirrored streets and neighbourhoods."""
from town_design import Town, ROAD


def build():
    town = Town('Neris Relief Quarter', 400, 4, True)
    town.night = False
    town.rect(-200, -200, 200, 200)
    main = town.path([(-200, 0), (200, 0)], 14)
    town.disk(0, 0, 38, ROAD)
    paths = [main]
    for side in (-1, 1):
        paths.append(town.path([(0, -125), (side*70, -125),
                               (side*70, 125), (0, 125)], 10))
        for template, z in ((0, -145), (4, -65), (1, 65), (5, 145)):
            town.place(template, side*115, z, 1.8, 90 if side == 1 else 270)
        town.place(8, side*34, 85, 1.5, 0)
        town.place(2, side*34, -92, 1.7, 180)
        town.place(20, side*34, -46, 1.3, 0)
        for z in (-150, -100, -50, 50, 100, 150):
            town.place(18, side*166, z, 2.3, 0)
        for z in (-168, 168):
            for x in (30, 70, 115):
                town.place(19, side*x, z, 2.1, 0)
        for x, z in ((25, 25), (25, -25), (52, 92), (52, -92)):
            town.place(14, side*x, z, 1.8, 90 if side == 1 else 270)
        town.lamps([(side*x, z) for x in (35, 95, 145) for z in (-13, 13)], 1.7)
        town.lamps([(side*58, z) for z in (-115, -55, 55, 115)], 1.7)
    town.path([(0, -125), (0, 70)], 10)
    town.place(9, 0, 100, 1.8, 0)
    town.gates(destinations=('Neris Town', 'Neris Waterworks'), width=14)
    main[0], main[-1] = (-196, 0), (196, 0)
    town.acceptance_paths = paths
    town.notes = [
        'Level symmetrical town rebuilt from clean source, with a central civic square and City Hall.',
        'Mirrored residential streets, shops, garden pavilions, fountains, benches, trees and lamps.',
        'Existing map-edge travel connections are retained; there are no mountain or desert districts.']
    return town
