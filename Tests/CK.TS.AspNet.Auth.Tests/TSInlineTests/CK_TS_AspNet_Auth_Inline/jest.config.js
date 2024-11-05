//-AllowAutoUpdate
// Notes:
//   - To settle this file, simply remove the //-AllowAutoUpdate
//     first line: it will not be updated.
//   - Jest is not ESM compliant. Using CJS here.
//
const { pathsToModuleNameMapper } = require('ts-jest');
const { compilerOptions } = require('./tsconfig');

module.exports = {
    moduleFileExtensions: ['js', 'json', 'ts'],
    testRegex: 'src/.*\\.spec\\.ts$',
    transform: {
        '^.+\\.ts$': ['ts-jest', {
            // Removes annoying ts-jest[config] (WARN) message TS151001: If you have issues related to imports, you should consider...
            diagnostics: {ignoreCodes: ['TS151001']}
        }],
    },
    moduleNameMapper: pathsToModuleNameMapper(compilerOptions.paths ?? {}, { prefix: '<rootDir>/' } ),
    testEnvironment: 'jsdom',
    globals: {
        // CK.Testing.StObjTypeScriptEngine uses this:
        // TestHelper.CreateTypeScriptTestRunner inject test environment variables here
        // in a "persistent" way: these environment variables will be available until
        // the Runner returned by TestHelper.CreateTypeScriptTestRunner is disposed.
        // The CKTypeScriptEnv is then available to the tests (with whatever required like urls of
        // backend servers).
        // Do NOT alter the comments below.
        // Start-CKTypeScriptEnv
        CKTypeScriptEnv: {}
        // Stop-CKTypeScriptEnv-Stop
    },
    setupFilesAfterEnv: ['<rootDir>/jest-setup.ts']
};