using CK.Core;
using CK.Setup;
using System.IO;

namespace CK.TS.Angular.Engine
{
    /// <summary>
    /// Implements https://github.com/thymikee/jest-preset-angular/blob/main/examples/example-app-v18.
    /// </summary>
    sealed class AngularJestSetupHandler : TypeScriptIntegrationContext.JestSetupHandler
    {
        public AngularJestSetupHandler( TypeScriptIntegrationContext context )
            : base( context )
        {
        }

        protected override bool Run( IActivityMonitor monitor )
        {
            if( !base.Run( monitor ) ) return false;
            UpdateTypeScriptConfigSpec( monitor );
            return true;
        }

        protected override bool WriteJestConfigFile( IActivityMonitor monitor )
        {
            var content = $$"""
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
                    setupFilesAfterEnv: ['<rootDir>/{{JestSetupTSFileName}}']
                };
                """;
            if( ShouldWriteAllowAutoUpdateFile( monitor, JestConfigFilePath, content ) )
            {
                File.WriteAllText( JestConfigFilePath, content );
            }
            return true;
        }

        protected override bool WriteJestSetupTSFile( IActivityMonitor monitor )
        {
            const string content = """
                //-AllowAutoUpdate
                // Generated by: CK.TS.Angular.Engine.AngularJestSetupHandler.
                import 'jest-preset-angular/setup-jest';

                // Augments the global with our CKTypeScriptEnv. See https://stackoverflow.com/questions/59459312/using-globalthis-in-typescript
                declare global { var CKTypeScriptEnv: { [key: string]: string }; }

                Object.defineProperty(document, 'doctype', {
                    value: '<!DOCTYPE html>',
                });

                Object.defineProperty(window, 'getComputedStyle', {
                    value: () => {
                        return {
                            display: 'none',
                            appearance: ['-webkit-appearance'],
                        };
                    },
                });

                /**
                 * ISSUE: https://github.com/angular/material2/issues/7101
                 * Workaround for JSDOM missing transform property
                 */
                Object.defineProperty(document.body.style, 'transform', {
                    value: () => {
                        return {
                            enumerable: true,
                            configurable: true,
                        };
                    },
                });

                HTMLCanvasElement.prototype.getContext = <typeof HTMLCanvasElement.prototype.getContext>jest.fn();
                """;
            if( ShouldWriteAllowAutoUpdateFile( monitor, JestSetupTSFilePath, content ) )
            {
                File.WriteAllText( JestSetupTSFilePath, content );
            }
            return true;
        }

        void UpdateTypeScriptConfigSpec( IActivityMonitor monitor )
        {
            var specConfigPath = TargetProjectPath.AppendPart( "tsconfig.spec.json" );
            var specConfig = TSConfigJsonFile.ReadFile( monitor, specConfigPath, mustExist: true );
            if( specConfig != null && specConfig.CompilerOptionsTypes.Contains( "jasmine" ) )
            {
                monitor.Info( "Replacing 'jasmine' with 'jest' in tsconfig.spec.json file." );
                specConfig.CompilerOptionsTypes.Remove( "jasmine" );
                specConfig.CompilerOptionsTypes.Add( "jest" );
                specConfig.Save();
            }
        }
    }
}
