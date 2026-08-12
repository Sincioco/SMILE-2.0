# Phase 2A dependency hardening fixture

This fixture proves that a dependent library imports and calls a public member from a base library without adding language syntax.

- `Base` builds `Example.Phase2A.Base.smilelib`.
- `Dependent` references the Base project and builds a package whose manifest records the exact `Example.Phase2A.Base` `1.0.0` dependency.
- `ConsumerPackages` supplies the dependent package and base package explicitly.
- `ConsumerMixed` supplies the dependent package and the exact base project provider.

Build Base and Dependent with the library target before compiling either consumer. Both consumers print `12`. `Dependent.Hidden` remains private and is absent from package API metadata and qualified IntelliSense.
