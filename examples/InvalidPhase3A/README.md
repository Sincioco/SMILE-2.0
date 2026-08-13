# Phase 3A invalid fixtures

Each source intentionally fails with one stable Phase 3A diagnostic family:

| Fixture | Expected code |
|---|---|
| `OptionExplicitLate.smile` | SML3300 |
| `OptionExplicitUndeclared.smile` | SML3303 |
| `ScalarDimWithoutAs.smile` | SML3302 |
| `UnknownBuiltInType.smile` | SML3401 |
| `NumberToTextAssignment.smile` | SML3304 |
| `TextToBooleanAssignment.smile` | SML3304 |
| `MixedTextAddition.smile` | SML3308 |
| `TextRelationalComparison.smile` | SML3308 |
| `InvalidByRefLiteral.smile` | SML3305 |
| `InvalidByRefConstant.smile` | SML3305 |
| `WrongArgumentType.smile` | SML3304 |
| `WrongReturnType.smile` | SML3304 |
| `InconsistentLegacyReturnTypes.smile` | SML3309 |
| `DuplicateLocal.smile` | SML3306 |
| `UseBeforeLocalDeclaration.smile` | SML3307 |
