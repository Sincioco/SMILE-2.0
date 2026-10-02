"""Bake portable towns with Studio's native save owner, then verify a fresh import.

Uses the repository compiler and Windows runtime; no renderer or external packages.
"""
import argparse
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
from town_design import ROOT


COMMON = '''Option Explicit

Import Smile.Tools.TownDocument As Document
Import Smile.Tools.TownDocumentStore As Store
Import Smile.Tools.TownSavePreparation As Preparation
Import Smile.Tools.TownDerivedCache As Derived
Import Smile.Tools.TownTerrainCache As Cache
Import Smile.Tools.TownRoadGraph As Graph

Dim Town As Document.State
Dim Work As Preparation.State
Dim Batch As Cache.Batch
Dim Roads As Graph.State
Dim Failed As Boolean
Dim PreparationGeneration As Number
Dim VerificationGeneration As Number

Game Window "Prepare Saved Towns" Size 400 By 200

Sub Check(Ok As Boolean, Label As Text)

    If Not Ok Then
        Print "FAIL " + Label
        Failed = True
    End If

End Sub

Sub Await(Job As Number)

    Dim Limit As Number
    Dim Status As Number

    Limit = Timer() + 20000

    Do
        Status = Data_FileStatus(Job)
        Show Screen

    Loop Until Status <> 0 Or Timer() > Limit

    Call Check(Status = 1, "File Transfer")

End Sub

Sub Prepare(Input As Text, Output As Text)

    Dim Job As Number
    Dim Ok As Boolean
    Dim Deadline As Number

    Job = Data_FileStart(False, "Source", Input)

    Call Await(Job)

    Ok = Store.LoadDocument(Town, "Source")

    Call Check(Ok, "Source Loads")

    Ok = Store.SaveDocument(Town, "Snapshot", False)

    Call Check(Ok, "Snapshot Saved")

    PreparationGeneration = PreparationGeneration + 1

    Call Preparation.Begin(Work, Town, "Snapshot", PreparationGeneration)

    Deadline = Timer() + 60000

    Do

        Call Preparation.Update(Work, Town, PreparationGeneration)

        Show Screen

    Loop Until Work.Stage = 0 Or Work.Failed Or Timer() > Deadline

    Call Check(Work.Stage = 0 And Not Work.Failed, "Save Preparation")

    Job = Data_BundleStart(True, "Snapshot", Output, Work.Records)

    Call Await(Job)

    Print "Prepared " + Town.Name

End Sub

Sub Verify(Input As Text)

    Dim Job As Number
    Dim Ok As Boolean
    Dim Key As Text
    Dim Index As Number

    ' One immutable import key per town prevents any earlier records masking gaps.
    Job = Data_BundleStart(False, Input, Input)

    Call Await(Job)

    Ok = Store.LoadDocument(Town, Input)

    Call Check(Ok, "Prepared Town Loads")

    Ok = Derived.BindBundle(Town, Input)

    Call Check(Ok, "Imported Preparation Binds")

    Key = Derived.ReadKey(Town, "Terrain9", False)

    Call Check(Text_Length(Key) > 0, "Terrain Is Prepared")

    For Index = 0 To Derived.ReadNumber(Key + ".Batches") - 1
        Ok = Cache.LoadBatch(Batch, Key, Index)

        Call Check(Ok, "Terrain Page Valid")
    End For

    VerificationGeneration = VerificationGeneration + 1

    Call Graph.Adopt(Roads, Town, VerificationGeneration)
    Call Check(Graph.Progress(Roads) = 100 And Graph.CollisionChecks(Roads) = 0,
        "Roads Need No Preparation")

    Print "Verified " + Town.Name

End Sub
'''


def run_native(label, calls, work):
    viewer = ROOT / 'tools/Character3DViewer'
    source = work / (label + '.smile')
    source.write_text(COMMON + '\n' + '\n'.join(calls) + '''
If Not Failed Then
    Print "PASS Prepared Towns"
End If
''', encoding='utf-8')
    tree = ET.parse(viewer / 'TownEditorTests.smileproj')
    props = tree.find('PropertyGroup')
    props.find('ProjectKind').text = 'Game'
    props.find('StartupFile').text = str(source)
    props.find('ApplicationId').text = 'smile.town-preparation.run' + os.urandom(8).hex()
    for node in tree.findall('ItemGroup/*'):
        if node.get('Include'):
            include = source if node.get('StartupOnly') == 'true' else viewer / node.get('Include').replace('\\', '/')
            node.set('Include', str(include.resolve()))
    project, exe = work / (label + '.smileproj'), work / (label + '.exe')
    tree.write(project, encoding='utf-8')
    compiler = ROOT / 'artifacts/compiler/smilec.exe'
    result = subprocess.run([str(compiler), '--project', str(project), '--target',
                             'windows-x64', '-o', str(exe)], capture_output=True, text=True)
    (work / (label + '.compile.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    result = subprocess.run([str(exe)], cwd=work, capture_output=True, text=True,
                            startupinfo=startup, timeout=max(60, len(calls) * 65))
    (work / (label + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    print(result.stdout, flush=True)
    if result.returncode or 'FAIL' in result.stdout or 'PASS Prepared Towns' not in result.stdout:
        raise RuntimeError('Native preparation failed; see ' + str(work))


def prepare_maps(source, output, work):
    paths = sorted(source.resolve().glob('*.town'))
    if not paths:
        raise ValueError('No town files found')
    if source.resolve() == output.resolve():
        raise ValueError('Use a separate output folder to retain the source revision')
    output.mkdir(parents=True, exist_ok=True)
    work.mkdir(parents=True, exist_ok=True)
    run_native('Prepare', [f'Call Prepare("{p}", "{output.resolve() / p.name}")' for p in paths], work.resolve())
    run_native('Verify', [f'Call Verify("{output.resolve() / p.name}")' for p in paths], work.resolve())


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--work', type=Path, required=True)
    args = parser.parse_args()
    prepare_maps(args.source, args.output, args.work)
