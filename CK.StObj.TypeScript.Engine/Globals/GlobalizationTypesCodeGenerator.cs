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
        public bool Initialize( IActivityMonitor monitor, TypeScriptContext context ) => true;

        public bool ConfigureBuilder( IActivityMonitor monitor, TypeScriptContext context, TSGeneratedTypeBuilder builder )
        {
            if( builder.Type == typeof( SimpleUserMessage ) )
            {
                builder.TryWriteValueImplementation = SimpleUserMessageWrite;
                builder.Implementor = SimpleUserMessageCode;
            }
            else if( builder.Type == typeof( ExtendedCultureInfo ) )
            {
                builder.TryWriteValueImplementation = ExtendedCultureInfoWrite;
                builder.Implementor = ExtendedCultureInfoCode;
            }
            else if( builder.Type == typeof( NormalizedCultureInfo ) )
            {
                builder.TryWriteValueImplementation = NormalizedCultureInfoWrite;
                builder.Implementor = NormalizedCultureInfoCode;
            }
            return true;
        }

        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool SimpleUserMessageWrite( ITSCodeWriter w, object o  )
        {
            if( o is SimpleUserMessage m )
            {
                w.Append( "new SimpleUserMessage( " )
                 .Append( m.Level ).Append( ", " )
                 .AppendSourceString( m.Message ).Append( ", " )
                 .Append( m.Depth );
                return true;
            }
            return false;
        }

        bool SimpleUserMessageCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.File.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );
                t.EnsureTypePart( closer: "" ).Append( """
                        /**
                         * Simple info, warn or error message with an optional indentation.
                         **/
                        export class SimpleUserMessage
                        {
                            /**
                             * @param message Message level (info, warn or error). 
                             * @param message Message text. 
                             * @param message Optional indentation. 
                            **/
                            constructor(
                                public readonly level: UserMessageLevel,
                                public readonly message: string,
                                public readonly depth: number = 0
                            ) {}

                            toString() {
                                return '['+UserMessageLevel[this.level]+'] ' + this.message;
                        }
                        """ );
            }
            return true;
        }

        bool ExtendedCultureInfoWrite( ITSCodeWriter w, object o )
        {
            if( o is ExtendedCultureInfo c )
            {
                w.Append( "new ExtendedCultureInfo( " ).AppendSourceString( c.Name ).Append( " )" );   
                return true;
            }
            return false;
        }

        bool ExtendedCultureInfoCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.EnsureTypePart( closer: "" ).Append( """
                        export class ExtendedCultureInfo {
                            constructor(public readonly name: string) {}
                            toString() { return this.name; }
                        }
                        """ );
            }
            return true;
        }

        bool NormalizedCultureInfoWrite( ITSCodeWriter w, object o )
        {
            if( o is NormalizedCultureInfo c )
            {
                w.Append( "new NormalizedCultureInfo( " ).AppendSourceString( c.Name ).Append( " )" );   
                return true;
            }
            return false;
        }

        bool NormalizedCultureInfoCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.File.Imports.EnsureImport( monitor, typeof( ExtendedCultureInfo ) );
                t.EnsureTypePart( closer: "" ).Append( """
                        export class NormalizedCultureInfo extends ExtendedCultureInfo {
                            constructor(name: string) 
                            {
                                super(name);
                            }
                        }
                        """ );
            }
            return true;
        }

    }
}
