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
    public partial class TypeScriptPocoClass
    {
        internal void AppendPocoJsonSupport( IActivityMonitor monitor, TSTypeFile tsTypedFile, ITSKeyedCodePart b )
        {
            tsTypedFile.File.Imports.EnsureImportAllFromLibrary( "io-ts", "t" );
            tsTypedFile.File.Imports.EnsureImportFromLibrary( "fp-ts/lib/Either", "isLeft" );
            tsTypedFile.File.Imports.EnsureImportFromLibrary( "io-ts/PathReporter", "PathReporter" );
            AppendToPocoJsonMethod( monitor, b, tsTypedFile );
        }

        internal void AppendToPocoJsonMethod( IActivityMonitor monitor, ITSCodePart b, TSTypeFile tsTypedFile )
        {
            string iotsConstName = $"{TypeName}IOTS";

            b.Append( "public toPocoJson() {" ).NewLine().Append( $"const {iotsConstName} = t.strict(" ).Append( "{" );

            foreach( var prop in Properties )
            {
                var p = prop.CreateMethodParameter;
                if( p == null ) continue;

                b.Append( p.Name )
                 .Append( ":" );
                GetTypeIoTs( monitor, prop.PocoProperty, b, tsTypedFile.Context );
                b.Append( "," )
                .NewLine();

            }
            b.Append( "})" ).NewLine();

            b.Append( $"const decode = {iotsConstName}.decode(this);" ).NewLine();

            b.Append( "if(isLeft(decode)){" )
             .NewLine()
             .Append( "throw new Error(PathReporter.report(decode)[0])" )
             .NewLine()
             .Append( "}" )
             .NewLine();


            b.Append( $"const arr = [{TypeName},decode.right]" ).NewLine();

            b.Append( "JSON.stringify(arr)" ).NewLine();
            b.Append( "}" );

        }

        internal static void GetTypeIoTs( IActivityMonitor monitor, IPocoPropertyInfo p, ITSCodePart b, TypeScriptContext typeScriptContext )
        {
            if( p.IsReadOnly )
            {
                b.Append( "t.readonly(" );
            }

            if( p.IsUnionType )
            {
                b.Append( "t.union([" );
                foreach( var unionProperties in p.PropertyUnionTypes )
                {
                    b.AppendComplexIOTSTypeName( monitor, typeScriptContext, unionProperties );
                    if( p.PropertyUnionTypes.Last() != unionProperties ) b.Append( ',' );
                }

                if( p.IsNullable ) b.Append( ",t.undefined" );
                b.Append( "])" );

            }
            else
            {
                b.AppendComplexIOTSTypeName( monitor, typeScriptContext, p.PropertyNullableTypeTree );
            }

            //End of readonly
            if( p.IsReadOnly ) b.Append( ")" );
        }

    }
}
