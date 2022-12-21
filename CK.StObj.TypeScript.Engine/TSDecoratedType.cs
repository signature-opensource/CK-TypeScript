using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Captures typed generators and [TypeScript] values on a type.
    /// </summary>
    readonly struct TSDecoratedType
    {
        public TSDecoratedType( IReadOnlyList<ITSCodeGeneratorType> generators, TypeScriptAttribute? attr )
        {
            Generators = generators;
            Attribute = attr;
        }

        public readonly TypeScriptAttribute? Attribute;

        public readonly IReadOnlyList<ITSCodeGeneratorType> Generators;

        public bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, TSGeneratedTypeBuilder builder )
        {
            if( Attribute != null )
            {
                builder.TypeName = Attribute.TypeName;
                builder.SameFolderAs = Attribute.SameFolderAs;
                builder.SameFileAs = Attribute.SameFileAs;
                builder.Folder = Attribute.Folder;
                builder.FileName = Attribute.FileName;
            }
            bool success = true;
            foreach( var g in Generators )
            {
                success &= g.ConfigureBuilder( monitor, context, builder );
            }
            return success;
        }
    }
}
