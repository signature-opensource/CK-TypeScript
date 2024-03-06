using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of any type that appears in the <see cref="IPocoTypeSystem"/>.
    /// This is exposed on the <see cref="TypeScriptContext.PocoCodeGenerator"/>.
    /// <para>
    /// This is not a <see cref="ITSCodeGenerator"/>: <see cref="OnResolveObjectKey(IActivityMonitor, RequireTSFromObjectEventArgs)"/>
    /// and <see cref="OnResolveType(IActivityMonitor, RequireTSFromTypeEventArgs)"/> are called by the TypeScriptContext.
    /// </para>
    /// </summary>
    sealed partial class PocoCodeGenerator : ITSPocoCodeGenerator
    {
        readonly TypeScriptContext _typeScriptContext;
        readonly CTSTypeSystem? _ctsTypeSystem;
        readonly IPocoTypeSet _typeScriptSet;

        internal PocoCodeGenerator( TypeScriptContext typeScriptContext,
                                    IPocoTypeSet typeScriptSet,
                                    IPocoTypeNameMap? jsonExchangeableNames )
        {
            _typeScriptContext = typeScriptContext;
            _typeScriptSet = typeScriptSet;
            if( jsonExchangeableNames != null )
            {
                _ctsTypeSystem = new CTSTypeSystem( typeScriptContext, jsonExchangeableNames );
            }
        }

        public event EventHandler<GeneratingPrimaryPocoEventArgs>? PrimaryPocoGenerating;

        void RaiseGeneratingPrimaryPoco( GeneratingPrimaryPocoEventArgs e )
        {
            _ctsTypeSystem?.OnGeneratingPrimaryPoco( e );
            PrimaryPocoGenerating?.Invoke( this, e );
        }

        public event EventHandler<GeneratingAbstractPocoEventArgs>? AbstractPocoGenerating;

        void RaiseGeneratingAbstractPoco( GeneratingAbstractPocoEventArgs e )
        {
            AbstractPocoGenerating?.Invoke( this, e );
        }

        public event EventHandler<GeneratingNamedRecordPocoEventArgs>? NamedRecordPocoGenerating;

        void RaiseGeneratingNamedRecordPoco( GeneratingNamedRecordPocoEventArgs e )
        {
            _ctsTypeSystem?.OnGeneratingNamedRecord( e );
            NamedRecordPocoGenerating?.Invoke( this, e );
        }

        public IPocoTypeSet TypeScriptSet => _typeScriptSet;

        internal bool OnResolveObjectKey( IActivityMonitor monitor, RequireTSFromObjectEventArgs e )
        {
            if( e.KeyType is IPocoType t && _typeScriptSet.Contains( t ) )
            {
                var tsTypeManager = _typeScriptContext.Root.TSTypes;

                if( !t.IsOblivious && t.IsNullable )
                {
                    e.SetResolvedType( tsTypeManager.ResolveTSType( monitor, t.NonNullable ).Nullable );
                    return true;
                }

                // Secondary is mapped to its Primary. It is erased in TS.
                // Secondary interfaces are not visible outside: we simply map them to the primary type.
                if( t.Kind == PocoTypeKind.SecondaryPoco )
                {
                    e.SetResolvedType( tsTypeManager.ResolveTSType( monitor, ((ISecondaryPocoType)t).PrimaryPocoType ) );
                    return true;
                }

                // We'll do the CTS mapping only for Oblivous type: Json serialization works on the Oblivious types.
                bool callCTSMapping = _ctsTypeSystem != null && t.IsOblivious;

                // Important: the type nullability is projected here.
                bool isNullable = t.IsNullable;
                if( isNullable ) t = t.NonNullable;

                ITSType ts;
                // The following types are handled by their C# Type. This "ping-pong" allows:
                // - The basic types to be associated to preregistered/mapped types like object => {}, decimal => bigint, DateTime => luxon datetime, etc.
                // - Enum types to be generated by the CK.TypeScript.CodeGen lib (that currently handles the [ExternalName] attribute so is Poco compatible).
                // - The complex types (AbstractPoco, PrimaryPoco, Record) to use the "builder" capabilities of the "by type" event instead
                //   of being restricted to set the final ResolvedType that this "by object" event supports: they need to defer their
                //   implementation generation.
                if( t.Kind is PocoTypeKind.Basic
                              or PocoTypeKind.Any
                              or PocoTypeKind.Enum
                              or PocoTypeKind.PrimaryPoco
                              or PocoTypeKind.AbstractPoco
                              or PocoTypeKind.Record )
                {
                    ts = tsTypeManager.ResolveTSType( monitor, t.Type );
                }
                else
                {
                    Throw.DebugAssert( "Handles array, list, set, dictionary, union and anonymous record here.",
                                        t.Kind is PocoTypeKind.Array
                                                  or PocoTypeKind.List
                                                  or PocoTypeKind.HashSet
                                                  or PocoTypeKind.Dictionary
                                                  or PocoTypeKind.UnionType
                                                  or PocoTypeKind.AnonymousRecord );
                    if( t is ICollectionPocoType tColl )
                    {
                        switch( tColl.Kind )
                        {
                            case PocoTypeKind.Array:
                            case PocoTypeKind.List:
                                {
                                    var inner = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                    // Uses Array<> rather than "(inner.TypeName)[]".
                                    var tName = tColl.IsAbstractReadOnly ? $"ReadonlyArray<{inner.TypeName}>" : $"Array<{inner.TypeName}>";
                                    ts = tsTypeManager.FindByTypeName( tName )
                                         ?? new TSBasicType( tsTypeManager, tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), "[]" );
                                    break;
                                }
                            case PocoTypeKind.HashSet:
                                {
                                    var inner = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                    var tName = tColl.IsAbstractReadOnly ? $"ReadonlySet<{inner.TypeName}>" : $"Set<{inner.TypeName}>";
                                    ts = tsTypeManager.FindByTypeName( tName )
                                         ?? new TSBasicType( tsTypeManager, tName, i => inner.EnsureRequiredImports( i.EnsureImport( inner ) ), $"new {tName}()" );
                                    break;
                                }
                            case PocoTypeKind.Dictionary:
                                {
                                    var tKey = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[0] );
                                    var tValue = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[1] );
                                    var tName = tColl.IsAbstractReadOnly ? $"ReadonlyMap<{tKey.TypeName},{tValue.TypeName}>" : $"Map<{tKey.TypeName},{tValue.TypeName}>";
                                    ts = tsTypeManager.FindByTypeName( tName )
                                         ?? new TSBasicType( tsTypeManager, tName,
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
                        var types = tU.AllowedTypes.Where( _typeScriptSet.Contains )
                                                   .Select( t => (t, ts: tsTypeManager.ResolveTSType( monitor, t )) ).ToArray();
                        var b = new StringBuilder();
                        foreach( var (_,u) in types )
                        {
                            if( b.Length > 0 ) b.Append( '|' );
                            b.Append( u.NonNullable.TypeName );
                        }
                        var tName = b.ToString();
                        ts = tsTypeManager.FindByTypeName( tName )
                             ?? new TSUnionType( tsTypeManager,
                                                 tName,
                                                 i =>
                                                 {
                                                     foreach( var t in types )
                                                     {
                                                         i.EnsureImport( t.ts );
                                                     }
                                                 },
                                                 types );
                    }
                    else
                    {
                        Throw.DebugAssert( t is IRecordPocoType a && a.IsAnonymous );
                        var r = (IRecordPocoType)t;
                        var fieldsWriter = FieldsWriter.Create( monitor, r, true, _typeScriptContext, _typeScriptSet );
                        ts = fieldsWriter.CreateAnonymousRecordType( monitor, out var fields );
                        if( callCTSMapping )
                        {
                            Throw.DebugAssert( _ctsTypeSystem != null );
                            _ctsTypeSystem.EnsureMappingForAnonymousRecord( r, ts, fields );
                            callCTSMapping = false;
                        }
                    }
                }
                e.SetResolvedType( isNullable ? ts.Nullable : ts );
                // This is where the serialization layer kicks in: all IPocoType will have a CTSType entry.
                //
                // For "inline" TSTypes that reference other types (collections and union types), the EnsureMapping
                // also creates the CTSType model of the type ("genParams" and "allowedTypes").
                // For the composites (record and primary poco), the model of "fields" are created when generating their
                // implementations.
                //
                // OnAfterCodeGeneration: all Oblivious types that are mapped to true javascript object with a constructor should
                // have the ITSKeyedCodePart.ConstructorBodyPart so that the "_ctsType" key can be defined (it is hidden in typescript)
                // that points to its CTSType entry.
                //
                if( callCTSMapping )
                {
                    Throw.DebugAssert( _ctsTypeSystem != null );
                    _ctsTypeSystem?.EnsureMapping( t, ts );
                }
            }
            return true;
        }

        internal bool OnResolveType( IActivityMonitor monitor, RequireTSFromTypeEventArgs builder )
        {
            // Gets the IPocoType if this is a Poco compliant type and it must be exchangeable.
            // If a type is not a Poco or is not an exchangeable one, it is not an error: other code generators
            // can handle it.
            // Important note: If the type is a Poco compliant type, then this is actually called by OnResolveObjectKey with the IPocoType
            //                 and it is OnResolveObjectKey that will call the CTSTypeSystem.EnsureMapping (if serialization is available).
            IPocoType? t = _typeScriptSet.TypeSystem.FindByType( builder.Type );
            if( t != null && _typeScriptSet.Contains( t ) )
            {
                // Only PrimaryPoco, AbstractPoco, named record (including the CK.Globalization.SimpleUserMessage),
                // and the IBasicRefType ExtendedCultureInfo and NormalizedCultureInfoCode are handled here.
                // Other Poco compliant types are handled by their associated IPocoType (by OnResolveObjectKey).
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
                    // There is no default value for abstraction.
                    // We must only build a name with its required imports.
                    var tAbstract = (IAbstractPocoType)t;
                    // AbstractPoco are TypeScript interfaces in their own file... except for generics:
                    // ICommand<string> or ICommand<List<IMission>> are TSType (without implementation file). 
                    if( tAbstract.IsGenericType )
                    {
                        var b = _typeScriptContext.Root.GetTSTypeSignatureBuilder();
                        var genDef = _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, tAbstract.GenericTypeDefinition.Type );
                        b.TypeName.AppendTypeName( genDef ).Append( "<" );
                        foreach( var arg in tAbstract.GenericArguments )
                        {
                            b.TypeName.AppendTypeName( _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, arg.Type ) );
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
                else if( t.Kind == PocoTypeKind.Basic )
                {
                    if( builder.Type == typeof( ExtendedCultureInfo ) )
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
                    else if( builder.Type == typeof( SimpleUserMessage ) )
                    {
                        builder.TryWriteValueImplementation = SimpleUserMessageWrite;
                        builder.Implementor = SimpleUserMessageCode;
                    }

                    static bool ExtendedCultureInfoWrite( ITSCodeWriter w, ITSFileCSharpType t, object o )
                    {
                        if( o is ExtendedCultureInfo c )
                        {
                            w.Append( "new ExtendedCultureInfo( " ).AppendSourceString( c.Name ).Append( " )" );
                            return true;
                        }
                        return false;
                    }

                    static bool ExtendedCultureInfoCode( IActivityMonitor monitor, ITSFileCSharpType t )
                    {
                        t.TypePart.Append( """
                                    /**
                                    * Mere encapsulation of the culture name..
                                    **/
                                    export class ExtendedCultureInfo {

                                    constructor(public readonly name: string)
                                    {
                                """ )
                            .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart )
                            .Append( """
                                    }
                                        toString() { return this.name; }
                                        toJSON() { return this.name; }
                                    """ );
                        return true;
                    }

                    static bool NormalizedCultureInfoWrite( ITSCodeWriter w, ITSFileCSharpType t, object o )
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

                    static bool NormalizedCultureInfoCode( IActivityMonitor monitor, ITSFileCSharpType t )
                    {
                        t.File.Imports.EnsureImport( monitor, typeof( ExtendedCultureInfo ) );
                        t.TypePart.Append( """
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
                                """ )
                            .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart )
                            .Append( """
                                }
                                """ );
                        return true;
                    }

                    static bool SimpleUserMessageWrite( ITSCodeWriter w, ITSFileCSharpType t, object o )
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

                    static bool SimpleUserMessageCode( IActivityMonitor monitor, ITSFileCSharpType t )
                    {
                        t.File.Imports.EnsureImport( monitor, typeof( UserMessageLevel ) );
                        t.TypePart.Append( """
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
                                    )
                                    {

                                """ )
                            .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart )
                            .Append( """
                                    }

                                    toString() { return '['+UserMessageLevel[this.level]+'] ' + this.message; }
                                    toJSON() { return this.level !== UserMessageLevel.None
                                                        ? [this.level,this.message,this.depth]
                                                        : [0]; }
                                """ );
                            return true;
                    }
                }
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
                _fieldsWriter ??= FieldsWriter.Create( monitor, _type, false, _generator._typeScriptContext, _generator._typeScriptSet );
                //// INamedRecord is a pure TS type defined in IPoco.ts.
                ////tsType.File.Imports.EnsureImport( _generator.PocoModel.IPocoType.File, "INamedRecord" );
                tsType.TypePart.InsertPart( out var documentationPart )
                    .Append( "export class " ).Append( tsType.TypeName )
                    .OpenBlock()
                    .Append( "public constructor(" ).InsertPart( out var ctorParametersPart ).Append( ")" ).NewLine()
                    .Append( "{" ).NewLine()
                    .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart, out var ctorBodyPart )
                    .Append( "}" ).NewLine()
                    .Append( "readonly _brand!: {\"" ).Append( (_type.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();

                // This sorts the fields and retrieves the TSNamedCompositeField list that can be altered (skipping fields
                // and altering documentation) by other generators.
                var fields = _fieldsWriter.Value.SortAndGetNamedCompositeFields();

                // Raises the event.
                var e = new GeneratingNamedRecordPocoEventArgs( monitor,
                                                                _generator._typeScriptContext,
                                                                tsType,
                                                                _type,
                                                                fields,
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
                _fieldsWriter = FieldsWriter.Create( monitor, _type, false, _generator._typeScriptContext, _generator._typeScriptSet );
                return _fieldsWriter.Value.HasDefault
                        ? $"new {type.TypeName}()"
                        : null;
            }
        }

        bool GeneratePrimaryPoco( IActivityMonitor monitor, ITSFileCSharpType tsType, IPrimaryPocoType t )
        {
            Throw.DebugAssert( !tsType.IsNullable && !t.IsNullable );

            tsType.TypePart.InsertPart( out var documentationPart )
                .Append( "export class " ).Append( tsType.TypeName ).InsertPart( out var interfacesPart )
                .OpenBlock()
                .Append( "public constructor(" ).InsertPart( out var ctorParametersPart ).Append( ")" ).NewLine()
                .Append( "{" ).NewLine()
                .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart, out var ctorBodyPart )
                .Append( "}" ).NewLine();

            var fieldsWriter = FieldsWriter.Create( monitor, t, false, _typeScriptContext, _typeScriptSet );
            Throw.DebugAssert( fieldsWriter.HasDefault );
            // This sorts the fields and retrieves the TSField list that can be altered (skipping fields
            // and altering documentation) by other generators.
            var fields = fieldsWriter.SortAndGetNamedCompositeFields();
            // Raises the event.
            var e = new GeneratingPrimaryPocoEventArgs( monitor,
                                                        _typeScriptContext,
                                                        tsType,
                                                        t,
                                                        t.MinimalAbstractTypes,
                                                        fields,
                                                        interfacesPart,
                                                        ctorParametersPart,
                                                        ctorBodyPart );

            RaiseGeneratingPrimaryPoco( e );

            documentationPart.AppendDocumentation( monitor, e.ClassDocumentation, e.DocumentationExtension );
            WriteInterfacesAndBrand( monitor,
                                     interfacesPart,
                                     isPrimaryPoco: true,
                                     t.MinimalAbstractTypes,
                                     isIPoco: false,
                                     t,
                                     tsType );
            WriteCtorParameters( tsType, ctorParametersPart, fields );
            return true;
        }

        static void WriteCtorParameters( ITSFileCSharpType tsType, ITSCodePart ctorParametersPart, ImmutableArray<TSNamedCompositeField> fields )
        {
            bool atLeastOne = false;
            for( int i = 0; i < fields.Length; i++ )
            {
                var f = fields[i];
                if( i == 0 ) ctorParametersPart.NewLine();
                else if( atLeastOne ) ctorParametersPart.Append( ", " ).NewLine();
                if( !f.ConstructorSkip )
                {
                    f.WriteCtorFieldDefinition( tsType.File, ctorParametersPart );
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
                var iPoco = _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, typeof(IPoco) );
                interfaces.Append( isPrimaryPoco ? " implements " : " extends " )
                          .AppendTypeName( iPoco );
                tsType.TypePart.Append( iPoco.TypeName ).Append( "[\"_brand\"]" );
            }
            tsType.TypePart.Append( " & {\"" ).Append( (pocoType.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSFileCSharpType tsType, IAbstractPocoType a )
        {
            var part = tsType.TypePart;
            part.InsertPart( out var docPart )
                .Append( "export interface " ).Append( tsType.TypeName ).InsertPart( out var interfacesPart )
                .OpenBlock();
            foreach( var f in a.Fields )
            {
                if( !_typeScriptSet.Contains( f.Type ) ) continue;
                if( _typeScriptContext.Root.DocBuilder.GenerateDocumentation )
                {
                    part.AppendDocumentation( monitor, f.Originator );
                }
                if( f.IsReadOnly ) part.Append( "readonly " );
                part.AppendIdentifier( f.Name );
                if( f.Type.IsNullable ) part.Append( "?" );
                part.Append( ": " ).AppendTypeName( _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, f.Type.NonNullable ) ).Append(";").NewLine();
            }

            var e = new GeneratingAbstractPocoEventArgs( monitor, _typeScriptContext, tsType, a, a.MinimalGeneralizations, interfacesPart, part );
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
    }
}
