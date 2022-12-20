using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using CommunityToolkit.HighPerformance;
using System;
using System.Diagnostics;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    sealed class TSPocoTypeMap
    {
        readonly TypeScriptContext _context;
        readonly TSType[] _tsTypes;

        public TSPocoTypeMap( TypeScriptContext context, IPocoTypeSystem typeSystem )
        {
            _tsTypes = new TSType[ typeSystem.AllNonNullableTypes.Count ];
            _context = context;
        }

        public ITSType GetTSPocoType( IActivityMonitor monitor, IPocoType exchangeableType )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckArgument( exchangeableType?.IsExchangeable == true );
            var ts = _tsTypes[exchangeableType.Index >> 1];
            if( ts == null )
            {
                _tsTypes[exchangeableType.Index >> 1] = ts = BuildTSType( monitor, exchangeableType.NonNullable );
            }
            return exchangeableType.IsNullable ? ts.Nullable : ts;
        }

        TSType BuildTSType( IActivityMonitor monitor, IPocoType type )
        {
            Debug.Assert( !type.IsNullable );
            switch( type.Kind )
            {
                case PocoTypeKind.Dictionary:
                    {
                        var c = (ICollectionPocoType)type;
                        var tKey = GetTSPocoType( monitor, c.ItemTypes[0] );
                        var tValue = GetTSPocoType( monitor, c.ItemTypes[1] );
                        var typeName = $"Map<{tKey.TypeName},{tValue.TypeName}>";
                        var imports = tKey.RequiredImports.Combine( tValue.RequiredImports );
                        return new TSType( typeName, imports, $"new {typeName}()" );
                    }
                case PocoTypeKind.Array:
                case PocoTypeKind.List:
                    {
                        var c = (ICollectionPocoType)type;
                        var tItem = GetTSPocoType( monitor, c.ItemTypes[0] );
                        var typeName = $"Array<{tItem.TypeName}>";
                        return new TSType( typeName, tItem.RequiredImports, $"new {typeName}()" );
                    }
                case PocoTypeKind.HashSet:
                    {
                        var c = (ICollectionPocoType)type;
                        var tItem = GetTSPocoType( monitor, c.ItemTypes[0] );
                        var typeName = $"Set<{tItem.TypeName}>";
                        return new TSType( typeName, tItem.RequiredImports, $"new {typeName}()" );
                    }
                case PocoTypeKind.Record:
                case PocoTypeKind.IPoco:
                    {
                        var file = _context.DeclareTSType( monitor, type.Type );
                        return new TSType( file.TypeName, i => i.EnsureImport( file.File, file.TypeName ), $"new {file.TypeName}()" );
                    }
                case PocoTypeKind.AbstractIPoco:
                    {
                        var file = _context.DeclareTSType( monitor, type.Type );
                        return new TSType( file.TypeName, i => i.EnsureImport( file.File, file.TypeName ), null );
                    }
                case PocoTypeKind.Any:
                    return new TSType( "unknown", null, null );
                case PocoTypeKind.Basic:
                    {
                        var intrinsicName = KnownTSTypes.IntrinsicTypeName( type.Type );
                        if( intrinsicName != null )
                        {
                            var def = intrinsicName switch
                            {
                                "boolean" => "false",
                                "string" => "''",
                                "number" => "0",
                                "BigInt" => "0n",
                                _ => null
                            };
                            return new TSType( intrinsicName, null, def );
                        }
                        if( type.Type == typeof( DateTime ) || type.Type == typeof( DateTimeOffset ) )
                        {
                            return new TSType( "DateTime",
                                                i => i.EnsureImportFromLibrary( TypeScriptAspect.LuxonLib, "DateTime" ),
                                                "DateTime.utc(1,1,1)" );
                        }
                        if( type.Type == typeof( TimeSpan ) )
                        {
                            return new TSType( "Duration",
                                               i => i.EnsureImportFromLibrary( TypeScriptAspect.LuxonLib, "Duration" ),
                                               "Duration.fromMillis(0)" );
                        }
                        if( type.Type == typeof( Guid ) )
                        {
                            var file = _context.DeclareTSType( monitor, type.Type );
                            Debug.Assert( file.TypeName == "Guid");
                            return new TSType( file.TypeName, i => i.EnsureImport( file.File, file.TypeName ), "Guid.empty" );
                        }
                        return Throw.NotSupportedException<TSType>( type.ToString() );
                    }
                case PocoTypeKind.AnonymousRecord:
                    {
                        var r = (IRecordPocoType)type;
                        Action<ITSFileImportSection>? imports = null;
                        var bType = new StringBuilder();
                        var bDef = new StringBuilder();
                        bType.Append( '{' );
                        bDef.Append( '{' );
                        bool atLeastOne = false;
                        foreach( var f in r.Fields )
                        {
                            if( atLeastOne )
                            {
                                bType.Append( ", " );
                                bDef.Append( ", " );
                            }
                            atLeastOne = true;

                            var fType = GetTSPocoType( monitor, f.Type );
                            imports = imports.Combine( fType.RequiredImports );

                            bType.Append( f.Name );
                            if( fType.HasDefaultValue )
                            {
                                bDef.Append( fType.DefaultValueSource );
                            }
                            else
                            {
                                bType.Append( '?' );
                                bDef.Append( "undefined" );
                            }
                            bType.Append(": ");
                            bType.Append( fType.NonNullable.TypeName );
                        }
                        bType.Append( '}' );
                        bDef.Append( '}' );
                        return new TSType( bType.ToString(), imports, bDef.ToString() );
                    }
                case PocoTypeKind.Enum:
                    {
                        // Generation of the enumeration is done by default by
                        // TSTypeFile.Implement if the TypePart has not been generated
                        // by anyone.
                        var e = (IEnumPocoType)type;
                        var file = _context.DeclareTSType( monitor, type.Type );
                        var def = $"{file.TypeName}.{e.DefaultValueName}";
                        return new TSType( file.TypeName, i => i.EnsureImport( file.File, file.TypeName ), def );
                    }
                default: return Throw.NotSupportedException<TSType>( type.ToString() );
            }
        }
    }
}
