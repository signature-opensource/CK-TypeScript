//-AllowAutoUpdate
// Generated by: CK.TS.Angular.Engine.AngularJestSetupHandler.
// Notes:
//   - To settle this file, simply remove the //-AllowAutoUpdate
//     first line: it will not be updated.
//   - Jest is not (yet) ESM compliant. Using CJS here.
//

const { pathsToModuleNameMapper } = require('ts-jest');

const { paths } = require('./tsconfig.json').compilerOptions;

/** @type {import('ts-jest/dist/types').JestConfigWithTsJest} */
module.exports = {
    preset: 'jest-preset-angular',
    moduleNameMapper: pathsToModuleNameMapper(paths, { prefix: '<rootDir>' }),
    testEnvironment: 'jsdom',
    globals: {
        // CK.Testing.TypeScriptEngine uses this:
        // TestHelper.CreateTypeScriptTestRunner inject test environment variables here
        // in a "persistent" way: these environment variables will be available until
        // the Runner returned by TestHelper.CreateTypeScriptTestRunner is disposed.
        // The CKTypeScriptEnv is then available to the tests (with whatever required like urls of
        // backend servers).
        // Do NOT alter the comments below.
        // Start-CKTypeScriptEnv
        CKTypeScriptEnv: {},
        // Stop-CKTypeScriptEnv
    },
    // TestHelper.CreateTypeScriptTestRunner also replaces the 'http://localhost' (that is the default)
    // with the server address (like 'http://[::1]:55235' - a free dynamic port is used). 
    // Do NOT alter the comments below.
    testEnvironmentOptions: { 
        // Start-CKTypeScriptSrv
        url: 'http://localhost',
        // Stop-CKTypeScriptSrv
    },
    setupFilesAfterEnv: ['<rootDir>/jest-setup.ts']
};