"""Build a separate Luma atlas; preserve the user's original Luma world."""
import json
from pathlib import Path
from town_design import atomic_write, unwrap


def integer(value):
    return int(value).to_bytes(3, 'little')


def text(value):
    return integer(len(value)) + b''.join(integer(ord(c)) for c in value)


def build(folder):
    graph = json.loads((folder / 'world-layout.json').read_text(encoding='utf-8'))
    nodes = graph['nodes']
    links = {tuple(pair) for pair in graph['links']}
    ports = {(a,b):port for a,b,port in graph.get('ports', [])}
    payload = integer(2) + text(graph['name']) + integer(len(nodes))
    for name, x, y in nodes:
        payload += text(name) + integer(x) + integer(y)
    for i, (_, x, y) in enumerate(nodes):
        for j, (_, tx, ty) in enumerate(nodes):
            linked = tuple(sorted((i, j))) in links
            port = (2 if tx > x else 0) if abs(tx-x) >= abs(ty-y) else (3 if ty > y else 1)
            port = ports.get((i,j), port)
            payload += integer(linked) + integer(port if linked else 0)
    path = folder / 'Luma - Story Atlas.world'
    atomic_write(path, payload)
    assert unwrap(path.read_bytes()) == payload
    print(f'Built {path.name}: {len(nodes)} maps, {len(links)} connections')


if __name__ == '__main__':
    build(Path(__file__).resolve().parent.parent)
