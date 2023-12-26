using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Linq;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="PocoTypeSystem"/>.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>,
    /// it is not initiated by an attribute like other code generators (that is generally done thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    public partial class PocoCodeGenerator : ITSCodeGenerator
    {
        internal PocoCodeGenerator( IPocoTypeSystem pocoTypeSystem )
        {
            PocoTypeSystem = pocoTypeSystem;
        }

        public IPocoTypeSystem PocoTypeSystem { get; }

        public bool Initialize( IActivityMonitor monitor, TypeScriptContext context ) => true;

        public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, TSTypeRequiredEventArgs e )
        {
            if( e.KeyType is IPocoType t )
            {
                // Important: the type nullability is projected here.
                bool isNullable = t.IsNullable;
                if( isNullable ) t = t.NonNullable;
                // "Actual types" (not anonymous records, collections and union types) are handled by their type.
                if( t.Kind is PocoTypeKind.Basic
                              or PocoTypeKind.Any
                              or PocoTypeKind.Enum
                              or PocoTypeKind.PrimaryPoco
                              or PocoTypeKind.SecondaryPoco
                              or PocoTypeKind.AbstractPoco
                              or PocoTypeKind.Record )
                {
                    var mapped = context.Root.TSTypes.ResolveTSType( monitor, t.Type );
                    e.Resolved = isNullable ? mapped.Nullable : mapped;
                    return true;
                }
                if( !CheckExchangeable( monitor, t ) ) return false;
                Throw.DebugAssert( "Handles array, list, set, dictionary, union and anonymous record here.",
                                    t.Kind is PocoTypeKind.Array
                                              or PocoTypeKind.List
                                              or PocoTypeKind.HashSet
                                              or PocoTypeKind.Dictionary
                                              or PocoTypeKind.UnionType
                                              or PocoTypeKind.AnonymousRecord );
                TSType ts;
                if( t is ICollectionPocoType tColl )
                {
                    switch( tColl.Kind )
                    {
                        case PocoTypeKind.Array:
                        case PocoTypeKind.List:
                            {
                                var inner = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                var tName = inner.TypeName + "[]";
                                ts = new TSType( tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), $"[]" );
                                break;
                            }
                        case PocoTypeKind.HashSet:
                            {
                                var inner = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                var tName = $"Set<{inner.TypeName}>";
                                ts = new TSType( tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), $"new {tName}()" );
                                break;
                            }
                        case PocoTypeKind.Dictionary:
                            {
                                var tKey = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                var tValue = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[1] );
                                var tName = $"Map<{tKey.TypeName},{tValue.TypeName}>";
                                ts = new TSType( tName,
                                                 i =>
                                                 {
                                                    tKey.EnsureRequiredImports( i.EnsureImport( tKey ) );
                                                    tValue.EnsureRequiredImports( i.EnsureImport( tValue ) );
                                                 },
                                                 $"new {tName}()" );
                                break;
                            }
                        default: throw new NotSupportedException( t.ToString() );
                    }
                }
                else if( t is IUnionPocoType tU )
                {
                    var types = tU.AllowedTypes.Select( t => context.Root.TSTypes.ResolveTSType( monitor, t ) ).ToList();
                    var b = new StringBuilder();
                    foreach( var u in types )
                    {
                        if( b.Length > 0 ) b.Append( '|' );
                        b.Append( u.TypeName );
                    }
                    var tName = b.ToString();
                    // Default value is not easy here :(.
                    // For the moment, consider the unknown... It's possible that we decide that union types
                    // should always be a nullable field (a always nullable reference type).
                    ts = new TSType( tName,
                                     i =>
                                     {
                                         foreach( var t in types )
                                         {
                                             t.EnsureRequiredImports( i.EnsureImport( t ) );
                                         }
                                     },
                                     defaultValue: "undefined" );
                }
                else
                {
                    Throw.DebugAssert( t is IRecordPocoType a && a.IsAnonymous );
                    var r = (IRecordPocoType)t;
                    if( r.Fields.All( f => f.IsUnnamed ) )
                    {
                        // Use typescript tuples.
                        var b = new StringBuilder();
                        b.Append( '[' );
                        foreach( var f in r.Fields )
                        {
                            if( b.Length > 1 ) b.Append( ", " );
                            AppendFieldDefinition()
                        }
                        b.Append( ']' );
                    }
                }
                e.Resolved = isNullable ? ts.Nullable : ts;
            }
            return true;
        }

        public bool ConfigureBuilder( IActivityMonitor monitor,
                                      TypeScriptContext context,
                                      TypeBuilderRequiredEventArgs builder )
        {
            var t = PocoTypeSystem.FindByType( builder.Type );
            if( t != null )
            {
                if( !CheckExchangeable( monitor, t ) ) return false;
                if( t.Kind == PocoTypeKind.SecondaryPoco )
                {
                    // Secondary interfaces are not visible outside:
                    // we simply map them to the primary type.
                    t = ((ISecondaryPocoType)t).PrimaryPocoType;
                }
                if( t.Kind == PocoTypeKind.PrimaryPoco )
                {
                    // For IPoco, if the TypeName is not set (which should almost never be set),
                    // we remove the 'I' if this is the interface name.
                    if( builder.TypeName == null )
                    {
                        var p = (INamedPocoType)t;
                        var typeName = p.ExternalOrCSharpName;
                        var iDot = typeName.LastIndexOf( '.' );
                        if( iDot >= 0 )
                        {
                            ++iDot;
                            if( p.ExternalName == null
                                && iDot < typeName.Length
                                && typeName[iDot] == 'I' )
                            {
                                ++iDot;
                            }
                            typeName = typeName.Substring( iDot );
                        }
                        builder.TypeName = typeName;
                    }
                    builder.DefaultValueSource = $"new {builder.TypeName}()";
                    builder.Implementor = ( monitor, tsType ) => GeneratePrimaryPoco( monitor, context, tsType, (IPrimaryPocoType)t );
                }
                else if( t.Kind == PocoTypeKind.AbstractPoco )
                {
                    // AbstractPoco are TypeScript interfaces. The default type name (with the leading 'I') is fine.
                    // There is no DefaultValueSource (let to null).
                    builder.Implementor = ( monitor, tsType ) => GenerateAbstractPoco( monitor, tsType, (IAbstractPocoType)t );
                }
                else if( t.Kind == PocoTypeKind.Record )
                {
                    builder.Implementor = ( monitor, tsType ) => GenerateRecord( monitor, tsType, (IRecordPocoType)t );
                }
                // Other PocoTypes are handled by their IPocoType.
            };
            return true;
        }

        /// <summary>
        /// Does nothing (it is the <see cref="ConfigureBuilder"/> method that sets a <see cref="TSGeneratedTypeBuilder.Implementor"/>). 
        /// </summary>
        /// <param name="monitor">Unused.</param>
        /// <param name="context">Unused.</param>
        /// <returns>Always true.</returns>
        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool GenerateRecord( IActivityMonitor monitor, ITSGeneratedType tsType, IRecordPocoType r )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.Append( "export class " ).Append( tsType.TypeName )
                .OpenBlock();
            return true;
        }

        bool GeneratePrimaryPoco( IActivityMonitor monitor, TypeScriptContext context, ITSGeneratedType tsType, IPrimaryPocoType p )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.Append( "export class " ).Append( tsType.TypeName )
                .CreateKeyedPart( out var interfaces, "interfaces" )
                .OpenBlock()
                .Append( "public constructor( " ).NewLine()
                .CreateKeyedPart( out var ctorParameters, "ctorParameters" )
                .Append( ") {}" );

            var atLeastOne = false;
            foreach( var f in p.Fields )
            {
                // Skips any non exchangeable fields.
                if( !p.IsExchangeable ) continue;

                var ts = context.Root.TSTypes.ResolveTSType( monitor, f.Type );
                if( atLeastOne )
                {
                    ctorParameters.Append( ", " ).NewLine();
                }
                atLeastOne = true;
                ctorParameters.Append( "public " );
                AppendFieldDefinition( ctorParameters, f, ts );
            }

            //foreach( var a in p.AbstractTypes )
            //{
            //    if( atLeastOne ) interfaces.Append( ", " );
            //    else
            //    {
            //        interfaces.Append( " implements " );
            //        atLeastOne = true;
            //    }
            //    interfaces.AppendTypeName( _pocoTypeMap.GetTSPocoType( monitor, a ) );
            //}
            //foreach( var f in p.Fields )
            //{
            //    // Skips any non exchangeable fields.
            //    if( !p.IsExchangeable ) continue;

            //    Debug.Assert( f.FieldAccess != PocoFieldAccessKind.ReadOnly, "Readonly fields are non exchangeable." );

            //    var tsFieldType = _pocoTypeMap.GetTSPocoType( monitor, f.Type );
            //    // Creates the backing field.
            //    fields.Append( "private " );
            //    if( f.FieldAccess == PocoFieldAccessKind.MutableCollection || f.FieldAccess == PocoFieldAccessKind.IsByRef )
            //    {
            //        // If it can be readonly, it should be.
            //        fields.Append( "readonly " );
            //    }
            //    fields.Append( f.PrivateFieldName ).Space().AppendTypeName( tsFieldType ).Append( ";" ).NewLine();

            //    defaultCtor.Append( "this." ).Append( f.PrivateFieldName ).Append( " = " );
            //    // If the field has a default value, use it. 
            //    if( f.HasOwnDefaultValue )
            //    {
            //        Debug.Assert( f.DefaultValueInfo.DefaultValue != null );
            //        defaultCtor.Append( f.DefaultValueInfo.DefaultValue.SimpleValue );
            //    }
            //    else
            //    {
            //        defaultCtor.Append( tsFieldType.DefaultValueSource );
            //    }

            //    // Creates the property.
            //    //if( f.FieldAccess == PocoFieldAccessKind.IsByRef )
            //    //{
            //    //    // A ref property is only the return of the ref backing field.
            //    //    tB.Append( "public ref " ).Append( f.Type.CSharpName ).Space().Append( f.Name )
            //    //      .Append( " => ref " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();
            //    //}
            //    //else
            //    //{
            //    //    // The getter is always the same.
            //    //    tB.Append( "public " ).Append( f.Type.CSharpName ).Space().Append( f.Name );
            //    //    if( f.FieldAccess != PocoFieldAccessKind.HasSetter )
            //    //    {
            //    //        Debug.Assert( f.FieldAccess == PocoFieldAccessKind.MutableCollection || f.FieldAccess == PocoFieldAccessKind.ReadOnly );
            //    //        // Readonly and MutableCollection doesn't require the "get".
            //    //        // This expose a public (read only) property that is required for MutableCollection but
            //    //        // a little bit useless for pure ReadOnly. However we need an implementation of the property
            //    //        // declared on the interface (we could have generated explicit implementations here but it
            //    //        // would be overcomplicated).
            //    //        tB.Append( " => " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();
            //    //    }
            //    //    else
            //    //    {
            //    //        // For writable properties we need the get/set. 
            //    //        tB.OpenBlock()
            //    //          .Append( "get => " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();

            //    //        tB.Append( "set" )
            //    //            .OpenBlock();
            //    //        // Always generate the null check.
            //    //        if( !f.Type.IsNullable && !f.Type.Type.IsValueType )
            //    //        {
            //    //            tB.Append( "Throw.CheckNotNullArgument( value );" ).NewLine();
            //    //        }
            //    //        // UnionType: check against the allowed types.
            //    //        if( f.Type is IUnionPocoType uT )
            //    //        {
            //    //            Debug.Assert( f.Type.Kind == PocoTypeKind.UnionType );
            //    //            // Generates the "static Type[] _vXXXAllowed" array.
            //    //            fieldPart.Append( "static readonly Type[] " ).Append( f.PrivateFieldName ).Append( "Allowed = " )
            //    //                     .AppendArray( uT.AllowedTypes.Select( u => u.Type ) ).Append( ";" ).NewLine();

            //    //            if( f.Type.IsNullable ) tB.Append( "if( value != null )" ).OpenBlock();

            //    //            // Generates the check.
            //    //            tB.Append( "Type tV = value.GetType();" ).NewLine()
            //    //              .Append( "if( !" ).Append( f.PrivateFieldName ).Append( "Allowed" )
            //    //              .Append( ".Any( t => t.IsAssignableFrom( tV ) ) )" )
            //    //                .OpenBlock()
            //    //                .Append( "Throw.ArgumentException( $\"Unexpected Type '{tV.ToCSharpName()}' in UnionType. Allowed types are: '" )
            //    //                .Append( uT.AllowedTypes.Select( tU => tU.CSharpName ).Concatenate( "', '" ) )
            //    //                .Append( "'.\");" )
            //    //                .CloseBlock();

            //    //            if( f.Type.IsNullable ) tB.CloseBlock();
            //    //        }
            //    //        tB.Append( f.PrivateFieldName ).Append( " = value;" )
            //    //            .CloseBlock()
            //    //          .CloseBlock();
            //    //    }
            //    //}
            //}

            return true;
        }

        static void AppendFieldDefinition( ITSCodeWriter ctorParameters, IPrimaryPocoField f, ITSType ts )
        {
            ctorParameters.AppendIdentifier( f.Name );
            if( f.Type.IsNullable ) ctorParameters.Append( "?" );
            ctorParameters.Append( ": " ).AppendTypeName( ts );
            // Note: a PocoField has necessarily an allowed or requires init default value.
            //       True IsDisallowed (no default value: the value must be provided) is handled here for
            //       record fields not used as a Poco field.
            var defInfo = f.DefaultValueInfo;
            if( !defInfo.IsDisallowed )
            {
                ctorParameters.Append( " = " );
                if( defInfo.RequiresInit && f.HasOwnDefaultValue )
                {
                    var defVal = defInfo.DefaultValue.SimpleValue;
                    if( defVal != null ) ts.WriteValue( ctorParameters, defVal );
                    else ctorParameters.Append( "new " ).Append( ts.TypeName ).Append( "()" );
                }
                else ctorParameters.Append( ts.DefaultValueSource );
            }
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IAbstractPocoType a )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.Append( "export interface " ).Append( tsType.TypeName )
                .OpenBlock()
                .CreatePart( out var body );
            return true;
        }

        static ITSKeyedCodePart? CreateTypePart( IActivityMonitor monitor, ITSGeneratedType tsType )
        {
            if( tsType.TypePart != null )
            {
                monitor.Error( $"Type part for '{tsType.Type:C}' in '{tsType.File}' has been initialized by another generator." );
                return null;
            }
            return tsType.EnsureTypePart();
        }

        static bool CheckExchangeable( IActivityMonitor monitor, IPocoType t )
        {
            if( !t.IsExchangeable )
            {
                monitor.Error( $"PocoType '{t}' has been marked as nor exchangeable." );
                return false;
            }
            return true;
        }

    }
}
