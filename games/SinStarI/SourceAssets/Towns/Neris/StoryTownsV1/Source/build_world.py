"""Build a separate Luma atlas; preserve the user's original Luma world."""
import json
from pathlib import Path
from town_design import atomic_write, unwrap


def integer(value):
    return int(value).to_bytes(3, 'little')


def text(value):
    return integer(len(value)) + b''.join(integer(ord(c)) for c in value)


def build(folder):
    nodes = [
        ('Neris Spaceport', 80, 150), ('Neris Town', 500, 150),
        ('Horizon Airport', 920, 150), ('Neris Canals', 80, 520),
        ('Neris Star Lake', 500, 520), ('Neris Crown Isles', 920, 520),
        ('East Valley', 250, 920), ("Orin's Village", 750, 920)]
    links = set()
    for i in range(1, len(nodes)):
        if i != 2:
            links.add(tuple(sorted((0, i))))
            links.add(tuple(sorted((2, i))))
    links.add((0, 2))
    payload = integer(2) + text('Luma - Story Atlas') + integer(len(nodes))
    for name, x, y in nodes:
        payload += text(name) + integer(x) + integer(y)
    for i, (_, x, y) in enumerate(nodes):
        for j, (_, tx, ty) in enumerate(nodes):
            linked = tuple(sorted((i, j))) in links
            port = (2 if tx > x else 0) if abs(tx-x) >= abs(ty-y) else (3 if ty > y else 1)
            payload += integer(linked) + integer(port if linked else 0)
    path = folder / 'Luma - Story Atlas.world'
    atomic_write(path, payload)
    assert unwrap(path.read_bytes()) == payload
    (folder / 'world-layout.json').write_text(json.dumps(dict(
        name='Luma - Story Atlas', nodes=nodes, links=sorted(links)), indent=2)+'\n', encoding='utf-8')
    print(f'Built {path.name}: {len(nodes)} maps, {len(links)} connections')


if __name__ == '__main__':
    build(Path(__file__).resolve().parent.parent)
