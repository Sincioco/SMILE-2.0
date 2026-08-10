# Integration, Commit Plan, Validation, and Definition of Done

---

# 1. Expected files

## Platform Quest

```text
games\PlatformQuest\
    PlatformQuest.smileproj
    PlatformQuest.slnx
    Program.smile
    Program-NoDemo.smile
    README.md
    MAP_AUTHORING.md
    Maps\
        default.map
        custom.map
    Assets\
        Background.wav
        Start.wav
        Jump.wav
        Coin.wav
        Block.wav
        Stomp.wav
        Hurt.wav
        Goal.wav
        GameOver.wav
```

## Sky Hopper

```text
games\SkyHopper\
    SkyHopper.smileproj
    SkyHopper.slnx
    Program.smile
    Program-NoDemo.smile
    README.md
    Assets\
        Background.wav
        Start.wav
        Flap.wav
        Score.wav
        Hit.wav
        GameOver.wav
```

---

# 2. Project format

Follow current project conventions.

Platform Quest:

```xml
<SmileProject Version="1.0">
  <PropertyGroup>
    <ProjectKind>Game</ProjectKind>
    <StartupFile>Program.smile</StartupFile>
    <OutputName>PlatformQuest</OutputName>
    <GraphicsBackend>Auto</GraphicsBackend>
    <VSync>true</VSync>
  </PropertyGroup>
  <ItemGroup>
    <SmileSource Include="Program.smile" />
    <SmileSource Include="Program-NoDemo.smile" />
    <Asset Include="Assets\**\*" />
    <Asset Include="Maps\**\*" />
  </ItemGroup>
</SmileProject>
```

Sky Hopper is the same without the Maps asset entry.

---

# 3. Commit order

Use separate coherent commits.

Suggested:

```text
Sin and Codex: feat(game): add Platform Quest side-scrolling platformer

Sin and Codex: feat(game): add Sky Hopper obstacle-flight game

Sin and Codex: docs(games): integrate Platform Quest and Sky Hopper
```

The third commit is optional and should contain genuine final integration work.

Push each validated milestone.

---

# 4. Current documentation

Update current:

```text
AGENTS.md
README.md
docs\architecture\README.md
docs\language\README.md
```

Add both games to the actual current game count.

Add both to the `.smile` proof list.

Mention:

- Platform Quest external maps and random chunks;
- Sky Hopper procedural gates;
- both demo and no-demo sources;
- original generated audio.

Do not rewrite historical reports merely to change old game counts.

---

# 5. Sound generation

Extend:

```text
scripts\generate-sounds.ps1
```

Add original effects and melody loops.

The script must remain deterministic and fast.

Verify each output is a valid RIFF/WAVE file.

Do not add external dependencies.

---

# 6. Smoke integration

Update:

```text
scripts\smoke-test.cmd
scripts\verify-artifacts.ps1
```

Compile:

```text
PlatformQuest.exe
PlatformQuest-NoDemo.exe
SkyHopper.exe
SkyHopper-NoDemo.exe
```

Copy and verify all assets.

Run:

```text
scripts\validate-platform-quest-maps.ps1
```

Verify each executable is:

- x64;
- PE32+;
- Windows GUI subsystem;
- no CLR header.

Retain VSIX checks.

---

# 7. Short manual validation

## Platform Quest

DirectX:

1. Select and start default map.
2. Run, jump, and scroll camera.
3. Land on one-way platform.
4. collect coin;
5. break block;
6. use bonus block;
7. stomp enemy;
8. touch spike or fall once;
9. reach goal;
10. select custom map;
11. select random level;
12. rename one map and confirm fallback;
13. wait five seconds for demo;
14. cancel demo with one key.

GDI:

1. start default map;
2. move/jump/scroll;
3. verify geometry and HUD;
4. exit.

## Sky Hopper

DirectX:

1. start user game;
2. flap;
3. pass and score one gate;
4. collide and see user game over;
5. retry;
6. wait five seconds for demo;
7. observe AI gate passage;
8. cancel with one key.

GDI:

1. start;
2. flap through one gate;
3. verify geometry and HUD;
4. exit.

No long soak.

---

# 8. Definition of Done — Platform Quest

- [ ] Original project identity.
- [ ] Demo and genuine no-demo source.
- [ ] Fixed-step run/jump physics.
- [ ] Variable jump.
- [ ] Solid and one-way collision.
- [ ] Camera scrolling.
- [ ] Coins.
- [ ] Breakable and bonus blocks.
- [ ] Enemies and stomping.
- [ ] Hazards and lives.
- [ ] Goal and user victory.
- [ ] Score and persisted high score.
- [ ] Default/custom/random title choices.
- [ ] External map parser in SMILE.
- [ ] Random safe chunk fallback.
- [ ] Map authoring guide.
- [ ] Original music/effects.
- [ ] Demo direct return to title.
- [ ] Demo records isolated.
- [ ] DirectX/GDI checks.
- [ ] Both native artifacts verified.

---

# 9. Definition of Done — Sky Hopper

- [ ] Original project identity.
- [ ] Demo and genuine no-demo source.
- [ ] Fixed-step gravity/flap physics.
- [ ] Procedural gate generation/recycling.
- [ ] Safe random gaps.
- [ ] Collision.
- [ ] Scoring/high score.
- [ ] Increasing difficulty.
- [ ] Original visuals.
- [ ] Original music/effects.
- [ ] Demo AI.
- [ ] Demo direct return to title.
- [ ] Demo records isolated.
- [ ] No unnecessary map system.
- [ ] DirectX/GDI checks.
- [ ] Both native artifacts verified.

---

# 10. Final report

Report:

1. starting commit;
2. final commits;
3. branch pushed;
4. files added/changed;
5. confirmation no new SMILE syntax was required;
6. Platform Quest map format;
7. random chunk design;
8. Platform Quest demo AI;
9. Sky Hopper procedural gate design;
10. Sky Hopper demo AI;
11. background music format and generation;
12. sound assets;
13. executable paths;
14. smoke-suite result;
15. map validator result;
16. short DirectX/GDI results;
17. deferred subjective checks;
18. known limitations.
