// Jest is not ESM compliant. Using CJS here.
module.exports = {
    moduleFileExtensions: ['js', 'json', 'ts'],
    rootDir: 'src',
    testRegex: '.*\\.spec\\.ts$',
    transform: {
        '^.+\\.ts$': ['ts-jest', {
            // Removes annoying ts-jest[config] (WARN) message TS151001: If you have issues related to imports, you should consider...
            diagnostics: {ignoreCodes: ['TS151001']},
            // tsconfig fo Jest comes here. 
            tsconfig: {
                "lib": ["es2019", "dom"]
              }    
        }],
    },
    testEnvironment: 'node',
    setupFiles: ["../jest.StObjTypeScriptEngine.js"],
};