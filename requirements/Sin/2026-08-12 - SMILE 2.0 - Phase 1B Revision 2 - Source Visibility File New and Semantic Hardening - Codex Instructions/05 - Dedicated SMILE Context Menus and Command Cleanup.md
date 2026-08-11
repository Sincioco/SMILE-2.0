# Dedicated SMILE Context Menus and Command Cleanup

## Mission

Expose a focused project menu containing only working SMILE commands.

The final project menu should be:

```text
Build
Rebuild
Clean
------------------------------
New SMILE 2.0 Source Code
Add Existing SMILE 2.0 Source Code...
------------------------------
Edit SMILE 2.0 Project File
Open Project Folder
```

Hide/remove irrelevant inherited entries including:

```text
Connected Services
New EditorConfig File
```

---

# 1. Dedicated menu

Prefer a dedicated SMILE project context menu rather than adding a command group to the generic Visual Studio project-node menu.

Return that dedicated context-menu identity from the SMILE hierarchy project node.

Keep source and folder menus similarly focused.

---

# 2. Existing source command

Rename the visible command:

```text
Add Existing SMILE 2.0 Source Code...
```

Required behavior remains:

- `.smile` filter;
- project-relative include;
- safe copy into project for outside files;
- duplicate detection;
- immediate hierarchy visibility;
- immediate workspace refresh;
- native/Web inclusion.

---

# 3. Edit project command

Add:

```text
Edit SMILE 2.0 Project File
```

It opens the real `.smileproj` in the text editor.

---

# 4. Preserve source commands

Keep working source-item commands:

```text
Set as Startup
Include as Support Source
Remove from Project
Open Containing Folder
```

The current startup remains visibly marked.

Do not regress the user-confirmed `Program-NoDemo.smile` startup workflow.
