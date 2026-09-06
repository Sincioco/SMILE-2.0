# H6.1 completed hardening — Start Here

This delivery records prerequisite hardening and current Desktop/Chrome Web parity
for SMILE 2.0. Read the files in this order:

1. `h6-1-hardening-and-web-parity-report.md`: decision, preservation, implementation,
   outputs, scope and limitations.
2. `h6-1-native-web-parity-matrix.md` and `h6-1-native-and-browser-observations.md`:
   native/VM/actual-browser evidence, without conflating them.
3. `h6-1-known-web-issues.json`: all 26 known issue dispositions.
4. `h6-1-hardening-and-web-parity-gate.json` and its `.schema.json`: fixed G01–G12
   acceptance with the explicit user-approved Chrome-only scope revision.
5. `h6-1-evidence.json`, `h6-1-artifacts.json` and chronological
   `h6-1-hardening-and-web-parity-ledger.md`: evidence references, identities and
   commands, including recorded intermediate failures.
6. `h6-1-web-deployment.md`: normal full-fidelity builds and static hosting.

In the evidence ZIP, these documents keep their repository-relative paths under
`Repository-Files/docs/implementation`. Selected logs remain under
`Repository-Files/artifacts/temp`; companions are verification aids, not programs
to execute blindly. A separate `bundle-checksums.json` hashes delivery members.
The public Web ZIP contains only the three generated applications and Start Here.

Validated source: `bc6f607bec5a60df1e72a0d3541156bc9175fe82`, branch `main`.
Acceptance is PASS-NATIVE-WEB on the revised Windows/Chrome baseline, not Firefox,
Safari or all devices. Original H0–H6 history is retained separately.

Stop before Battle Scene Editor E0–E12. Later explicitly approved work consists
of optimized-Web tiers, a visual README rewrite and then loader/splash extensions;
none is claimed complete by this hardening report. Double is not part of this work.
