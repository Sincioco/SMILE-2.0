# Phase 6 recommendation: ApplicationId

Phase 5.1 intentionally does not add `ApplicationId`.

For Phase 6, evaluate one explicit, stable application identity in `.smileproj` so persistent-data and packaged-asset namespaces do not depend on a mutable output filename. The identity should be target-neutral, validated once by the shared project model, serialized only where consumers need it, and preserve existing projects through a documented compatibility default.

Do not couple this identifier to Character, Party, Inventory, Equipment, Abilities, MP, Shop, or RPG Save designs. Those remain separate Phase 6 decisions.
