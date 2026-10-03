"""Quarter-turn an authored map about its own center, including native landmarks."""
from copy import deepcopy


def rotate(document, degrees):
    assert degrees % 90 == 0
    doc = deepcopy(document)
    turns = (degrees // 90) % 4
    cx = (doc['xs'][0] + doc['xs'][-1]) / 2
    cz = (doc['zs'][0] + doc['zs'][-1]) / 2

    def point(x, z):
        for _ in range(turns):
            x, z = cx + z - cz, cz - x + cx
        return [x, z]

    def array(values, width, height):
        for _ in range(turns):
            values = [values[z * width + x] for x in range(width-1, -1, -1)
                      for z in range(height)]
            width, height = height, width
        return values

    columns, rows = doc['columns'], doc['rows']
    for key in ('cells', 'base_cells', 'appearance'):
        if key in doc:
            doc[key] = array(doc[key], columns, rows)
    if 'heights' in doc:
        doc['heights'] = array(doc['heights'], columns+1, rows+1)
    corners = [point(x, z) for x in doc['xs'] for z in (doc['zs'][0], doc['zs'][-1])]
    corners += [point(x, z) for z in doc['zs'] for x in (doc['xs'][0], doc['xs'][-1])]
    doc['xs'] = sorted({p[0] for p in corners})
    doc['zs'] = sorted({p[1] for p in corners})
    doc['columns'], doc['rows'] = len(doc['xs'])-1, len(doc['zs'])-1
    for tile in doc.get('map_tiles', []):
        x, z, w, h = tile['x'], tile['z'], columns, rows
        for _ in range(turns):
            x, z, w, h = z, w-1-x, h, w
        tile['x'], tile['z'] = x, z
    for item in doc['items']:
        item['position'][0], item['position'][2] = point(item['position'][0], item['position'][2])
        item['yaw'] = (item['yaw'] + degrees) % 360
    for brush in doc.get('curves', []):
        brush[2:4] = point(*brush[2:4])
        if brush[0] in (1, 4, 5, 7, 8):
            brush[4:6] = point(*brush[4:6])
        if brush[0] == 1:
            brush[2], brush[4] = sorted((brush[2], brush[4]))
            brush[3], brush[5] = sorted((brush[3], brush[5]))
        if brush[0] in (7, 8):
            brush[7:9] = point(*brush[7:9])
        if brush[0] == 6 and turns % 2:
            brush[4], brush[5] = brush[5], brush[4]
    if doc.get('teleport_spawn') is not None:
        doc['teleport_spawn'] = point(*doc['teleport_spawn'])
    doc['landmark_rotation'] = (doc.get('landmark_rotation', 0) + degrees) % 360
    return doc
