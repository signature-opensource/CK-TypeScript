using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CK.TS.Angular.Engine
{
    public class AngularCodeGeneratorImpl : ITSCodeGenerator
    {
        public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
        {
            initializer.IntegrationContext.OnBeforeIntegration += OnBeforeIntegration; ;
            return true;
        }

        void OnBeforeIntegration( object? sender, TypeScriptIntegrationContext.BeforeEventArgs e )
        {
            var angularJsonPath = e.TargetProjectPath.AppendPart( "angular.json" );
            if( !File.Exists( angularJsonPath ) )
            {
                // TODO
            }
        }

        bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

        public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            return true;
        }
    }
}
