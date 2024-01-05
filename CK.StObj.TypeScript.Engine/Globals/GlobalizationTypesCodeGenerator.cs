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

        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e ) => true;

        public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, TypeBuilderRequiredEventArgs builder )
        {
            if( builder.Type == typeof( SimpleUserMessage ) )
            {
                builder.TryWriteValueImplementation = SimpleUserMessageWrite;
                builder.Implementor = SimpleUserMessageCode;
            }
            else if( builder.Type == typeof( ExtendedCultureInfo ) )
            {
                builder.DefaultValueSource = "NormalizedCultureInfo.codeDefault";
                builder.TryWriteValueImplementation = ExtendedCultureInfoWrite;
                builder.Implementor = ExtendedCultureInfoCode;
            }
            else if( builder.Type == typeof( NormalizedCultureInfo ) )
            {
                builder.DefaultValueSource = "NormalizedCultureInfo.codeDefault";
                builder.TryWriteValueImplementation = NormalizedCultureInfoWrite;
                builder.Implementor = NormalizedCultureInfoCode;
            }
            return true;
        }

        static bool SimpleUserMessageWrite( ITSCodeWriter w, ITSGeneratedType t, object o  )
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

        static bool SimpleUserMessageCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.File.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );
                t.EnsureTypePart( closer: "" ).Append( """
                        /**
                         * Immutable simple info, warn or error message with an optional indentation.
                         **/
                        export class SimpleUserMessage
                        {
                            /**
                             * Initializes a new SimpleUserMessage.
                             * @param level Message level (info, warn or error). 
                             * @param message Message text. 
                             * @param depth Optional indentation. 
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

        static bool ExtendedCultureInfoWrite( ITSCodeWriter w, ITSGeneratedType t, object o )
        {
            if( o is ExtendedCultureInfo c )
            {
                w.Append( "new ExtendedCultureInfo( " ).AppendSourceString( c.Name ).Append( " )" );   
                return true;
            }
            return false;
        }

        static bool ExtendedCultureInfoCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.File.Imports.EnsureImport( monitor, typeof( NormalizedCultureInfo ) );
                t.EnsureTypePart( closer: "" ).Append( """
                        /**
                         * Mere encapsulation of the culture name..
                         **/
                        export class ExtendedCultureInfo {

                        constructor(public readonly name: string) {}
                            toString() { return this.name; }
                        }
                        """ );
            }
            return true;
        }

        static bool NormalizedCultureInfoWrite( ITSCodeWriter w, ITSGeneratedType t, object o )
        {
            if( o is NormalizedCultureInfo c )
            {
                if( c == NormalizedCultureInfo.CodeDefault )
                {
                    w.Append( "NormalizedCultureInfo.codeDefault" );
                }
                else
                {
                    w.Append( "new NormalizedCultureInfo( " ).AppendSourceString( c.Name ).Append( " )" );
                }
                return true;
            }
            return false;
        }

        static bool NormalizedCultureInfoCode( IActivityMonitor monitor, ITSGeneratedType t )
        {
            if( t.TypePart == null )
            {
                t.File.Imports.EnsureImport( monitor, typeof( ExtendedCultureInfo ) );
                t.EnsureTypePart( closer: "" ).Append( """
                        /**
                         * Only mirrors the C# side architecture: a normalized culture is an extended
                         * culture with a single culture name.
                         **/
                        export class NormalizedCultureInfo extends ExtendedCultureInfo {

                        /**
                         * Gets the code default culture ("en").
                         **/
                        static codeDefault: NormalizedCultureInfo = new NormalizedCultureInfo("en");
                        
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
