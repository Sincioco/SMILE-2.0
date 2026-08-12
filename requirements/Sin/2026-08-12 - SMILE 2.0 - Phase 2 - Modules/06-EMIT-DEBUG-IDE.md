# Phase 2 Native/Web Emission, Debugging, and IntelliSense

## 1. Shared lowering

Both emitters consume the same bound module/import model.

Do not create emitter-specific import lookup.

---

# 2. Native emission

Mangle module members into stable MASM/linker-safe identities.

Requirements:

- modules may export identical member names without collision;
- private/public visibility does not create unstable runtime names;
- constants, arrays, SUBs, and FUNCTIONs work;
- legacy global names retain existing behavior;
- one module global state instance exists per final program compilation.

Example conceptual identity:

```text
Smile.Math.Extras::Clamp
```

maps to one deterministic native symbol.

---

# 3. Web emission

Map the same bound identity to stable JavaScript-safe output.

Avoid exposing import aliases as browser globals.

The consumer alias is a source-binding concept, not the module's runtime identity.

---

# 4. Project-reference debugging

Required native Debug behavior:

- breakpoint binds in a local module source;
- breakpoint binds in a project-referenced library source;
- F10 remains in the real `.smile` file;
- F11 enters a called library routine when supported by the existing debugger model;
- Shift+F11 returns to the consumer;
- identical line numbers in different module files remain distinct.

For packaged `.smilelib`:

- diagnostics use deterministic extracted source paths;
- opening extracted read-only source is acceptable;
- project-reference debugging is the primary Phase 2 acceptance path.

Browser `.smile` breakpoints remain deferred.

---

# 5. IntelliSense

Required completion:

```text
IMPORT
known module names after IMPORT
AS
local alias
Alias.
public module members
routine signatures
constant descriptions
array dimensions
```

Private members never appear in consumer completion.

Completion descriptions identify:

```text
module
package/project provider
member kind/signature
```

---

# 6. Live analysis

Use current open buffers for:

- consumer source;
- local module source;
- loaded project-referenced library source.

A physical library source used by multiple projects must retain the correct active project context.

Reference add/remove and library-source changes invalidate dependent analysis without requiring solution reload.

---

# 7. Diagnostics and navigation

Import/usage errors point to consumer source.

Declaration errors point to library source.

Error List navigation opens real `.smile` source, not generated MASM/C/JavaScript.

Preserve File -> Open and Solution Explorer double-click.
