# Workspace Lifecycle, Multi-Project, and Governance Hardening

## 1. Buffer lifecycle

The project workspace must unregister an editor buffer and its invalidation callback when the buffer/document closes.

Do not permanently retain:

- closed text buffers;
- stale text snapshots;
- analysis-cache callbacks;
- closed document references.

Use the appropriate Visual Studio text-document/buffer lifetime event and a disposable registration.

---

# 2. One source, multiple projects

Do not map one physical source path to only one owner.

Prepare the workspace so one source path may be associated with several project source sets. Analysis should select the correct project context using the active hierarchy/document context where available.

This is needed before future reusable library/project references.

Do not implement libraries in this milestone.

---

# 3. Hierarchy mutation integration

After:

- New SMILE 2.0 Source Code;
- Add Existing;
- Remove from Project;
- Set as Startup;
- Include as Support;

refresh:

- source set;
- hierarchy;
- open-buffer ownership;
- IntelliSense;
- diagnostics;
- next native build;
- next Web build.

---

# 4. Root governance

Update root `AGENTS.md` to record:

- Windows native is priority 1.
- Web is priority 2.
- Both consume one shared language.
- Language evolution may migrate all ten legacy games.
- Reusable components will be written in SMILE.
- Approved future generic services include image/sprite primitives, persistent data blocks, and multiple sound-effect channels.
- `Smile.RPG.Abilities` is approved.
- Magic Points / MP replaces Technique Points / TP.

Remove or clarify stale wording that prohibits the already approved Web target.

---

# 5. Preserve user-confirmed functionality

Explicitly regression-test:

- F10 source stepping;
- Set as Startup;
- Web sound;
- native/Web launch;
- File > Open;
- Solution Explorer double-click;
- IntelliSense;
- Tools > Build SMILE File.
