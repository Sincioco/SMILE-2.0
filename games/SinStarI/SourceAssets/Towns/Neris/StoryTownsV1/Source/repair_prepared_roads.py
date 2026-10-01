"""Rebuild derived road records, preserving every authored byte and terrain record.

The old batch preparer reused generation 1 for different maps. This one-time
repair uses native collision/graph code to rebuild each edge and writes
new output files only. Live saves are repaired separately after Studio closes.
"""
import argparse
import hashlib
import struct
from pathlib import Path
import prepare_maps
from town_design import unwrap
from town_document_codec import envelope, prepared_records


REBUILD = '''
Sub RebuildRoad(Input As Text, Output As Text)

    Dim Job As Number
    Dim Ok As Boolean
    Dim Deadline As Number

    Job = Data_FileStart(False, "Repair.Source", Input)

    Call Await(Job)

    Ok = Store.LoadDocument(Town, "Repair.Source")

    Call Check(Ok, "Road Repair Source Loads")

    VerificationGeneration = VerificationGeneration + 1

    Call Graph.Adopt(Roads, Town, VerificationGeneration)

    Deadline = Timer() + 60000

    Do

        Call Graph.Prepare(Roads, Town, VerificationGeneration, 3)

    Loop Until Graph.Progress(Roads) = 100 Or Timer() > Deadline

    Call Check(Graph.Progress(Roads) = 100, "Road Repair Completes")
    Call Graph.Export(Roads, Town, "Repair.Output")

    Job = Data_FileStart(True, "Repair.Output.Connections", Output)

    Call Await(Job)

    Print "Rebuilt Roads " + Town.Name

End Sub
'''


def bundle(payload, records):
    output = bytearray(envelope(payload) + b'SMB1' + struct.pack('<I', len(records)))
    for name, record in records.items():
        key = name.encode('ascii')
        wrapped = envelope(record)
        output.extend(struct.pack('<II', len(key), len(wrapped)) + key + wrapped)
    return bytes(output) + hashlib.sha256(output).digest()


def repair(paths, output, work):
    output.mkdir(parents=True, exist_ok=True)
    work.mkdir(parents=True, exist_ok=True)
    calls, originals = [], []
    for index, path in enumerate(paths):
        raw = path.read_bytes()
        payload, records = unwrap(raw), prepared_records(raw)
        if '.Roads.Connections' not in records:
            continue
        source = work / f'{index}.town'
        road = work / f'{index}.roads'
        source.write_bytes(envelope(payload))
        calls.append(f'Call RebuildRoad("{source.resolve()}", "{road.resolve()}")')
        originals.append((path, raw, payload, records, road))
    prepare_maps.COMMON += REBUILD
    prepare_maps.run_native('RepairPreparedRoads', calls, work.resolve())
    for path, raw, payload, records, road in originals:
        rebuilt = unwrap(road.read_bytes())
        changed = rebuilt != records['.Roads.Connections']
        records['.Roads.Connections'] = rebuilt
        repaired = bundle(payload, records)
        assert unwrap(repaired) == payload and prepared_records(repaired) == records
        target = output / path.name
        if target.exists():
            raise ValueError('Choose a fresh output folder: ' + str(target))
        target.write_bytes(repaired if changed else raw)
        print(('Repaired ' if changed else 'Verified ') + path.name, flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--work', type=Path, required=True)
    args = parser.parse_args()
    repair(sorted(args.source.glob('*.town')), args.output, args.work)
