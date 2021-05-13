using CK.Core;
using CK.Setup;
using CK.StObj.TypeScript.Engine;
using CK.Text;
using CK.TypeScript.CodeGen;
using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.StObj.TypeScript.Tests
{
    /// <summary>
    /// The Engine part.
    /// </summary>
    public class EmptyTypeScriptAttributeImpl : TypeScriptAttributeImpl, ITSCodeGeneratorType
    {
        public EmptyTypeScriptAttributeImpl( TypeScriptAttribute a, Type t )
            : base( a, t )
        {
        }

        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor, ITSTypeFileBuilder builder, TypeScriptAttribute a )
        {
            return true;
        }

        public bool GenerateCode( IActivityMonitor monitor, TSTypeFile file )
        {
            // Let the default closer "}" in EnsureTypePart.
            var code = file.EnsureTypePart();
            code.Append( "export " ).Append( file.Type.IsEnum
                                                ? "enum "
                                                : file.Type.IsInterface
                                                    ? "interface "
                                                    : "class " ).Append( file.TypeName )
                .OpenBlock();
            // Thanks to the "}" closer, this part stays "opened": the closing } will be
            // called when generating the final text.
            return true;
        }
    }
}
