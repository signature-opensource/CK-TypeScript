# Testing code generated TypeScript 

## Jest as the default test framework

When no `"scripts": { "test": "..." }` exists in the target application's package.json, Jest
is installed with a configuration that is ready to run and the "test" command is set to "jest".

When executing test, the runner enables environment variables to be set on the node process.
For Jest another feature is available: the runner duplicates the environment variables settings
in a "persistent" way: these environment variables will be available until the TypeScriptRunner
returned by CreateTypeScriptTestRunner is disposed.

By suspending the execution with a `Task.Delay` or `TestHelper.SuspendAsync`, the test configuration
and its environment (the running process, its endpoints, etc.) are available: TypeScript code can be
edited and tested in the context of a test with a living application and its debugger on the .Net side.

We use a Jest's setupFiles named `jest.StObjTypeScriptEngine.js` for this. This module run once
before each test file and before the testing framework is installed. This file should usually be
empty and may be .gitignored (but it's not required): if "test" command is "jest" it will be
recreated before each test run.

If a test in progress is abruptly interrupted, this `jest.StObjTypeScriptEngine.js` will not be
cleaned up (but this is not really an issue since it will be rewritten by the next test run).

_Note:_ The jest setupFile has been preferred over a more general solution like 'dotenv'
(that is very good) to limit the dependencies as much as possible but this may evolve.

