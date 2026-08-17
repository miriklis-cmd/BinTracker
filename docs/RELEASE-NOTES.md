# BinTracker Current Release Notes

## v0.4.0-alpha.23.4.1

### Build tooling self-heal

Corrected the remaining build-tooling failures exposed by running alpha.23.4 over an existing extracted folder.

- Detects and removes the exact obsolete `global.json` created by alpha.23.3.
- Does not delete unrelated/user-managed `global.json` files.
- Uses direct `dotnet ... || goto :fail` command guards for restore, build and test.
- A failed restore/build/test can no longer continue to later stages or print BUILD SUCCESSFUL.
- Retains stale build-server cleanup and conservative MSBuild settings.

The obsolete SDK pin persisted because ZIP extraction cannot delete a file that existed in an older extracted folder but is absent from a newer archive.

### Test requirement

Run this build over the existing folder once. It should report removal of the obsolete global.json, use SDK 10.0.400, and complete restore/build/tests normally.
