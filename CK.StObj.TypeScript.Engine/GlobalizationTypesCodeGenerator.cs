using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Provides an implementation for Globalization types.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/>.
    /// </remarks>
    sealed class GlobalizationTypesCodeGenerator : ITSCodeGenerator
    {
        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor, ITSTypeFileBuilder builder, TypeScriptAttribute a )
        {
            if( builder.Type == typeof( SimpleUserMessage ) ) builder.Finalizer = GenerateSimpleUserMessage;
            if( builder.Type == typeof( NormalizedCultureInfo ) ) builder.Finalizer = GenerateNormalizedCultureInfo;
            if( builder.Type == typeof( ExtendedCultureInfo ) ) builder.Finalizer = GenerateExtendedCultureInfo;
            return true;
        }

        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool GenerateSimpleUserMessage( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart == null )
            {
                var userMessageLevel = file.Context.DeclareTSType( monitor, typeof( UserMessageLevel ) );
                Debug.Assert( userMessageLevel != null );
                file.File.Imports.EnsureImport( userMessageLevel.File, userMessageLevel.TypeName );

                var part = file.EnsureTypePart( closer: "" );
                part.Append( @"
export class SimpleUserMessage {
    
    constructor(public readonly level: UserMessageLevel, public readonly message: string, public readonly depth: number) {}

    toString() {
        return '['+UserMessageLevel[this.level]+'] ' + this.message;
      }
}" );
            }
            return true;
        }

        bool GenerateExtendedCultureInfo( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart == null )
            {
                var part = file.EnsureTypePart( closer: "" );
                part.Append( @"
export class ExtendedCultureInfo {
    constructor(public readonly name: string) {}
    toString() { return this.name; }
}" );
            }
            return true;
        }

        bool GenerateNormalizedCultureInfo( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart == null )
            {
                var extendedCultureInfo = file.Context.DeclareTSType( monitor, typeof( ExtendedCultureInfo ) );
                Debug.Assert( extendedCultureInfo != null );
                file.File.Imports.EnsureImport( extendedCultureInfo.File, extendedCultureInfo.TypeName );
                var part = file.EnsureTypePart( closer: "" );
                part.Append( @"
export class NormalizedCultureInfo extends ExtendedCultureInfo {
    constructor(name: string) 
    {
        super(name);
    }
}" );
            }
            return true;
        }


    }
}
