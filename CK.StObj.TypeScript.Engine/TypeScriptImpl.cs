using CK.Core;
using CK.Setup;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine
{
    public class TypeScriptImpl : ICodeGenerator
    {
        readonly Type _type;

        public TypeScriptImpl( TypeScriptAttribute a, Type type )
        {
            Attribute = a;
            _type = type;
        }

        public TypeScriptAttribute Attribute { get; }

        AutoImplementationResult ICodeGenerator.Implement( IActivityMonitor monitor, ICodeGenerationContext codeGenContext )
        {
            var generator = codeGenContext.GetTypeScriptGenerator( monitor );
            if( generator == null ) return AutoImplementationResult.Success;

            if( codeGenContext.CurrentRun.EngineMap.AllTypesAttributesCache[_type].GetTypeCustomAttributes<ICodeGeneratorTypeScript>().Any() )
            {
                monitor.Trace( $"TypeScript generation for '{_type.Name}' is handled by other ICodeGeneratorTypeScript." );
                return AutoImplementationResult.Success;
            }
            if( _type.IsEnum )
            {
                var ts = generator.GetTSTypeFile( _type );
                ts.EnsureFile().Body.Append( "export " ).AppendEnumDefinition( monitor, _type, ts.TypeName );
                return AutoImplementationResult.Success;
            }
            monitor.Error( $"TypeScript generation for '{_type.Name}' must be handled by specific ICodeGeneratorTypeScript." );
            return AutoImplementationResult.Failed;
        }
    }
}
