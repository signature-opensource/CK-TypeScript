using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CSemVer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

namespace CK.TS.Angular.Engine
{
    public partial class AngularCodeGeneratorImpl : ITSCodeGenerator
    {
        const string _defaultAngularCliVersion = "^18.2.0";
        const string _conflictFolderName = "_ckConflict_";

        public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
        {
            if( initializer.BinPathConfiguration.IntegrationMode != CKGenIntegrationMode.Inline )
            {
                monitor.Error( $"Angular application requires Inline IntegrationMode. '{initializer.BinPathConfiguration}' mode is not supported." );
                return false;
            }
            Throw.DebugAssert( "Inline mode => IntegrationContext.", initializer.IntegrationContext != null );
            initializer.IntegrationContext.OnBeforeIntegration += OnBeforeIntegration;
            initializer.IntegrationContext.OnAfterIntegration += OnAfterIntegration;
            return true;
        }

        bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

        public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            var f = context.Root.Root.FindOrCreateManualFile( "CK/Angular/CKGenAppModule.ts" );
            f.File.Body.Append( """
                import { NgModule, Provider } from '@angular/core';

                @NgModule({
                    imports: [
                    // Registered NgModules come here.
                    ],
                    exports: [
                    // Registered NgModules also come here.
                ]
                  })
                export class CKGenAppModule {
                   static Providers : Provider[] = [
                    // Registered providers come here.
                   ];
                }
                """ );
            return true;
        }
    }
}
