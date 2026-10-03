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
    payload = integer(2) + text(graph['name']) + integer(len(nodes))
    for name, x, y in nodes:
        payload += text(name) + integer(x) + integer(y)
    # Layout only. Studio derives directed connections from authored yellow areas.
    for _ in nodes:
        for _ in nodes:
            payload += integer(0) + integer(0)
    path = folder / 'Luma - Story Atlas.world'
    atomic_write(path, payload)
    assert unwrap(path.read_bytes()) == payload
    print(f'Built {path.name}: {len(nodes)} maps, connections derived from town documents')


if __name__ == '__main__':
    build(Path(__file__).resolve().parent.parent)
