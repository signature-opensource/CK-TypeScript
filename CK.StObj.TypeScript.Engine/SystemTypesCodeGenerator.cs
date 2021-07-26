using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Provides an implementation for Guid and other system types.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/>.
    /// </remarks>
    class SystemTypesCodeGenerator : ITSCodeGenerator
    {
        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor, ITSTypeFileBuilder builder, TypeScriptAttribute a )
        {
            if( builder.Type == typeof(Guid) ) builder.Finalizer = GenerateGuid;
            return true;
        }

        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool GenerateGuid( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart == null )
            {
                var part = file.EnsureTypePart( closer: "" );
                part.Append( @"
export class Guid {
    private readonly _guid: string;  
    
    constructor (public guid: string) {
        this._guid = guid;
    }

    get [Symbol.toStringTag]() {
        return this._guid;
      }
}" );
            }
            return true;
        }
    }
}
