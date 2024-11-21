using CK.Core;
using CK.Setup;
using CK.TypeScript.Engine;
using CK.TypeScript.CodeGen;
using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace CK.TypeScript.Tests.EmptyCodeGeneratorTypeSample;

/// <summary>
/// The Engine part.
/// </summary>
public class EmptyTypeScriptAttributeImpl : TypeScriptAttributeImpl, ITSCodeGeneratorType
{
    public EmptyTypeScriptAttributeImpl( TypeScriptAttribute a, Type t )
        : base( a, t )
    {
    }

    public bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder )
    {
        builder.Implementor = GenerateCode;
        return true;
    }

    bool GenerateCode( IActivityMonitor monitor, ITSFileCSharpType tsType )
    {
        // Let the default closer "}" in EnsureTypePart.
        tsType.TypePart.Append( "export " ).Append( tsType.Type.IsEnum
                                            ? "enum "
                                            : tsType.Type.IsInterface
                                                ? "interface "
                                                : "class " ).Append( tsType.TypeName )
            .OpenBlock();
        // Thanks to the "}" closer, this part stays "opened": the closing } will be
        // appended when generating the final text (via ToString or Build methods).
        return true;
    }
}
