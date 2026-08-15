# Invalid Phase 5.2 fixtures

`ConsoleDrawStack` must report exactly one consumer-located `SML3704` because `DrawStack` transitively requires `Game Window`, without a diagnostic cascade from inside `Smile.UI.MenuNavigator` or `Smile.UI.Menu`.
