using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="_pocoTypeSystem"/>.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>,
    /// it is not initiated by an attribute like other code generators (that is generally done thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    public partial class PocoCodeGenerator : ITSCodeGenerator
    {
        readonly IPocoTypeSystem _pocoTypeSystem;

        IAbstractPocoType? _closedPocoType;
        IAbstractPocoType? _pocoType;
        ITSType? _tsPocoType;

        IAbstractPocoType GetClosedPocoType() => _closedPocoType ??= _pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IClosedPoco ) )!;

        IAbstractPocoType GetPocoType() => _pocoType ??= _pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IPoco ) )!;

        ITSType GetTSPocoType( IActivityMonitor monitor, TypeScriptRoot root ) => _tsPocoType ??= root.TSTypes.ResolveTSType( monitor, GetPocoType() );


        internal PocoCodeGenerator( IPocoTypeSystem pocoTypeSystem )
        {
            _pocoTypeSystem = pocoTypeSystem;
        }

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
                    e.ResolvedType = isNullable ? mapped.Nullable : mapped;
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
                    var types = tU.AllowedTypes.Select( t => context.Root.TSTypes.ResolveTSType( monitor, t ) ).ToArray();
                    var b = new StringBuilder();
                    foreach( var u in types )
                    {
                        if( b.Length > 0 ) b.Append( '|' );
                        b.Append( u.NonNullable.TypeName );
                    }
                    var tName = b.ToString();
                    ts = new TSUnionType( tName,
                                          i =>
                                          {
                                                foreach( var t in types )
                                                {
                                                    i.EnsureImport( t );
                                                }
                                          },
                                          types );
                }
                else
                {
                    Throw.DebugAssert( t is IRecordPocoType a && a.IsAnonymous );
                    var r = (IRecordPocoType)t;
                    Throw.DebugAssert( r.IsExchangeable == r.Fields.Any( f => f.IsExchangeable ) );
                    var fieldsWriter = FieldsWriter.Create( monitor, r, true, context.Root );
                    ts = fieldsWriter.CreateAnonymousRecordType( monitor );
                }
                e.ResolvedType = isNullable ? ts.Nullable : ts;
            }
            return true;
        }

        public bool OnResolveType( IActivityMonitor monitor,
                                   TypeScriptContext context,
                                   TypeBuilderRequiredEventArgs builder )
        {
            var t = _pocoTypeSystem.FindByType( builder.Type );
            if( t != null )
            {
                if( !CheckExchangeable( monitor, t ) ) return false;
                if( t.Kind == PocoTypeKind.SecondaryPoco )
                {
                    // Secondary interfaces are not visible outside:
                    // we simply map them to the primary type. We resolve the C# type here to avoid
                    // a useless object => IPocoType.Type intermediate step.
                    builder.ResolvedType = context.Root.TSTypes.ResolveTSType( monitor, ((ISecondaryPocoType)t).PrimaryPocoType.Type );
                    return true;
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
                    // If someone else set this already (again, this should almost never be set),
                    // let's preserve its decision.
                    builder.DefaultValueSource ??= $"new {builder.TypeName}()";
                    // This one may be more common: implementaion may be subsituted.
                    builder.Implementor ??= ( monitor, tsType ) => GeneratePrimaryPoco( monitor, tsType, (IPrimaryPocoType)t );
                }
                else if( t.Kind == PocoTypeKind.AbstractPoco )
                {
                    var tAbstract = (IAbstractPocoType)t;
                    // AbstractPoco are TypeScript interfaces in their own file... except for generics:
                    // ICommand<string> or ICommand<List<IMission>> are TSType (without implementation file). 
                    if( tAbstract.IsGenericType )
                    {
                        // There is no default value for abstraction.
                        // We must only build a name with its required imports.
                        var b = context.Root.GetTSTypeBuilder();
                        var genDef = context.Root.TSTypes.ResolveTSType( monitor, tAbstract.GenericTypeDefinition.Type );
                        b.TypeName.AppendTypeName( genDef ).Append( "<" );
                        foreach( var arg in tAbstract.GenericArguments )
                        {
                            b.TypeName.AppendTypeName( context.Root.TSTypes.ResolveTSType( monitor, arg.Type ) );
                        }
                        b.TypeName.Append( ">" );
                        builder.ResolvedType = b.Build();
                    }
                    else
                    {
                        // The default type name (with the leading 'I') is fine.
                        // There is no DefaultValueSource (let to null).
                        // Here also, if it happens to be set, don't do anything.
                        builder.Implementor ??= ( monitor, tsType ) => GenerateAbstractPoco( monitor, tsType, tAbstract );
                    }
                }
                else if( t.Kind == PocoTypeKind.Record )
                {
                    // We use the standard default type name for record.
                    // Even if the default value is simply $"new TypeName()", to know IF there is
                    // a default (i.e. all fields have a defaults), one need to resolve the field types
                    // and this can lead to an infinite recursion. Field types resolution must be deferred.
                    bool hasDefaultSet = builder.DefaultValueSource != null || builder.DefaultValueSourceProvider != null;
                    if( builder.Implementor == null || !hasDefaultSet )
                    {
                        var b = new NamedRecordBuilder( (IRecordPocoType)t, context.Root );
                        builder.Implementor ??= b.GenerateRecord;
                        if( !hasDefaultSet ) builder.DefaultValueSourceProvider = b.GetDefaultValueSource;
                    }
                }
                // Other PocoTypes are handled by their IPocoType.
            }
            return true;
        }

        /// <summary>
        /// Does nothing (it is the <see cref="OnResolveType"/> method that sets a <see cref="TSGeneratedTypeBuilder.Implementor"/>). 
        /// </summary>
        /// <param name="monitor">Unused.</param>
        /// <param name="context">Unused.</param>
        /// <returns>Always true.</returns>
        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool GeneratePrimaryPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IPrimaryPocoType t )
        {
            TypeScriptRoot root = tsType.File.Root;

            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;

            // Unifies documentation from primary and secondaries definitions.
            part.AppendDocumentation( monitor, t.SecondaryTypes.Select( s => s.Type ).Prepend( t.Type ) );
            // Class with a single constructor (using TypeScript "Parameter Properties").
            part.Append( "export class " ).Append( tsType.TypeName );
            IEnumerable<IAbstractPocoType> bases = t.MinimalAbstractTypes;
            if( t.FamilyInfo.IsClosedPoco )
            {
                bases = bases.Append( GetClosedPocoType() );
            }
            WriteInterfaces( monitor, root, part, true, bases );
            part.OpenBlock()
                .Append( "public constructor( " ).NewLine();
            var fieldsWriter = FieldsWriter.Create( monitor, t, false, root );
            Throw.DebugAssert( fieldsWriter.HasDefault );
            fieldsWriter.WriteCtorParameters( monitor, tsType.File, part );
            part.NewLine().Append( ") {}" );
            return true;
        }


        void WriteInterfaces( IActivityMonitor monitor,
                              TypeScriptRoot root,
                              ITSCodePart interfaces,
                              bool implements,
                              IEnumerable<IAbstractPocoType> abstracts )
        {
            bool atLeastOne = false;
            var implied = new HashSet<IAbstractPocoType>( abstracts.SelectMany( a => a.Generalizations ) );
            foreach( var a in abstracts )
            {
                if( implied.Contains( a ) )
                {
                    continue;
                }
                if( atLeastOne ) interfaces.Append( ", " );
                else
                {
                    interfaces.Append( implements ? " implements " : " extends " );
                    atLeastOne = true;
                }
                interfaces.AppendTypeName( root.TSTypes.ResolveTSType( monitor, a ) );
            }
            if( !atLeastOne )
            {
                interfaces.Append( implements ? " implements " : " extends " )
                          .AppendTypeName( GetTSPocoType( monitor, root ) );
            }
        }

        sealed class NamedRecordBuilder
        {
            readonly IRecordPocoType _type;
            readonly TypeScriptRoot _root;
            FieldsWriter? _fieldsWriter;

            internal NamedRecordBuilder( IRecordPocoType type, TypeScriptRoot root )
            {
                _type = type;
                _root = root;
            }

            FieldsWriter GetFieldsWriter( IActivityMonitor monitor ) => _fieldsWriter ??= FieldsWriter.Create( monitor, _type, false, _root );

            internal bool GenerateRecord( IActivityMonitor monitor, ITSGeneratedType type )
            {
                return GetFieldsWriter( monitor ).GenerateRecordType( monitor, type );
            }

            internal string? GetDefaultValueSource( IActivityMonitor monitor, ITSGeneratedType type )
            {
                return GetFieldsWriter( monitor ).HasDefault
                        ? $"new {type.TypeName}()"
                        : null;
            }
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IAbstractPocoType a )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.AppendDocumentation( monitor, tsType.Type );

            part.Append( "export interface " ).Append( tsType.TypeName );
            var root = tsType.File.Root;
            if( tsType.Type != typeof( IPoco ) )
            {
                WriteInterfaces( monitor, root, part, false, a.MinimalGeneralizations );
            }
            part.OpenBlock();
            foreach( var f in a.Fields )
            {
                if( !f.Type.IsExchangeable ) continue;
                if( root.DocBuilder.GenerateDocumentation )
                {
                    part.AppendDocumentation( monitor, f.Originator );
                }
                part.AppendIdentifier( f.Name );
                if( f.Type.IsNullable ) part.Append( "?" );
                part.Append( ": " ).AppendTypeName( root.TSTypes.ResolveTSType( monitor, f.Type.NonNullable ) );
            }
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
                monitor.Error( $"PocoType '{t}' has been marked as not exchangeable." );
                return false;
            }
            return true;
        }
    }
}
