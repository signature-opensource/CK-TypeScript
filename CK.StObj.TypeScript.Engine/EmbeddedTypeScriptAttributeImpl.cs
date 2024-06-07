using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.TypeScript.Engine
{
    public sealed class EmbeddedTypeScriptAttributeImpl : ITSCodeGenerator
    {
        readonly EmbeddedTypeScriptAttribute _attr;
        readonly Type _target;

        public EmbeddedTypeScriptAttributeImpl( EmbeddedTypeScriptAttribute attr, Type target )
        {
            _attr = attr;
            _target = target;
        }

        public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

        public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

        public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

        public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
        {
            var resLocator = new ResourceLocator( _target, _attr.ResourcePath ?? "Res" );
            NormalizedPath targetPath = _attr.TargetFolderName ?? _target.Namespace!.Replace( '.', '/' );
            foreach( var r in _attr.ResourceNames )
            {
                var content = resLocator.GetRequiredString( r );
                TSManualFile file = context.Root.Root.FindOrCreateManualFile( targetPath.AppendPart( r ) );
                file.File.Body.Append( content );
            }
            return true;
        }
    }
}
