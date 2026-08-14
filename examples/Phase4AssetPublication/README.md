# Phase4AssetPublication

Run `CreateFixtureAssets.ps1` to create a deterministic asset tree.

Open `Phase4AssetPublication.slnx` for the normal Visual Studio Windows/Web platform selector.

The project deliberately combines one explicit file, one nonrecursive `*.png` pattern, one recursive `**/*.wav`
pattern, one empty wildcard, and one overlapping explicit include.

The expected resolved and published paths are listed in `ExpectedAssetPaths.txt`. Files deliberately present but
excluded include `Assets/UI/Click.wav`, `Assets/UI/Sub/Nested.png`, `Assets/Audio/Sub/Notes.txt`, and
`Assets/Unlisted/Secret.txt`.
