# Invalid Type Member Fixtures

These focused sources keep the Wave C diagnostic contract executable. Each standalone file produces one exact diagnostic family: `SML3440` for declaration collisions or illegal private fields, `SML3441` for an invalid Property shape, `SML3442` for `Me` outside an instance member, `SML3443` for a missing or non-Type receiver member, `SML3444` for a nonaddressable temporary receiver, `SML3445` for unavailable accessors, and `SML3446` for private access outside the containing Type.

The capability projects consume `Smile.Lightweight.Oop.Proof` through both project and format-6 package references. Calling `Counter.DrawProbe` or reading `Counter.GameProbe` from a Console project must report `SML3704`; assigning `Counter.GameProbe` remains valid because the setter has no Game Window requirement.
