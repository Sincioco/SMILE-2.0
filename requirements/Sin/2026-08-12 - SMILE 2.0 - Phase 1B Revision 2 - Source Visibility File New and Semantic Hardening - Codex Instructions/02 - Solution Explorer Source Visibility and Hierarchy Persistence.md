# Solution Explorer Source Visibility and Hierarchy Persistence

## Mission

Correct the project hierarchy so every source included by `SmileProjectSourceSet` is represented exactly once as a visible Solution Explorer item.

The current source-creation operation appears to succeed below the hierarchy layer. The hierarchy must be repaired, not bypassed.

---

# 1. Required root-cause investigation

Reproduce with `games\Snake\Snake.slnx`:

1. Record the project XML before adding.
2. Use the current Add New command.
3. Record:
   - physical path;
   - resulting project XML;
   - `SmileProjectSourceSet.Items`;
   - `SmileProjectSourceSet.CompilationSources`;
   - hierarchy `_items`;
   - root `Children`;
   - item IDs;
   - `FirstChild` / `NextSibling` chain;
   - hierarchy notifications sent.
4. Confirm why Add Existing reports the source is already included.
5. Close and reopen Visual Studio and compare initial hierarchy construction.

Document the confirmed root cause in the final report.

Do not assume the problem is the XML editor merely because the item is invisible.

---

# 2. One authoritative hierarchy projection

Create or extract the smallest deterministic hierarchy-projection helper that can be focused-tested without running Visual Studio.

Given:

```text
SmileProjectSourceSet.Items
ProjectKind
AssetIncludes / Assets folder
physical asset children
```

the projection must include:

- selected startup source;
- every alternate `StartupOnly="true"` source;
- every ordinary support source;
- Assets folder when applicable;
- every existing asset child according to current rules.

Every project source appears exactly once.

Do not use separate source loops whose interaction can silently omit ordinary support files unless tests prove the result unambiguously. A simple single source projection ordered by clear policy is preferable.

Suggested display order:

```text
selected startup
alternate startup candidates
ordinary support sources
Assets
```

Another deterministic order is acceptable, but sources must never be hidden behind or lost after asset enumeration.

---

# 3. Initial load and live mutation

Both paths are mandatory.

## Initial project load

A project file already containing:

```xml
<SmileSource Include="Program.smile" StartupOnly="true" />
<SmileSource Include="Program-NoDemo.smile" StartupOnly="true" />
<SmileSource Include="Helpers.smile" />
```

must show all three source files after opening Visual Studio.

## Live mutation

After creating or adding `Helpers2.smile`:

- source-set reload succeeds;
- a stable nonreserved item ID is assigned;
- root child/sibling enumeration includes it;
- the hierarchy receives correct add/invalidate notifications;
- the item appears without reload;
- open documents remain open.

---

# 4. Hierarchy contracts to verify

Review and test:

- `VSHPROPID_FirstChild`
- `VSHPROPID_FirstVisibleChild`
- `VSHPROPID_NextSibling`
- `VSHPROPID_NextVisibleSibling`
- `VSHPROPID_Parent`
- `VSHPROPID_ChildrenEnumerated`
- `VSHPROPID_IsHiddenItem`
- `VSHPROPID_IsNonMemberItem`
- `OnItemAdded`
- `OnItemDeleted`
- `OnItemsAppended`
- `OnInvalidateItems`
- property changes for parent/previous sibling
- stable item-ID reuse
- no collision with `VSITEMID_ROOT` or `VSITEMID_NIL`

The visible tree after a restart proves the initial projection. Dynamic notification tests prove the live path.

---

# 5. Focused automated tests

Add testable coverage for:

1. Game project with selected startup, alternate startup, ordinary support, and Assets.
2. Console project with startup and several supports.
3. Every `SmileProjectSourceSet.Items` source maps to one hierarchy item.
4. No hierarchy source exists without a project-source item.
5. Root child traversal reaches every expected source.
6. Adding a source adds one visible node.
7. Removing a source removes one node.
8. Re-adding restores it.
9. IDs remain valid and unique across repeated refreshes.
10. Initial reload produces the same visible source set as live mutation.

A test that only asserts project XML is insufficient.

---

# 6. Companion fixture

Add the supplied:

```text
examples\SourceVisibilityBasics
```

It contains:

- `Program.smile`
- `Program-NoDemo.smile`
- `Helpers.smile`
- `Assets\Readme.txt`

All source files must be visible on first open.

Then use the project command to add `DynamicHelper.smile`, prove immediate visibility, restart Visual Studio, and prove persistent visibility.
