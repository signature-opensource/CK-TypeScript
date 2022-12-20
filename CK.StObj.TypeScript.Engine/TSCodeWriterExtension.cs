using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using CK.Setup;
using CK.Core;
using CK.StObj.TypeScript.Engine;
using CK.CodeGen;
using System.Numerics;

namespace CK.TypeScript.CodeGen
{
    /// <summary>
    /// Provides Append fluent extension methods to <see cref="ITSCodeWriter"/> specializations.
    /// </summary>
    public static class TSCodeWriterExtensions
    {
        /// <summary>
        /// Appends an enum definition. The underlying type should be safely convertible into Int32.
        /// </summary>
        /// <typeparam name="T">Actual type of the code writer.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="enumType">The enum type.</param>
        /// <param name="typeName">The TypeScript type name.</param>
        /// <param name="export">True to prefix with 'export ' the enum definition.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        static public T AppendEnumDefinition<T>( this T @this, IActivityMonitor monitor, Type enumType, string typeName, bool export ) where T : ITSCodeWriter
        {
            Throw.CheckArgument( enumType.IsEnum );
            var uT = enumType.GetEnumUnderlyingType();
            if( uT == typeof(UInt32) || uT == typeof(Int64) || uT == typeof(UInt64) )
            {
                monitor.Error( $"Enum: {enumType.Name} cannot be generated as TypeScript since it is based on {uT.Name} type. Only types that can be safely converted into Int32 should be used." );
                return @this;
            }
            string? docValuePrefix = null;
            var xDoc = @this.File.Folder.Root.GenerateDocumentation
                        ? XmlDocumentationReader.GetXmlDocumentation( monitor, enumType.Assembly, @this.File.Folder.Root.Memory )
                        : null;
            if( xDoc != null )
            {
                @this.AppendDocumentation( xDoc, enumType );
                docValuePrefix = XmlDocumentationReader.GetNameAttributeValueFor( "F:", enumType );
            }
            return @this.Append( export ? "export enum " : "enum " ).Append( typeName )
                        .OpenBlock()
                        .Append( b =>
                        {
                            string[] names = Enum.GetNames( enumType );
                            int[] values = Enum.GetValues( enumType ).Cast<object>().Select( x => Convert.ToInt32( x ) ).ToArray();

                            for( int i = 0; i < names.Length; ++i )
                            {
                                if( i > 0 ) b.Append( "," ).NewLine();

                                var n = names[i];
                                if( xDoc != null )
                                {
                                    Debug.Assert( docValuePrefix != null );
                                    b.AppendDocumentation( XmlDocumentationReader.GetDocumentationElement( xDoc, docValuePrefix + '.' + n ) );
                                }
                                b.Append( n ).Append( " = " ).Append( values[i] );
                            }
                        } )
                        .CloseBlock();
        }

        /// <summary>
        /// Appends a type name defined in a <see cref="TSTypeFile"/>: the type is automatically
        /// imported from the file (in the <see cref="TypeScriptFile.Imports"/>).
        /// </summary>
        /// <typeparam name="T">The code writer type.</typeparam>
        /// <param name="this">This code writer.</param>
        /// <param name="t">The type to import.</param>
        /// <returns>This code writer to enable fluent syntax.</returns>
        public static T AppendImportedTypeName<T>( this T @this, TSTypeFile t ) where T : ITSCodeWriter
        {
            @this.File.Imports.EnsureImport( t.File, t.TypeName );
            @this.Append( t.TypeName );
            return @this;
        }

    }
}
