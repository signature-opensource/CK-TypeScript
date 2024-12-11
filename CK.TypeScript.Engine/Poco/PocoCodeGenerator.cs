using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CK.TypeScript.Engine;

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

    public CTSTypeSystem? CTSTypeSystem => _ctsTypeSystem;

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

    /// <summary>
    /// Secondary is mapped to its Primary: secondary interfaces are not visible in TS, we map them to the primary type
    /// and use the NonSecondaryConcreteCollection that also ignores abstract collection.
    /// <para>
    /// Nullability is preserved.
    /// </para>
    /// </summary>
    /// <param name="t">The poco type.</param>
    /// <returns>The mapped type.</returns>
    internal static IPocoType MapType( IPocoType t )
    {
        if( t is ISecondaryPocoType sec ) t = sec.PrimaryPocoType;
        else if( t is ICollectionPocoType c )
        {
            Throw.DebugAssert( !c.IsAbstractReadOnly );
            t = c.NonSecondaryConcreteCollection;
        }
        return t;
    }

    internal bool OnResolveObjectKey( IActivityMonitor monitor, RequireTSFromObjectEventArgs e )
    {
        if( e.KeyType is IPocoType t )
        {
            var tsTypeManager = _typeScriptContext.Root.TSTypes;

            // Regardless of the TypeScript set of types, blindly maps the nullable to their non nullable.
            // We only handle non nullable types afterwards.
            if( t.IsNullable )
            {
                var mapped = tsTypeManager.ResolveTSType( monitor, t.NonNullable );
                Throw.DebugAssert( !mapped.IsNullable );
                e.SetResolvedType( mapped.Nullable );
                return true;
            }

            if( !_typeScriptSet.Contains( t ) )
            {
                // For abstractions (coming from GenerateAbstractPoco) fields types we need to allow them
                // as they have implementations.
                if( t.Kind == PocoTypeKind.Any )
                {
                    e.SetResolvedType( tsTypeManager.ResolveTSType( monitor, t.Type ) );
                }
                else if( t is ICollectionPocoType c && c.IsAbstractReadOnly )
                {
                    e.SetResolvedType( MapCollectionType( monitor, tsTypeManager, c ) );
                }
                // No more handling at our level.
                return true;
            }

            // Erase the Secondary poco type.
            var tMapped = MapType( t );
            if( tMapped != t )
            {
                Throw.DebugAssert( !tMapped.IsNullable );
                var mapped = tsTypeManager.ResolveTSType( monitor, tMapped );
                Throw.DebugAssert( !mapped.IsNullable );
                e.SetResolvedType( mapped );
                return true;
            }

            // Anonymous records call EnsureCTSEntry directly.
            bool callCTSMapping = _ctsTypeSystem != null;

            ITSType ts;
            // The following types are handled by their C# Type. This "ping-pong" allows:
            // - The basic types to be associated to preregistered/mapped types like object => {}, decimal => bigint, DateTime => luxon datetime, etc.
            // - Enum types to be generated by the CK.TypeScript.CodeGen lib (that currently handles the [ExternalName] attribute so is Poco compatible).
            // - The complex types (AbstractPoco, PrimaryPoco, Record) to use the "builder" capabilities of the "by type" event instead
            //   of being restricted to set the final ResolvedType that this "by object" event supports: they need to defer their
            //   implementation generation.
            // - This also implements the mapping UserMessage to SimpleUserMessage.
            if( t.Kind is PocoTypeKind.Basic
                          or PocoTypeKind.Any
                          or PocoTypeKind.Enum
                          or PocoTypeKind.PrimaryPoco
                          or PocoTypeKind.AbstractPoco
                          or PocoTypeKind.Record )
            {
                var tMap = t.Type;
                if( tMap == typeof( UserMessage ) ) tMap = typeof( SimpleUserMessage );
                ts = tsTypeManager.ResolveTSType( monitor, tMap );
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
                    ts = MapCollectionType( monitor, tsTypeManager, tColl );
                }
                else if( t is IUnionPocoType tU )
                {
                    var types = tU.AllowedTypes.Where( _typeScriptSet.Contains )
                                               .Select( t => (t, ts: tsTypeManager.ResolveTSType( monitor, t )) ).ToArray();
                    var b = new StringBuilder();
                    foreach( var (_, u) in types )
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
                                                     i.Import( t.ts );
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
                        _ctsTypeSystem.EnsureMappingForAnonymousRecord( r, ts, fields, fieldsWriter.UseTupleSyntax );
                        callCTSMapping = false;
                    }
                }
            }
            e.SetResolvedType( ts );
            // This is where the serialization layer kicks in: all IPocoType will have a CTSType entry.
            //
            // The EnsureMapping creates the CTSType model of the type with its json( o: any ) and
            // its nosj( o: T|undefined ) functions.
            //
            // OnAfterCodeGeneration: all types that are mapped to true javascript object with a constructor should
            // have the ITSKeyedCodePart.ConstructorBodyPart so that the "_ctsType" key can be defined (_ctsType is a
            // javascript property, not exposed in typescript) that points to its CTSType entry.
            //
            if( callCTSMapping )
            {
                Throw.DebugAssert( _ctsTypeSystem != null );
                _ctsTypeSystem.EnsureCTSEntry( t, ts );
            }
        }
        return true;
    }

    static ITSType MapCollectionType( IActivityMonitor monitor, TSTypeManager tsTypeManager, ICollectionPocoType tColl )
    {
        ITSType ts;
        switch( tColl.Kind )
        {
            case PocoTypeKind.Array:
            case PocoTypeKind.List:
            {
                var inner = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[0] );
                // Uses Array<> rather than "(inner.TypeName)[]".
                var tName = tColl.IsAbstractReadOnly ? $"ReadonlyArray<{inner.TypeName}>" : $"Array<{inner.TypeName}>";
                ts = tsTypeManager.FindByTypeName( tName )
                        ?? new TSBasicType( tsTypeManager, tName, i => inner.EnsureRequiredImports( i.Import( inner ) ), "[]" );
                break;
            }
            case PocoTypeKind.HashSet:
            {
                var inner = tsTypeManager.ResolveTSType( monitor, tColl.ItemTypes[0] );
                var tName = tColl.IsAbstractReadOnly ? $"ReadonlySet<{inner.TypeName}>" : $"Set<{inner.TypeName}>";
                ts = tsTypeManager.FindByTypeName( tName )
                        ?? new TSBasicType( tsTypeManager, tName, i => inner.EnsureRequiredImports( i.Import( inner ) ), $"new {tName}()" );
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
                                                tKey.EnsureRequiredImports( i.Import( tKey ) );
                                                tValue.EnsureRequiredImports( i.Import( tValue ) );
                                            },
                                            $"new {tName}()" );
                break;
            }
            default: throw new NotSupportedException( tColl.ToString() );
        }

        return ts;
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
            // We work on the non nullable type.
            t = t.NonNullable;

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
                if( builder.Type == typeof( UserMessage ) )
                {
                    // This should not happen: PocoType are first resolved by OnResolveObjectKey that does
                    // the mapping UserMessage => SimpleUserMessage.
                    builder.ResolvedType = _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, typeof( SimpleUserMessage ) );
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
                else if( builder.Type == typeof( SimpleUserMessage ) )
                {
                    builder.DefaultValueSource = "SimpleUserMessage.invalid";
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
                    if( o is UserMessage uM )
                    {
                        w.Append( "new SimpleUserMessage( " )
                         .Append( uM.Level ).Append( ", " )
                         .AppendSourceString( uM.Text ).Append( ", " )
                         .Append( uM.Depth );
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

                            static #invalid: SimpleUserMessage;

                            /**
                            * Gets the default, invalid, message.
                            **/
                            static get invalid() : SimpleUserMessage { return SimpleUserMessage.#invalid ??= new SimpleUserMessage(UserMessageLevel.None, "", 0); }

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
                                static parse( o: {} ) : SimpleUserMessage
                                {
                                    if( o instanceof Array )
                                    {
                                        if( o.length === 1 )
                                        {
                                            if( o[0] === 0 ) return SimpleUserMessage.invalid;
                                        }
                                        else if( o.length === 3 || o.length === 8 )
                                        {
                                            const level = o[0];
                                            if( level === UserMessageLevel.Info || level === UserMessageLevel.Warn || level === UserMessageLevel.Error )
                                            {
                                                let msg = o[1];
                                                let d = o[2];
                                                if( typeof msg === "number" )
                                                {
                                                    // This is a UserMessage (o.length === 8). 
                                                    msg = d;
                                                    d = o[1];
                                                }
                                                if( typeof d === "number" && d >= 0 && d <= 255 )
                                                {
                                                    return new SimpleUserMessage( level, msg, d );
                                                }
                                            }
                                        }
                                    }
                                    throw new Error( `Unable to parse '{{o}}' as a SimpleUserMessage.` );
                                }
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
            tsType.TypePart.InsertPart( out var documentationPart )
                .Append( "export class " ).Append( tsType.TypeName )
                .OpenBlock()
                .Append( "public constructor(" ).InsertPart( out var ctorParametersPart ).Append( ")" ).NewLine()
                .Append( "{" ).NewLine()
                .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart, out var ctorBodyPart )
                .Append( "}" ).NewLine()
                .Append( "readonly _brand!: {\"" ).Append( (_type.Index >> 1).ToString( CultureInfo.InvariantCulture ) ).Append( "\":any};" ).NewLine();

            // This sorts the fields and retrieves the TSNamedCompositeField list that allows other generators to alter the documentation.
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
            WriteCtorParameters( ctorParametersPart, fields );
            return true;

            static void WriteCtorParameters( ITSCodePart ctorParametersPart, ImmutableArray<TSNamedCompositeField> fields )
            {
                for( int i = 0; i < fields.Length; i++ )
                {
                    var f = fields[i];
                    if( i == 0 ) ctorParametersPart.NewLine();
                    else ctorParametersPart.Append( ", " ).NewLine();
                    f.WriteCtorFieldDefinition( ctorParametersPart );
                }
            }

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
            .InsertPart( out var fieldDefinitionPart )
            .Append( "public constructor()" ).NewLine()
            .Append( "public constructor(" ).InsertPart( out var ctorParametersPart ).Append( ")" ).NewLine()
            .Append( "constructor(" ).InsertPart( out var ctorImplementationParametersPart ).Append( ")" ).NewLine()
            .Append( "{" ).NewLine()
            .InsertKeyedPart( ITSKeyedCodePart.ConstructorBodyPart, out var ctorBodyPart )
            .Append( "}" ).NewLine();

        var fieldsWriter = FieldsWriter.Create( monitor, t, false, _typeScriptContext, _typeScriptSet );
        Throw.DebugAssert( fieldsWriter.HasDefault );
        // Retrieves the TSNamedCompositeField list in their declaration order that allows other generators to alter the documentation.
        var fields = fieldsWriter.GetNamedCompositeFields();
        // Raises the event.
        var e = new GeneratingPrimaryPocoEventArgs( monitor,
                                                    _typeScriptContext,
                                                    tsType,
                                                    t,
                                                    t.MinimalAbstractTypes,
                                                    fields,
                                                    interfacesPart,
                                                    fieldDefinitionPart,
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

        for( int i = 0; i < fields.Length; i++ )
        {
            TSNamedCompositeField? f = fields[i];

            f.WriteFieldDefinition( fieldDefinitionPart );
            fieldDefinitionPart.Append( ";" ).NewLine();

            if( i != 0 ) ctorImplementationParametersPart.Append( "," );
            ctorImplementationParametersPart.NewLine()
                .Append( f.TSField.FieldName ).Append( "?: " ).AppendTypeName( f.TSField.TSFieldType.NonNullable );

            ctorBodyPart.Append( "this." ).Append( f.TSField.FieldName ).Append( " = " ).Append( f.TSField.FieldName );
            if( f.TSField.HasNonNullDefault )
            {
                ctorBodyPart.Append( " ?? " );
                f.TSField.WriteDefaultValue( ctorBodyPart );
            }
            ctorBodyPart.Append( ";" ).NewLine();
        }
        ctorParametersPart.Append( ctorImplementationParametersPart.ToString() );
        return true;
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
            var iPoco = _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, typeof( IPoco ) );
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
            var hasImpl = a.PrimaryPocoTypes.Where( _typeScriptSet.Contains )
                                            .Any( impl => impl.Fields.Any( implF => implF.Name == f.Name && _typeScriptSet.Contains( implF.Type ) ) );
            if( !hasImpl ) continue;
            if( _typeScriptContext.Root.DocBuilder.GenerateDocumentation )
            {
                part.AppendDocumentation( monitor, f.Originator );
            }
            if( f.IsReadOnly ) part.Append( "readonly " );
            part.AppendIdentifier( f.Name );
            if( f.Type.IsNullable ) part.Append( "?" );
            part.Append( ": " ).AppendTypeName( _typeScriptContext.Root.TSTypes.ResolveTSType( monitor, f.Type.NonNullable ) ).Append( ";" ).NewLine();
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
