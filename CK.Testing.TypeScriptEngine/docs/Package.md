Tests of the generated TypeScript, as `TSTestHelperExtensions` on the test helper:
`GetTypeScriptGeneratedOnlyTargetProjectPath` (only generates `ck-gen/`) and `GetTypeScriptInlineTargetProjectPath` (a
project with Yarn and Jest) give the target project of a test, `EnsureTypeScriptConfigurationAspect` configures a BinPath
for it, and `CreateTypeScriptRunner` returns a `Runner` that runs its tests (`yarn test` by default) and reverts the test
setup when disposed.
See the [README](https://github.com/signature-opensource/CK-TypeScript/blob/stable/CK.Testing.TypeScriptEngine/README.md).
