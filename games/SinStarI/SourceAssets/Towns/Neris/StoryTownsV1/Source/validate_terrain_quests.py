"""Focused production-native route, ground and flow acceptance for the two new maps."""
import argparse
import json
from pathlib import Path
import prepare_maps


CHECKS = '''
Sub CheckTerrain()

    Dim Index As Number
    Dim Sample As Terrain.Sample
    Dim Ok As Boolean

    Call Navigation.Rebuild(Town)

    For Index = 0 To Town.ItemCount - 1
        Sample = Terrain.SamplePoint(Town.Surface, Town.Items[Index].Position.X,
            Town.Items[Index].Position.Z)

        Call Check(Sample.Supported And
            Abs(Town.Items[Index].Position.Y - Sample.Offset - 23.0) < 0.011,
            "Prop Grounding")
    End For

    For Index = 0 To Town.Surface.Curves.BrushCount - 1

        If Town.Surface.Curves.Brushes[Index].WaterMode = 1 Then
            Ok = Not Water.Uphill(Town.Surface, Town.Surface.Curves.Brushes[Index])

            Call Check(Ok, "Stream Is Downhill Throughout")
        End If

    End For

End Sub

Sub CheckJourney(X0 As Double, Z0 As Double, X1 As Double, Z1 As Double)

    Dim Leader As P.Vector3
    Dim Goal As P.Vector3
    Dim Direction As P.Vector3
    Dim NextPoint As P.Vector3
    Dim Tick As Number
    Dim Ok As Boolean

    Leader = P.Vector(X0, Navigation.GroundHeight(X0, Z0), Z0)
    Goal = P.Vector(X1, Navigation.GroundHeight(X1, Z1), Z1)
    Ok = Travel.Begin(Town, Leader, Goal)

    Call Check(Ok, "Journey Begins: " + Travel.Message())

    For Tick = 1 To 12000

        If Not Travel.Active() Then
            Exit For
        End If

        Direction = Travel.Direction(Town, Leader, 2.0, 16)
        NextPoint = P.Vector(Leader.X + Direction.X * 2.0, 0.0,
            Leader.Z + Direction.Z * 2.0)
        Ok = Navigation.CanTraverse(Leader, NextPoint)

        Call Check(Ok, "Journey Movement Is Traversable")

        If Not Ok Then
            Exit For
        End If

        NextPoint.Y = Navigation.GroundHeight(NextPoint.X, NextPoint.Z)
        Leader = NextPoint
    End For

    Call Check(Travel.Message() = "Destination reached.",
        "Journey Completes: " + Travel.Message())

    Print "Journey " + Text_From_Double(X0) + "," + Text_From_Double(Z0) + " -> " + Text_From_Double(X1) + "," + Text_From_Double(Z1)

End Sub

Sub CheckSegment(X0 As Double, Z0 As Double, X1 As Double, Z1 As Double)

    Dim First As P.Vector3
    Dim Last As P.Vector3
    Dim Length As Double
    Dim Dx As Double
    Dim Dz As Double
    Dim Side As Number
    Dim Step As Number
    Dim SampleCount As Number
    Dim Ok As Boolean
    Dim High As Double
    Dim Low As Double

    Length = Sqrt((X1 - X0) * (X1 - X0) + (Z1 - Z0) * (Z1 - Z0))
    Dx = -(Z1 - Z0) / Length
    Dz = (X1 - X0) / Length
    SampleCount = Max(1, ToNumber(Length / 2.0))
    Ok = True
    Low = 10000.0

    For Side = -1 To 1

        First = P.Vector(X0 + Dx * ToDouble(Side) * 20.0, 0.0,
            Z0 + Dz * ToDouble(Side) * 20.0)

        For Step = 1 To SampleCount
            Last = P.Vector(X0 + (X1 - X0) * ToDouble(Step) / ToDouble(SampleCount) + Dx * ToDouble(Side) * 20.0,
                0.0, Z0 + (Z1 - Z0) * ToDouble(Step) / ToDouble(SampleCount) + Dz * ToDouble(Side) * 20.0)
            Last.Y = Navigation.GroundHeight(Last.X, Last.Z)
            High = Max(High, Last.Y)
            Low = Min(Low, Last.Y)
            Ok = (Navigation.CanStand(Last.X, Last.Z) And
                Navigation.CanTraverse(First, Last) And Navigation.CanTraverse(Last, First) And Ok)
            First = Last
        End For
    End For

    Call Check(Ok, "Four Metre Usable Route Both Directions")

    Print ("Route " + Text_From_Double(X0) + "," + Text_From_Double(Z0) + " -> " + Text_From_Double(X1) + "," + Text_From_Double(Z1) +
        " heights " + Text_From_Double(Low) + " .. " + Text_From_Double(High))

End Sub
'''


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--maps', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--work', type=Path, required=True)
    args = parser.parse_args()
    args.work.mkdir(parents=True, exist_ok=True)
    prepare_maps.COMMON = prepare_maps.COMMON.replace('Dim Town As Document.State', '''
Import Smile.Tools.TownDocumentNavigation As Navigation
Import Smile.Tools.TownRoadTravel As Travel
Import Smile.Simple3D.TerrainSurface3D As Terrain
Import Smile.Simple3D.TerrainWater3D As Water
Import Smile.Simple3D.Precision3D As P

Dim Town As Document.State''') + CHECKS
    calls = []
    for record in json.loads(args.manifest.read_text()):
        calls += [f'Call Verify("{args.maps.resolve()/record["file"]}")', 'Call CheckTerrain()']
        summit = (-132, -45) if record['name'] == 'Willowstep Highlands' else (144, -54)
        x, z = summit
        calls += [f'Call CheckJourney(0.0, 0.0, {x*10}.0, {z*10}.0)',
                  f'Call CheckJourney({x*10}.0, {z*10}.0, 0.0, 0.0)']
        for route in record['acceptance_paths']:
            for (x0, z0), (x1, z1) in zip(route, route[1:]):
                calls.append(f'Call CheckSegment({x0*10}.0, {z0*10}.0, {x1*10}.0, {z1*10}.0)')
                calls.append(f'Call CheckJourney({x0*10}.0, {z0*10}.0, {x1*10}.0, {z1*10}.0)')
    prepare_maps.run_native('QuestTerrainRoutes', calls, args.work.resolve())
