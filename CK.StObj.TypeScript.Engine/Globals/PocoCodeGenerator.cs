using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace CK.StObj.TypeScript.Engine
{

    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="IPocoTypeSystem"/>.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>,
    /// it is not initiated by an attribute like other code generators (that is generally done thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    sealed partial class PocoCodeGenerator : ITSPocoCodeGenerator, ITSCodeGenerator
    {
        readonly TypeScriptContext _typeScriptContext;
        readonly IPocoTypeSystem _pocoTypeSystem;
        [AllowNull] TSPocoModel _tsPocoModel;
        IAbstractPocoType? _closedPocoType;

        internal PocoCodeGenerator( TypeScriptContext typeScriptContext, IPocoTypeSystem pocoTypeSystem )
        {
            _typeScriptContext = typeScriptContext;
            _pocoTypeSystem = pocoTypeSystem;
        }

        internal void Initialize( IActivityMonitor monitor )
        {
            _tsPocoModel = new TSPocoModel( monitor, _typeScriptContext );
        }

        public TSPocoModel PocoModel => _tsPocoModel;

        public event EventHandler<GeneratingPrimaryPocoEventArgs>? PrimaryPocoGenerating;

        void RaiseGeneratingPrimaryPoco( GeneratingPrimaryPocoEventArgs e ) => PrimaryPocoGenerating?.Invoke( this, e );

        public event EventHandler<GeneratingAbstractPocoEventArgs>? AbstractPocoGenerating;

        void RaiseGeneratingAbstractPoco( GeneratingAbstractPocoEventArgs e ) => AbstractPocoGenerating?.Invoke( this, e );

        public event EventHandler<GeneratingNamedRecordPocoEventArgs>? NamedRecordPocoGenerating;

        void RaiseGeneratingNamedRecordPoco( GeneratingNamedRecordPocoEventArgs e ) => NamedRecordPocoGenerating?.Invoke( this, e );

        public bool IsExchangeable( IPocoType type )
        {
            if( _typeScriptContext.JsonNames != null )
            {
                return _typeScriptContext.JsonNames.IsExchangeable( type );
            }
            return type.IsExchangeable;
        }

        IAbstractPocoType GetClosedPocoType() => _closedPocoType ??= _pocoTypeSystem.FindByType<IAbstractPocoType>( typeof( IClosedPoco ) )!;

        /// <summary>
        /// Does nothing (it is the <see cref="OnResolveType"/> method that sets a <see cref="TSGeneratedTypeBuilder.Implementor"/>). 
        /// </summary>
        bool ITSCodeGenerator.GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool ITSCodeGenerator. Initialize( IActivityMonitor monitor, TypeScriptContext context ) => true;

        bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e )
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
                if( !CheckExchangeable( monitor, t ) )
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
                TSBasicType ts;
                if( t is ICollectionPocoType tColl )
                {
                    switch( tColl.Kind )
                    {
                        case PocoTypeKind.Array:
                        case PocoTypeKind.List:
                            {
                                var inner = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                // Uses Array<> rather than "(inner.TypeName)[]".
                                var tName = $"Array<{inner.TypeName}>";
                                ts = new TSBasicType( tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), "[]" );
                                break;
                            }
                        case PocoTypeKind.HashSet:
                            {
                                var inner = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                var tName = $"Set<{inner.TypeName}>";
                                ts = new TSBasicType( tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), $"new {tName}()" );
                                break;
                            }
                        case PocoTypeKind.Dictionary:
                            {
                                var tKey = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                var tValue = context.Root.TSTypes.ResolveTSType( monitor, tColl.ItemTypes[1] );
                                var tName = $"Map<{tKey.TypeName},{tValue.TypeName}>";
                                ts = new TSBasicType( tName,
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
                    var fieldsWriter = FieldsWriter.Create( monitor, r, true, context );
                    ts = fieldsWriter.CreateAnonymousRecordType( monitor );
                }
                e.ResolvedType = isNullable ? ts.Nullable : ts;
            }
            return true;
        }

        bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder )
        {
            // Hook the IPoco type to generate the IPoco.ts (with empty PartCloser) even
            // if the PocoTypeSystem is empty.
            if( builder.Type == typeof( IPoco ) )
            {
                // There are multiple types.
                builder.PartCloser = "";
                // Avoid a "missing implementor" warning.
                builder.Implementor ??= static ( monitor, tsType ) => true;
                return true;
            }
            var t = _pocoTypeSystem.FindByType( builder.Type );
            if( t != null )
            {
                if( !CheckExchangeable( monitor, t ) )
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
                        var b = context.Root.GetTSTypeSignatureBuilder();
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
                        var b = new NamedRecordBuilder( (IRecordPocoType)t, this );
                        builder.Implementor ??= b.GenerateRecord;
                        if( !hasDefaultSet ) builder.DefaultValueSourceProvider = b.GetDefaultValueSource;
                    }
                }
                // Other PocoTypes are handled by their IPocoType.
            }
            return true;
        }

        sealed class NamedRecordBuilder
        {
            readonly IRecordPocoType _type;
            readonly PocoCodeGenerator _generator;
            FieldsWriter? _fieldsWriter;

            internal NamedRecordBuilder( IRecordPocoType type, PocoCodeGenerator generator )
            {
                _type = type;
                _generator = generator;
            }

            internal bool GenerateRecord( IActivityMonitor monitor, ITSFileCSharpType tsType )
            {
                _fieldsWriter ??= FieldsWriter.Create( monitor, _type, false, _generator._typeScriptContext );
                // INamedRecord is a pure TS type defined in IPoco.ts.
                tsType.File.Imports.EnsureImport( _generator.PocoModel.IPocoType.File, "INamedRecord" );
                tsType.TypePart.CreatePart( out var documentationPart )
                    .Append( "export class " ).Append( tsType.TypeName ).Append( " implements INamedRecord" )
                    .OpenBlock()
                    .Append( "public constructor(" ).CreatePart( out var ctorParametersPart ).Append( ")" ).NewLine()
                    .Append( "{" )
                    .CreatePart( out var ctorBodyPart )
                    .Append( "}" ).NewLine()
                    .Append( "readonly _brand!: INamedRecord[\"_brand\"] & {\"" ).Append( (_type.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine()
                    .CreatePart( out var pocoTypeModelPart )
                    .Append( "};" ).NewLine();

                // This sorts the fields and retrieves the TSField list that can be altered (skipping fields
                // and altering documentation) by other generators.
                // TSField.PocoTypeModelPart enables field models to be extended.
                var fields = _fieldsWriter.Value.WritePocoTypeModelAndGetPocoFields( pocoTypeModelPart, tsType );

                // Raises the event.
                var e = new GeneratingNamedRecordPocoEventArgs( monitor,
                                                                _generator._tsPocoModel,
                                                                tsType,
                                                                _type,
                                                                fields,
                                                                pocoTypeModelPart,
                                                                ctorParametersPart,
                                                                ctorBodyPart );

                _generator.RaiseGeneratingNamedRecordPoco( e );

                if( _generator._typeScriptContext.Root.DocBuilder.GenerateDocumentation )
                {
                    var xE = XmlDocumentationReader.GetDocumentationFor( monitor, tsType.Type, _generator._typeScriptContext.Root.Memory );
                    if( xE != null && xE.Elements( "param" ).Any() )
                    {
                        // Clones the element and removes any param
                        // elements from it (for record constructor syntax).
                        xE = new XElement( xE );
                        xE.Elements( "param" ).Remove();
                    }
                    documentationPart.AppendDocumentation( xE, e.DocumentationExtension );
                }
                WriteCtorParameters( tsType, ctorParametersPart, fields );

                return true;
            }

            internal string? GetDefaultValueSource( IActivityMonitor monitor, ITSFileCSharpType type )
            {
                Throw.DebugAssert( !_fieldsWriter.HasValue );
                _fieldsWriter = FieldsWriter.Create( monitor, _type, false, _generator._typeScriptContext );
                return _fieldsWriter.Value.HasDefault
                        ? $"new {type.TypeName}()"
                        : null;
            }
        }

        bool GeneratePrimaryPoco( IActivityMonitor monitor, ITSFileCSharpType tsType, IPrimaryPocoType t )
        {
            Throw.DebugAssert( !tsType.IsNullable && !t.IsNullable );

            IEnumerable<IAbstractPocoType> implementedInterfaces = t.MinimalAbstractTypes;
            if( t.FamilyInfo.IsClosedPoco )
            {
                implementedInterfaces = implementedInterfaces.Append( GetClosedPocoType() );
            }

            tsType.TypePart.CreatePart( out var documentationPart )
                .Append( "export class " ).Append( tsType.TypeName ).CreatePart( out var interfacesPart )
                .OpenBlock()
                .Append( "public constructor(" ).CreatePart( out var ctorParametersPart ).Append( ")" ).NewLine()
                .Append( "{" )
                .CreatePart( out var ctorBodyPart )
                .Append( "}" ).NewLine()
                .CreatePart( out var pocoTypeModelPart )
                .Append( "};" ).NewLine();

            var fieldsWriter = FieldsWriter.Create( monitor, t, false, _typeScriptContext );
            Throw.DebugAssert( fieldsWriter.HasDefault );
            // This sorts the fields and retrieves the TSField list that can be altered (skipping fields
            // and altering documentation) by other generators.
            var fields = fieldsWriter.WritePocoTypeModelAndGetPocoFields( pocoTypeModelPart, tsType );
            // Raises the event.
            var e = new GeneratingPrimaryPocoEventArgs( monitor,
                                                        _tsPocoModel,
                                                        tsType,
                                                        t,
                                                        implementedInterfaces,
                                                        fields,
                                                        pocoTypeModelPart,
                                                        interfacesPart,
                                                        ctorParametersPart,
                                                        ctorBodyPart );

            RaiseGeneratingPrimaryPoco( e );

            documentationPart.AppendDocumentation( monitor, e.ClassDocumentation, e.DocumentationExtension );
            WriteInterfacesAndBrand( monitor,
                                     interfacesPart,
                                     isPrimaryPoco: true,
                                     implementedInterfaces,
                                     isIPoco: false,
                                     t,
                                     tsType );
            WriteCtorParameters( tsType, ctorParametersPart, fields );
            return true;
        }

        static void WriteCtorParameters( ITSFileCSharpType tsType, ITSCodePart ctorParametersPart, TSPocoField[] fields )
        {
            bool atLeastOne = false;
            for( int i = 0; i < fields.Length; i++ )
            {
                var f = fields[i];
                if( i == 0 ) ctorParametersPart.NewLine();
                else if( atLeastOne ) ctorParametersPart.Append( ", " ).NewLine();
                if( !f.ConstructorSkip )
                {
                    f.WriteFieldDefinition( tsType.File, ctorParametersPart );
                    atLeastOne = true;
                }
            }
        }

        void WriteInterfacesAndBrand( IActivityMonitor monitor,
                                      ITSCodePart interfaces,
                                      bool isPrimaryPoco,
                                      IEnumerable<IAbstractPocoType> abstracts,
                                      bool isIPoco,
                                      IPocoType pocoType,
                                      ITSFileCSharpType tsType )
        {
            bool atLeastOne = false;
            tsType.TypePart.Append( "readonly _brand" ).Append( isPrimaryPoco ? "!: " : ": " ); 
            foreach( var a in abstracts )
            {
                // Handles base interfaces.
                if( atLeastOne ) interfaces.Append( ", " );
                else
                {
                    interfaces.Append( isPrimaryPoco ? " implements " : " extends " );
                }
                ITSType i = _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, a );
                interfaces.AppendTypeName( i );

                // Handles branding.
                if( atLeastOne ) tsType.TypePart.Append( " & " );
                tsType.TypePart.Append( i.TypeName ).Append( "[\"_brand\"]" );

                atLeastOne = true;
            }
            if( !atLeastOne && !isIPoco )
            {
                interfaces.Append( isPrimaryPoco ? " implements " : " extends " )
                          .AppendTypeName( _tsPocoModel.IPocoType );
                tsType.TypePart.Append( _tsPocoModel.IPocoType.TypeName ).Append( "[\"_brand\"]" );
            }
            tsType.TypePart.Append( " & {\"" ).Append( (pocoType.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSFileCSharpType tsType, IAbstractPocoType a )
        {
            var part = tsType.TypePart;
            part.CreatePart( out var docPart )
                .Append( "export interface " ).Append( tsType.TypeName ).CreatePart( out var interfacesPart )
                .OpenBlock();
            foreach( var f in a.Fields )
            {
                if( !IsExchangeable( f.Type ) ) continue;
                if( _typeScriptContext.Root.DocBuilder.GenerateDocumentation )
                {
                    part.AppendDocumentation( monitor, f.Originator );
                }
                if( f.IsReadOnly ) part.Append( "readonly " );
                part.AppendIdentifier( f.Name );
                if( f.Type.IsNullable ) part.Append( "?" );
                part.Append( ": " ).AppendTypeName( _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, f.Type.NonNullable ) ).Append(";").NewLine();
            }

            var e = new GeneratingAbstractPocoEventArgs( monitor, _tsPocoModel, tsType, a, a.MinimalGeneralizations, interfacesPart, part );
            RaiseGeneratingAbstractPoco( e );

            docPart.AppendDocumentation( monitor, e.TypeDocumentation, e.DocumentationExtension );
            WriteInterfacesAndBrand( monitor,
                                     interfacesPart,
                                     isPrimaryPoco: false,
                                     e.ImplementedInterfaces,
                                     isIPoco: tsType.Type == typeof( IPoco ),
                                     a,
                                     tsType );

            return true;
        }

        bool CheckExchangeable( IActivityMonitor monitor, IPocoType t )
        {
            if( !IsExchangeable( t ) )
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
