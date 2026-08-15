# Phase 2 Validation Matrix and Definition of Done

## A. Syntax

| ID | Requirement |
|---|---|
| A1 | Module/End Module parse case-insensitively |
| A2 | dotted module names parse |
| A3 | Import module As alias parses |
| A4 | Public/Private declarations parse |
| A5 | Alias.Member value/call/array syntax parses |
| A6 | malformed forms diagnose clearly |
| A7 | legacy source syntax remains valid |

## B. Semantics

| ID | Requirement |
|---|---|
| B1 | one module spans multiple files |
| B2 | private is default |
| B3 | public imported members work |
| B4 | private access fails clearly |
| B5 | missing module/member fails clearly |
| B6 | aliases are physical-source scoped |
| B7 | module import cycles fail |
| B8 | duplicate module providers fail |
| B9 | modules cannot access consumer globals |
| B10 | library compilation requires module sources |

## C. Package/project

| ID | Requirement |
|---|---|
| C1 | `.smilelibproj` loads/builds |
| C2 | deterministic `.smilelib` builds |
| C3 | manifest/API/source entries validate |
| C4 | unsafe/malformed package is rejected |
| C5 | project/package references resolve |
| C6 | project-reference cycles fail |
| C7 | cache invalidates on package hash change |
| C8 | unchanged library does not rebuild unnecessarily |

## D. Visual Studio

| ID | Requirement |
|---|---|
| D1 | SMILE 2.0 Library template appears |
| D2 | library sources appear and refresh immediately |
| D3 | References node appears |
| D4 | add/remove reference works live |
| D5 | missing reference state is visible |
| D6 | library F5 gives non-runnable message |
| D7 | Import module completion works |
| D8 | Alias. public-member completion works |
| D9 | private members remain hidden |
| D10 | editor/build diagnostics agree |
| D11 | project-reference library breakpoint/F10 works |

## E. Targets

| ID | Requirement |
|---|---|
| E1 | LibraryConsumer native build/run |
| E2 | LibraryConsumer Web build/run |
| E3 | LocalModuleBasics native/Web |
| E4 | same bound model feeds both emitters |
| E5 | duplicate member names across modules do not collide |

## F. Regression

| ID | Requirement |
|---|---|
| F1 | `scripts\build.cmd` passes |
| F2 | `scripts\smoke-test.cmd` passes |
| F3 | ten normal games pass native/Web |
| F4 | ten no-demo games pass native/Web |
| F5 | Web sound remains working |
| F6 | DirectX/Direct2D and GDI remain working |
| F7 | immediate source/reference refresh works |
| F8 | breakpoints/F10 remain working |
| F9 | File -> New source template remains working |
| F10 | `git diff --check` passes |

---

# Mandatory live Visual Studio test

1. Create a **SMILE 2.0 Library** project.
2. Confirm its source appears immediately.
3. Build and confirm `.smilelib` output.
4. Open `LibraryConsumer`.
5. Add the library project reference through the UI.
6. Confirm the References node updates immediately.
7. Type:

```smile
Import Smile.Math.Extras As Math
```

8. Confirm completion after:

```text
Math.
```

9. Build/run native.
10. Build/run Web.
11. Set a breakpoint inside project-referenced `Clamp.smile`.
12. Confirm bind, stop, and F10.
13. Remove/re-add the reference and confirm live refresh.
14. Restart Visual Studio and confirm persistence.

---

# Definition of Done

Phase 2 is complete when a reusable component written entirely in SMILE:

- builds independently;
- produces a target-neutral `.smilelib`;
- is referenced by another SMILE project;
- is imported with a qualified alias;
- provides public-only IntelliSense;
- compiles and runs on Windows native and Web;
- debugs through real project-referenced `.smile` source;
- preserves the entire existing game and tooling regression surface.
