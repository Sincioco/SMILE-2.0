# Native module initializer regression

This native-only regression proves that accepted module-level scalar Class
initializers execute once before the selected startup source. Imported modules
initialize before their importers, declarations in one source retain declaration
order, and provider/package dependencies precede the consuming project. The
project intentionally lists `Dependent.smile` before `Base.smile` so the import
edge—not incidental file order—controls their initialization.

`Program.smile` also covers private and Public module scalars, named constructor
state, repeated access, explicit assignment, entry-scope and local `As New`, and
normal cleanup. `Failure.smile` proves that an initializer failure terminates
before entry execution and releases both completed and partially constructed
objects. Run `scripts\test-native-module-initializers.ps1` after building the
compiler and native runtime.

The shared order applies to accepted native compilation. Cyclic module imports
remain rejected by `SML3108`; project and package dependency cycles remain
rejected by `SML3205`, so no cyclic initializer order is implied.
