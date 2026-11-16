# Testing code generated TypeScript 

## Jest as the default test framework

When `TypeScriptBinPathAspectConfiguration.AutoInstallJest` is true (the default), Jest is installed
in the target application with a configuration that is ready to run (a `jest.config.js` and a
`jest-setup.ts` file written by `TypeScriptIntegrationContext.JestSetupHandler`) and, when no
`"scripts": { "test": "..." }` exists in its package.json, the "test" command is set to "jest".

When executing test, the runner enables environment variables to be set on the node process.
For Jest another feature is available: the runner duplicates the environment variables settings
in a "persistent" way: these environment variables will be available until the `TSTestHelperExtensions.Runner`
returned by `TSTestHelperExtensions.CreateTypeScriptRunner` is disposed.

By suspending the execution with a `Task.Delay` or `TestHelper.SuspendAsync`, the test configuration
and its environment (the running process, its endpoints, etc.) are available: TypeScript code can be
edited and tested in the context of a test with a living application and its debugger on the .Net side.

We use the `jest.config.js` file for this: when the "test" command is "jest",
`TypeScriptIntegrationContext.JestSetupHandler.PrepareJestRun` writes the environment variables in its
`globals.CKTypeScriptEnv` object (with at least `CK_TYPESCRIPT_ENGINE: "true"`) and the server address
in its `testEnvironmentOptions.url`, between the `// Start-CKTypeScriptEnv`/`// Stop-CKTypeScriptEnv`
and `// Start-CKTypeScriptSrv`/`// Stop-CKTypeScriptSrv` marker comments. Disposing the runner restores
the defaults (`CKTypeScriptEnv: {}` and `url: 'http://localhost'`). The `jest-setup.ts` file (Jest's
`setupFilesAfterEnv`) declares the global `CKTypeScriptEnv` for the tests.

If a test in progress is abruptly interrupted, the `jest.config.js` will not be
cleaned up (but this is not really an issue since it will be rewritten by the next test run).

_Note:_ The Jest configuration has been preferred over a more general solution like 'dotenv'
(that is very good) to limit the dependencies as much as possible but this may evolve.

