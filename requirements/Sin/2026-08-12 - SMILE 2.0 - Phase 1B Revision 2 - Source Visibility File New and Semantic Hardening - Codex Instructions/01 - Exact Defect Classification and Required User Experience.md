# Exact Defect Classification and Required User Experience

## 1. The source is included but invisible

The latest manual result is not “the Add New command does nothing.”

The precise behavior is:

```text
New source command
    -> creates physical .smile file
    -> adds enough project state that Add Existing detects a duplicate
    -> opens the source for editing
    -> fails to display the source in Solution Explorer
```

Codex must verify all three state layers independently:

```text
A. physical file exists
B. .smileproj contains exactly one SmileSource entry
C. SmileProjectSourceSet.Items contains the source
D. Visual Studio hierarchy exposes the source as a visible project child
```

A–C are not a substitute for D.

## 2. Required final workflow

```text
Right-click SMILE project
-> New SMILE 2.0 Source Code
-> enter Battle.smile
-> Battle.smile is created
-> project XML contains one relative SmileSource
-> Battle.smile appears immediately in Solution Explorer
-> Battle.smile opens in the SMILE editor
-> IntelliSense and diagnostics work
-> native build includes it
-> Web build includes it
```

Then:

```text
close Visual Studio
-> reopen the same solution
-> Battle.smile remains visible
```

## 3. Add Existing behavior

When `Battle.smile` is already included and visible:

```text
Add Existing SMILE 2.0 Source Code...
-> choose Battle.smile
-> clear “already included” message
-> no duplicate XML entry
-> existing visible hierarchy item remains intact
```

When it is physically present but not included:

```text
Add Existing SMILE 2.0 Source Code...
-> choose Battle.smile
-> project entry is created
-> item appears immediately
```

Do not “fix” the current failure by allowing duplicate entries.

## 4. Remove and re-add round trip

Required:

```text
right-click Battle.smile
-> Remove from Project
-> hierarchy item disappears
-> project entry disappears
-> physical file remains

Add Existing SMILE 2.0 Source Code...
-> choose Battle.smile
-> one project entry is restored
-> hierarchy item reappears immediately
```

## 5. File > New experience

With or without a solution open:

```text
File
-> New
-> File...
-> SMILE 2.0 Source Code
```

must create a standalone `.smile` editor document with SMILE syntax highlighting and normal Save As behavior.

When a SMILE project is open, the project-specific command remains the preferred way to create and include a support source.

## 6. Context-menu clarity

The SMILE project menu must not show nonfunctional entries such as:

```text
Connected Services
New EditorConfig File
```

The final menu should advertise only commands the focused SMILE project system actually supports.
