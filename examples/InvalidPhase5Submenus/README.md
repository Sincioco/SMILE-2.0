# Invalid Phase 5.2 fixtures

`ConsoleDrawStack` must report exactly one consumer-located `SML3704` because `MenuNavigator.Draw()` transitively requires `Game Window`, without a diagnostic cascade from inside the `Smile.UI.Menu` project or package source.
