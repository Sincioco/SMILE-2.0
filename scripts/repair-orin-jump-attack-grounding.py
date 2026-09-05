"""Ground Orin's accepted JumpAttack impact without changing other asset data."""

import argparse
import copy
import hashlib
import json
import struct
from pathlib import Path


ACCEPTED_INPUT_SHA256 = "84b55e0ec83746a0188a473102f73377e63c3e9f15f04b597cf3daba6b78ddcf"
IMPACT_SAMPLE = 37
IDLE_MINIMUM_Y_1000 = 3
JUMP_MINIMUM_Y_1000 = [
    -34, -34, -66, -110, -131, -137, -123, -95, -51, 10, 14, 13,
    26, 9, -38, -99, -131, -131, -106, -62, 3, 84, 149, 193, 195,
    197, 193, 186, 188, 179, 86, 14, -36, -46, -7, -12, -10, -2,
    79, 136, 193, 211, 229, 249, 281, 292, 290, 287, 285, 284, 283,
    282, 282, 272, 255, 233, 209, 184, 156, 127, 105, 82, 65, 61,
    42, 27, 21, 10, 7, 5, 4, 3,
]


def read_glb(path: Path):
    raw = path.read_bytes()
    json_length = struct.unpack_from("<I", raw, 12)[0]
    document = json.loads(raw[20 : 20 + json_length])
    binary = bytearray(raw[28 + json_length :])
    return raw, document, binary


def accessor_values(document, binary, index):
    accessor = document["accessors"][index]
    view = document["bufferViews"][accessor["bufferView"]]
    width = {"SCALAR": 1, "VEC3": 3}[accessor["type"]]
    offset = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
    stride = view.get("byteStride", width * 4)
    return [
        struct.unpack_from("<" + "f" * width, binary, offset + row * stride)
        for row in range(accessor["count"])
    ]


def sample_linear(times, rows, time):
    upper = next((index for index, value in enumerate(times) if value >= time), len(times) - 1)
    lower = max(0, upper - 1)
    span = max(0.000000001, times[upper] - times[lower])
    blend = max(0.0, min(1.0, (time - times[lower]) / span))
    return tuple(
        rows[lower][axis] * (1.0 - blend) + rows[upper][axis] * blend
        for axis in range(len(rows[lower]))
    )


def add_accessor(document, binary, rows, kind):
    while len(binary) % 4:
        binary.append(0)
    offset = len(binary)
    binary.extend(
        struct.pack(
            "<" + "f" * sum(len(row) for row in rows),
            *(value for row in rows for value in row),
        )
    )
    view_index = len(document["bufferViews"])
    document["bufferViews"].append(
        {"buffer": 0, "byteOffset": offset, "byteLength": len(binary) - offset}
    )
    accessor_index = len(document["accessors"])
    document["accessors"].append(
        {
            "bufferView": view_index,
            "componentType": 5126,
            "count": len(rows),
            "type": kind,
        }
    )
    return accessor_index


def write_glb(document, binary):
    document["buffers"][0]["byteLength"] = len(binary)
    encoded = json.dumps(document, separators=(",", ":")).encode()
    encoded += b" " * (-len(encoded) % 4)
    binary += b"\0" * (-len(binary) % 4)
    return (
        struct.pack("<III", 0x46546C67, 2, 28 + len(encoded) + len(binary))
        + struct.pack("<II", len(encoded), 0x4E4F534A)
        + encoded
        + struct.pack("<II", len(binary), 0x004E4942)
        + binary
    )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--model", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    arguments = parser.parse_args()
    if arguments.model.resolve() == arguments.output.resolve():
        raise ValueError("Use a separate review output before promoting the repaired model")

    raw, document, binary = read_glb(arguments.model)
    input_hash = hashlib.sha256(raw).hexdigest()
    if input_hash != ACCEPTED_INPUT_SHA256:
        raise ValueError(f"Unexpected accepted Orin checkpoint: {input_hash}")

    before = copy.deepcopy(document)
    root = next(index for index, node in enumerate(document["nodes"]) if node.get("name") == "Root")
    animation = next(item for item in document["animations"] if item["name"] == "JumpAttack")
    translation_channel = next(
        channel
        for channel in animation["channels"]
        if channel["target"]["node"] == root and channel["target"]["path"] == "translation"
    )
    translation_sampler = animation["samplers"][translation_channel["sampler"]]
    source_times = [row[0] for row in accessor_values(document, binary, translation_sampler["input"])]
    source_rows = accessor_values(document, binary, translation_sampler["output"])
    duration = max(
        accessor_values(document, binary, sampler["input"])[-1][0]
        for sampler in animation["samplers"]
    )
    times = [min(duration, sample / 30.0) for sample in range(round(duration * 30) + 1)]
    if len(times) != len(JUMP_MINIMUM_Y_1000):
        raise ValueError(f"Unexpected JumpAttack sample count: {len(times)}")

    translations = []
    for sample, time in enumerate(times):
        translation = list(sample_linear(source_times, source_rows, time))
        if sample >= IMPACT_SAMPLE:
            translation[1] += (IDLE_MINIMUM_Y_1000 - JUMP_MINIMUM_Y_1000[sample]) / 1000.0
        translations.append(tuple(translation))

    time_accessor = add_accessor(document, binary, [(time,) for time in times], "SCALAR")
    document["accessors"][time_accessor].update(min=[min(times)], max=[max(times)])
    translation_accessor = add_accessor(document, binary, translations, "VEC3")
    animation["channels"].remove(translation_channel)
    sampler_index = len(animation["samplers"])
    animation["samplers"].append(
        {"input": time_accessor, "output": translation_accessor, "interpolation": "LINEAR"}
    )
    animation["channels"].append(
        {"sampler": sampler_index, "target": {"node": root, "path": "translation"}}
    )

    for key in ("nodes", "meshes", "skins", "materials", "images", "textures"):
        if document.get(key) != before.get(key):
            raise AssertionError(f"Unexpected {key} mutation")
    if [item for item in document["animations"] if item["name"] != "JumpAttack"] != [
        item for item in before["animations"] if item["name"] != "JumpAttack"
    ]:
        raise AssertionError("A non-JumpAttack animation changed")

    output = write_glb(document, binary)
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_bytes(output)
    report = {
        "beforeSha256": input_hash.upper(),
        "afterSha256": hashlib.sha256(output).hexdigest().upper(),
        "clip": "JumpAttack",
        "impactSample": IMPACT_SAMPLE,
        "sampleRate": 30,
        "idleReferenceMinimumY1000": IDLE_MINIMUM_Y_1000,
        "policy": (
            "Preserve the accepted launch and all non-JumpAttack data; move only the Root "
            "translation from impact through the final get-up to the accepted Idle floor."
        ),
    }
    arguments.output.with_suffix(".repair.json").write_text(
        json.dumps(report, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(report))


if __name__ == "__main__":
    main()
