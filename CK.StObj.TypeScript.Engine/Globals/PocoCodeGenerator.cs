using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using static CK.CodeGen.TupleTypeName;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="IPocoTypeSystem"/>.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>,
    /// it is not initiated by an attribute like other code generators (that is generally done thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    sealed partial class PocoCodeGenerator : ITSCodeGenerator
    {
        readonly TypeScriptContext _typeScriptContext;
        readonly IPocoTypeSystem _pocoTypeSystem;

        IAbstractPocoType? _closedPocoType;
        ITSGeneratedType? _tsPocoType;

        IAbstractPocoType GetClosedPocoType() => _closedPocoType ??= _pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IClosedPoco ) )!;

        internal ITSGeneratedType GetTypeScriptPocoType( IActivityMonitor monitor )
        {
            if( _tsPocoType == null )
            {
                var pocoType = _pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IPoco ) );
                Throw.DebugAssert( pocoType != null );
                _tsPocoType = (ITSGeneratedType)_typeScriptContext.Root.TSTypes.ResolveTSType( monitor, pocoType );
            }
            return _tsPocoType;
        }

        internal PocoCodeGenerator( TypeScriptContext typeScriptContext, IPocoTypeSystem pocoTypeSystem )
        {
            _typeScriptContext = typeScriptContext;
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
                if( !CheckExchangeable( monitor, context, t ) )
                {
                    return false;
                }
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
                if( !CheckExchangeable( monitor, context, t ) )
                {
                    // This is an error: PocoType exchangeability must be resolved
                    // up front.
                    return false;
                }
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
                    // and this can lead to an infinite recursion.
                    // Field types resolution must be deferred: NamedRecordBuilder does this.
                    bool hasDefaultSet = builder.DefaultValueSource != null || builder.DefaultValueSourceProvider != null;
                    if( builder.Implementor == null || !hasDefaultSet )
                    {
                        var b = new NamedRecordBuilder( (IRecordPocoType)t, context );
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

        sealed class NamedRecordBuilder
        {
            readonly IRecordPocoType _type;
            readonly TypeScriptContext _typeScriptContext;
            FieldsWriter? _fieldsWriter;

            internal NamedRecordBuilder( IRecordPocoType type, TypeScriptContext context )
            {
                _type = type;
                _typeScriptContext = context;
            }

            FieldsWriter GetFieldsWriter( IActivityMonitor monitor ) => _fieldsWriter ??= FieldsWriter.Create( monitor, _type, false, _typeScriptContext.Root );

            internal bool GenerateRecord( IActivityMonitor monitor, ITSGeneratedType type )
            {
                return GetFieldsWriter( monitor ).GenerateRecordType( monitor, _typeScriptContext, _type, type );
            }

            internal string? GetDefaultValueSource( IActivityMonitor monitor, ITSGeneratedType type )
            {
                return GetFieldsWriter( monitor ).HasDefault
                        ? $"new {type.TypeName}()"
                        : null;
            }
        }

        bool GeneratePrimaryPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IPrimaryPocoType t )
        {
            Throw.DebugAssert( !tsType.IsNullable && !t.IsNullable );

            TypeScriptRoot root = tsType.File.Root;

            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;

            IEnumerable<IAbstractPocoType> implementedInterfaces = t.MinimalAbstractTypes;
            if( t.FamilyInfo.IsClosedPoco )
            {
                implementedInterfaces = implementedInterfaces.Append( GetClosedPocoType() );
            }

            part.CreatePart( out var documentationPart )
                .Append( "export class " ).Append( tsType.TypeName ).CreatePart( out var interfacesPart )
                .OpenBlock()
                .Append( "public constructor(" ).CreatePart( out var ctorParametersPart ).Append( ")" ).NewLine()
                .Append( "{" ).CreatePart( out var ctorBodyPart ).Append( "}" ).NewLine()
                // The get pocoTypeModel() returns a static (shared pocoTypeModel instance).
                .Append( "get pocoTypeModel() { return " )
                .Append( tsType.TypeName ).Append( "._m; }" ).NewLine()
                // The pocoTypeModel is extensible. 
                .Append( "private static readonly _m = {" ).NewLine()
                .Append( "isNamedRecord: false," ).NewLine()
                .CreatePart( out var pocoTypeModelPart )
                .Append( "};" ).NewLine();

            var fieldsWriter = FieldsWriter.Create( monitor, t, false, root );
            Throw.DebugAssert( fieldsWriter.HasDefault );
            // This sorts the fields and retrieves the TSField list that can be altered (skipping fields
            // and altering documentation) by other generators.
            var fields = fieldsWriter.GetPocoFields();
            // Extends the pocoTypeModel with names and fields meta data.
            fieldsWriter.WritePocoTypeModel( monitor, pocoTypeModelPart, t );
            // Raises the event.
            var e = new GeneratingPrimaryPocoEventArgs( monitor,
                                                        tsType,
                                                        t,
                                                        implementedInterfaces,
                                                        fields,
                                                        pocoTypeModelPart,
                                                        interfacesPart,
                                                        ctorParametersPart,
                                                        ctorBodyPart,
                                                        part );

            _typeScriptContext.RaiseGeneratingPrimaryPoco( e );

            documentationPart.AppendDocumentation( monitor, e.ClassDocumentation, e.DocumentationExtension );
            WriteInterfacesAndBrand( monitor,
                                     root,
                                     interfacesPart,
                                     isPrimaryPoco: true,
                                     implementedInterfaces,
                                     isIPoco: false,
                                     t,
                                     tsType,
                                     part );
            for( int i = 0; i < fields.Length; i++ )
            {
                var f = fields[ i ];
                if( i == 0 ) ctorParametersPart.NewLine();
                else ctorParametersPart.Append( ", " ).NewLine();
                if( !f.Skip ) f.WriteFieldDefinition( tsType.File, ctorParametersPart );
            }
            return true;
        }

        void WriteInterfacesAndBrand( IActivityMonitor monitor,
                                      TypeScriptRoot root,
                                      ITSCodePart interfaces,
                                      bool isPrimaryPoco,
                                      IEnumerable<IAbstractPocoType> abstracts,
                                      bool isIPoco,
                                      IPocoType pocoType,
                                      ITSGeneratedType tsType,
                                      ITSCodePart bodyPart )
        {
            bool atLeastOne = false;
            bodyPart.Append( "readonly _brand" ).Append( isPrimaryPoco ? "!: " : ": " ); 
            foreach( var a in abstracts )
            {
                // Handles base interfaces.
                if( atLeastOne ) interfaces.Append( ", " );
                else
                {
                    interfaces.Append( isPrimaryPoco ? " implements " : " extends " );
                }
                ITSType i = root.TSTypes.ResolveTSType( monitor, a );
                interfaces.AppendTypeName( i );

                // Handles branding.
                if( atLeastOne ) bodyPart.Append( " & " );
                bodyPart.Append( i.TypeName ).Append( "[\"_brand\"]" );

                atLeastOne = true;
            }
            if( !atLeastOne && !isIPoco )
            {
                var iPoco = GetTypeScriptPocoType( monitor );
                interfaces.Append( isPrimaryPoco ? " implements " : " extends " )
                          .AppendTypeName( iPoco );
                bodyPart.Append( iPoco.TypeName ).Append( "[\"_brand\"]" );
            }
            bodyPart.Append( " & {\"" ).Append( (pocoType.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IAbstractPocoType a )
        {
            if( tsType.TypeName == "IPoco" )
            {
                return GenerateIPoco( monitor, tsType );
            }
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;

            var root = tsType.File.Root;
            part.CreatePart( out var docPart )
                .Append( "export interface " ).Append( tsType.TypeName ).CreatePart( out var interfacesPart )
                .OpenBlock();
            foreach( var f in a.Fields )
            {
                if( !_typeScriptContext.IsExchangeable( f.Type ) ) continue;
                if( root.DocBuilder.GenerateDocumentation )
                {
                    part.AppendDocumentation( monitor, f.Originator );
                }
                part.AppendIdentifier( f.Name );
                if( f.Type.IsNullable ) part.Append( "?" );
                part.Append( ": " ).AppendTypeName( root.TSTypes.ResolveTSType( monitor, f.Type.NonNullable ) ).Append(";").NewLine();
            }

            var e = new GeneratingAbstractPocoEventArgs( monitor, tsType, a, a.MinimalGeneralizations, interfacesPart, part );
            _typeScriptContext.RaiseGeneratingAbstractPoco( e );

            docPart.AppendDocumentation( monitor, e.TypeDocumentation, e.DocumentationExtension );
            WriteInterfacesAndBrand( monitor,
                                     root,
                                     interfacesPart,
                                     isPrimaryPoco: false,
                                     e.ImplementedInterfaces,
                                     isIPoco: tsType.Type == typeof( IPoco ),
                                     a,
                                     tsType,
                                     part );

            return true;
        }

        static bool GenerateIPoco( IActivityMonitor monitor, ITSGeneratedType tsType )
        {
            var part = CreateTypePart( monitor, tsType, closer: "" );
            if( part == null ) return false;
            part.Append( """
                    /**
                     * Base interface for all IPoco types.
                     * IPocoType can be INamedRecord or IPoco: they have a name and fields.
                     * Anonymous records (C# value tuples) have no name: they have no model.
                     **/
                    export interface IPocoType {
                        /**
                         * Gets the Poco Type description. 
                         **/
                        readonly pocoTypeModel: IPocoTypeModel;
                    
                        readonly _brand: {};
                    }

                    /**
                     * Base interface for all IPoco types (Abstract that are TypeScript interfaces
                     * or PrimaryPoco that are TypeScript classes).
                     **/
                    export interface IPoco extends IPocoType {
                        readonly _brand: {"IPoco": any};
                    }

                    /**
                     * Base interface for all INamedRecord types (C# struct).
                     **/
                    export interface INamedRecord extends IPocoType {
                        readonly _brand: {"INamedRecord": any};
                    }

                    /**
                     * Describes a IPoco type. 
                     **/
                    export interface IPocoTypeModel {
                        /**
                         * Gets whether this is a INamedRecord or a IPoco. 
                         **/
                        readonly isNamedRecord: boolean;
                                            /**
                         * Gets the name of the Poco. 
                         **/
                        readonly name: string;

                        /**
                         * Gets a short name based on its unique index in
                         * the Poco system. 
                         **/
                        readonly idxName: string;
                    }
                                        
                    """ );
            return true;
        }

        static ITSKeyedCodePart? CreateTypePart( IActivityMonitor monitor, ITSGeneratedType tsType, string closer = "}\n" )
        {
            if( tsType.TypePart != null )
            {
                monitor.Error( $"Type part for '{tsType.Type:C}' in '{tsType.File}' has been initialized by another generator." );
                return null;
            }
            return tsType.EnsureTypePart( closer );
        }

        static bool CheckExchangeable( IActivityMonitor monitor, TypeScriptContext context, IPocoType t )
        {
            if( !context.IsExchangeable( t ) )
            {
                if( !t.IsExchangeable )
                {
                    monitor.Error( $"PocoType '{t}' has been marked as not exchangeable." );
                }
                else
                {
                    monitor.Error( $"PocoType '{t}' has been marked as not exchangeable in the Json map: " +
                                   $"a Poco type that is not Json serializable (when Json exchange is available) cannot be exchanged." );
                }
                return false;
            }
            return true;
        }
    }
}
