using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of <see cref="IPoco"/> by reproducing the IPoco interfaces
    /// in the TypeScript world.
    /// <para>
    /// This generator doesn't generate any IPoco by default: they must be "asked" to be generated
    /// by instantiating a <see cref="TSTypeFile"/> for the IPoco interface.
    /// </para>
    /// <para>
    /// The actual generator is configured by the <see cref="ConfigureTypeScriptAttribute"/> method that
    /// sets a <see cref="ITSTypeFileBuilder.Finalizer"/>.
    /// </para>
    /// <para>
    /// In other words, this code generator is "passive": it will only generate code for IPoco that
    /// have been declared (either by a TypeScriptAttribute or explicitly <see cref="TypeScriptContext.DeclareTSType(IActivityMonitor, Type, bool)"/>
    /// (typically because they appear in other types that must be generated).
    /// </para>
    /// <para>
    /// This "on demand" generation acts as a kind of "tree shaking": only the transitive closure of the referenced types
    /// of an explicitly generated type is generated.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/> as the first <see cref="TypeScriptContext.GlobalGenerators"/>,
    /// it is not initiated by an attribute like other code generators (typically thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    public partial class TSIPocoCodeGenerator : ITSCodeGenerator
    {
        internal TSIPocoCodeGenerator( IPocoSupportResult poco )
        {
            PocoSupport = poco;
        }

        /// <summary>
        /// Gets the descriptor of all the existing Pocos.
        /// </summary>
        public IPocoSupportResult PocoSupport { get; }

        /// <summary>
        /// Raised when a poco is about to be generated.
        /// This enables extensions to inject codes in the <see cref="PocoGeneratingEventArgs.TypeFile"/>.
        /// </summary>
        public event EventHandler<PocoGeneratingEventArgs>? PocoGenerating;

        /// <summary>
        /// If the type is a <see cref="IPoco"/>, the <see cref="IPocoRootInfo.PrimaryInterface"/> sets the type name
        /// and the folder and file for all other IPoco interfaces and the class.
        /// <para>
        /// Interfaces that are not IPoco (the ones in <see cref="IPocoRootInfo.OtherInterfaces"/>) are ignored by default.
        /// If another such interface is declared (<see cref="TypeScriptContext.FindDeclaredTSType"/> returned the type file),
        /// then it is imported and appears in the "extends" base interfaces.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="builder">
        /// The builder with the <see cref="ITSTypeFileBuilder.Type"/> that is handled, the <see cref="ITSTypeFileBuilder.Context"/>,
        /// its <see cref="ITSTypeFileBuilder.Generators"/> that includes this one and the current <see cref="ITSTypeFileBuilder.Finalizer"/>.
        /// </param>
        /// <param name="attr">
        /// The attribute to configure. It is empty or the attribute on the type (this generator
        /// is the first one in the <see cref="TypeScriptContext.GlobalGenerators"/>: it is the first to be solicited).
        /// </param>
        /// <returns>True on success, false on error (errors must be logged).</returns>
        /// <returns>Always true.</returns>
        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                  ITSTypeFileBuilder builder,
                                                  TypeScriptAttribute attr )
        {
            var type = builder.Type;
            bool isPoco = typeof( IPoco ).IsAssignableFrom( type );
            if( isPoco )
            {
                if( type.IsClass )
                {
                    var pocoClass = PocoSupport.Roots.FirstOrDefault( root => root.PocoClass == type );
                    if( pocoClass != null )
                    {
                        attr.TypeName = TypeScriptContext.GetPocoClassNameFromPrimaryInterface( pocoClass );
                        attr.SameFileAs = pocoClass.PrimaryInterface;
                        builder.Finalizer = ( m, f ) => EnsurePocoClass( m, f, pocoClass ) != null;
                    }
                    else
                    {
                        monitor.Warn( $"Type {type} is a class that implements at least one IPoco but is not a registered PocoClass. It is ignored." );
                    }
                }
                else
                {
                    if( PocoSupport.AllInterfaces.TryGetValue( type, out IPocoInterfaceInfo? itf ) )
                    {
                        if( itf.Root.PrimaryInterface != itf.PocoInterface )
                        {
                            if( attr.SameFileAs != null
                                || attr.SameFolderAs != null
                                || attr.Folder != null
                                || attr.FileName != null )
                            {
                                monitor.Warn( $"IPoco '{type.Name}' must not specify a SameFileAs, SameFolderAs, Folder or FileName since it will use the same file as the primary interface." );
                                attr.SameFolderAs = null;
                                attr.Folder = null;
                                attr.FileName = null;
                            }
                            attr.SameFileAs = itf.Root.PrimaryInterface;
                        }
                        else
                        {
                            if( attr.SameFileAs == null && attr.FileName == null )
                            {
                                string typeName = TypeScriptContext.GetPocoClassNameFromPrimaryInterface( itf.Root );
                                attr.FileName = TypeScriptContext.SafeFileChars( typeName ) + ".ts";
                            }
                        }
                        builder.Finalizer = ( m, f ) => EnsurePocoInterface( m, f, itf ) != null;
                    }
                    else
                    {
                        if( type != typeof( IPoco ) && type != typeof( IClosedPoco ) )
                        {
                            monitor.Warn( $"Interface '{type}' is a IPoco but cannot be found in the registered interfaces. It is ignored." );
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Does nothing (it is the <see cref="ConfigureTypeScriptAttribute"/> method that sets a <see cref="ITSTypeFileBuilder.Finalizer"/>). 
        /// </summary>
        /// <param name="monitor">Unused.</param>
        /// <param name="context">Unused.</param>
        /// <returns>Always true.</returns>
        public bool GenerateCode( IActivityMonitor monitor, TypeScriptContext context ) => true;

        internal TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TypeScriptContext g, IPocoRootInfo root )
        {
            var t = g.DeclareTSType( monitor, root.PocoClass, requiresFile: true );
            return t != null ? EnsurePocoClass( monitor, t, root ) : null;
        }

        TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TSTypeFile tsTypedFile, IPocoRootInfo root )
        {
            if( tsTypedFile.TypePart == null )
            {
                // Creates a part at the top of the file for the implementation class: this
                // handles any possible reentrancy here.
                var b = tsTypedFile.EnsureTypePart();

                // Ensures that the CK/Core/IPoco.ts is here.
                var iPocoFile = tsTypedFile.Context.DeclareTSType( monitor, typeof( IPoco ), requiresFile: true );
                if( iPocoFile == null ) return null;
                if( iPocoFile.TypePart == null )
                {
                    iPocoFile.EnsureTypePart( closer: String.Empty )
                        .Append( "export const SymbolPoco = Symbol();" ).NewLine()
                        .Append( "export interface IPoco" ).OpenBlock()
                        .Append( "[SymbolPoco]: unknown;" )
                        .CloseBlock();
                }

                // Generates the signature with all its interfaces.
                b.Append( "export class " ).Append( tsTypedFile.TypeName );
                List<TSTypeFile> interfaces = new();
                foreach( IPocoInterfaceInfo i in root.Interfaces )
                {
                    var itf = EnsurePocoInterface( monitor, tsTypedFile.Context, i );
                    if( itf == null ) return null;
                    b.Append( interfaces.Count == 0 ? " implements " : ", " );
                    interfaces.Add( itf );
                    b.AppendImportedTypeName( itf );
                }
                b.OpenBlock();

                // Creates the mutable lists of properties and create method parameters.
                var propList = new List<TypeScriptPocoPropertyInfo>( root.PropertyList.Count );
                int readOnlyPropertyCount = 0;
                int requiredParameterCount = 0;
                foreach( var p in root.PropertyList )
                {
                    var propName = tsTypedFile.Context.Root.ToIdentifier( p.PropertyName );
                    var propType = GetPropertyTypeScriptType( monitor, tsTypedFile, p );
                    if( propType == null ) return null;
                    var paramName = TypeScriptRoot.ToIdentifier( propName, false );

                    // Unifies the documentations from possibly more than one property declarations
                    // into a documentation text (without the stars).
                    var docElements = XmlDocumentationReader.GetDocumentationFor( monitor, p.DeclaredProperties );
                    var propComment = new DocumentationBuilder( withStars: false ).AppendDocumentation( b.File, docElements ).GetFinalText();
                    var paramComment = RemoveGetsOrSetsPrefix( propComment );

                    // When p.IsReadOnly, the DefaultValue is "new propType()". This works perfectly for (I)List (=> Array), (I)Set (=> Set) and (I)Dictionary (=> Map)
                    // and for Poco since the interface's name is mapped to its implementation class' name.
                    var prop = new TypeScriptPocoPropertyInfo( p, propType, propName, paramName, propComment, paramComment );
                    Debug.Assert( prop.CreateMethodParameter != null, "Never null on initialization." );
                    // If a DefaultValue attribute exists on the property, tries to get its TypeScript representation.
                    if( p.HasDefaultValue )
                    {
                        Debug.Assert( !p.IsReadOnly );
                        var temp = tsTypedFile.File.CreateDetachedPart();
                        if( temp.TryAppend( p.DefaultValue ) )
                        {
                            // This default value will be set in the Poco constructor: the create
                            // parameter becomes optional.
                            prop.Property.DefaultValue = temp.ToString();
                            prop.CreateMethodParameter.Optional = true;
                        }
                        else
                        {
                            monitor.Warn( $"Unable to generate TypeScript code for DefaultValue attribute on {p}." );
                        }
                    }

                    // The lists is ordered:
                    //  - The required parameters first.
                    //  - Then comes the optional parameters:
                    //    - first the simple nullable properties or the ones with a default value,
                    //    - and then the read only properties.
                    if( p.IsNullable || p.IsReadOnly || prop.Property.HasDefaultValue )
                    {
                        if( p.IsReadOnly )
                        {
                            // Inserts at the end.
                            propList.Add( prop );
                            readOnlyPropertyCount++;
                        }
                        else
                        {
                            // Inserts before the last readOnlyPropertyCount.
                            propList.Insert( propList.Count - readOnlyPropertyCount, prop );
                        }
                    }
                    else
                    {
                        // p is not nullable, not readonly and has no default value: it is required.
                        // Inserts at the required parameter index (and increments the index).
                        propList.Insert( requiredParameterCount++, prop );
                    }
                }
                // Raises the Generating event with the mutable TypeScriptPocoClass payload. 
                var pocoClass = new TypeScriptPocoClass( tsTypedFile.TypeName, b, root, propList, requiredParameterCount, readOnlyPropertyCount );
                var h = PocoGenerating;
                if( h != null )
                {
                    var ev = new PocoGeneratingEventArgs( monitor, tsTypedFile, pocoClass, this );
                    h.Invoke( this, ev );
                    if( ev.HasError )
                    {
                        monitor.Error( "Error occurred while raising PocoGenerating event." );
                        return null;
                    }
                }

                // Writes the properties.
                pocoClass.AppendProperties( b );

                // Defines the symbol marker and the constructor(s).
                tsTypedFile.File.Imports.EnsureImport( iPocoFile.File, "SymbolPoco" );
                b.NewLine()
                 .Append( "[SymbolPoco]: unknown;" ).NewLine();

                ITSCodePart ctorBody;
                if( readOnlyPropertyCount == 0 )
                {
                    b.Append( "constructor()" ).OpenBlock()
                     .CreatePart( out ctorBody )
                     .CloseBlock();
                }
                else
                {
                    // Using parts to:
                    //   - better see the structure.
                    //   - and generate the code in one pass.
                    //   - and reuse the createSignaturePart as the implSignaturePart.
                    //
                    // The first constructor overload is the one with the (all optional) parameters.
                    // Then comes the empty one.
                    b.CreatePart( out var docPart )
                     .Append( "constructor( " ).CreatePart( out var createSignaturePart ).Append( ")" ).NewLine()
                     .AppendDocumentation( $"Initializes a new empty {pocoClass.TypeName}." )
                     .Append( "constructor()" ).NewLine()
                     .Append( "// Implementation." ).NewLine()
                     .Append( "constructor( " ).CreatePart( out var implSignaturePart ).Append( ") " ).OpenBlock()
                        .CreatePart( out ctorBody )
                     .CloseBlock();

                    var docBuilder = new DocumentationBuilder();
                    docBuilder.Append( $"Initializes a new {pocoClass.TypeName} with initial values for the readonly properties.", endWithNewline: true )
                              .Append( "Use one of the create methods to set all properties.", endWithNewline: true );

                    bool paramsOnOneLine = readOnlyPropertyCount <= 2;
                    if( !paramsOnOneLine )
                    {
                        createSignaturePart.NewLine();
                    }
                    bool atLeastOne = false;
                    foreach( var prop in propList.Skip( propList.Count - readOnlyPropertyCount ) )
                    {
                        // Adds the @param to the doc if any.
                        if( !String.IsNullOrEmpty( prop.CtorParameterComment ) )
                        {
                            docBuilder.Append( "@param " ).Append( prop.CtorParameterName ).Append( " " );
                            docBuilder.AppendText( prop.CtorParameterComment, trimFirstLine: true, trimLastLines: true, startNewLine: false, endWithNewline: true );
                        }
                        if( atLeastOne )
                        {
                            ctorBody.NewLine();
                            createSignaturePart.Append( "," );
                            if( !paramsOnOneLine ) createSignaturePart.NewLine();
                        }
                        else atLeastOne = true;
                        createSignaturePart.Append( prop.CtorParameterName ).Append( "?: " ).Append( prop.Property.Type );

                        ctorBody.Append( "this." ).Append( prop.Property.Name ).Append( " = typeof " ).Append( prop.CtorParameterName ).Append( " === \"undefined\" ? " )
                                .Append( prop.Property.DefaultValue )
                                .Append( " : " ).Append( prop.CtorParameterName ).Append( ";" ).NewLine();
                    }
                    docPart.Append( docBuilder.GetFinalText() );
                    implSignaturePart.Append( createSignaturePart.ToString() );
                }
                ctorBody.Append( "this[SymbolPoco] = null;" ).NewLine();
                foreach( var prop in propList.Skip( requiredParameterCount )
                                  .Take( propList.Count - requiredParameterCount - readOnlyPropertyCount )
                                  .Where( prop => prop.Property.HasDefaultValue ) )
                {
                    ctorBody.Append( "this." ).Append( prop.Property.Name ).Append( " = " ).Append( prop.Property.DefaultValue ).Append( ";" );
                }
                pocoClass.AppendCreateMethod( monitor, b );
            }
            return tsTypedFile;
        }

        static readonly Regex _rGetSet = new Regex( @"^\s*Gets?(\s+(or\s+)?sets?)?\s+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase );

        static string RemoveGetsOrSetsPrefix( string text )
        {
            var m = _rGetSet.Match( text );
            if( m.Success )
            {
                text = text.Substring( m.Length );
                text = TypeScriptRoot.ToIdentifier( text, true );
            }
            return text;
        }

        TSTypeFile? EnsurePocoInterface( IActivityMonitor monitor, TypeScriptContext g, IPocoInterfaceInfo i )
        {
            var t = g.DeclareTSType( monitor, i.PocoInterface, requiresFile: true );
            return t != null ? EnsurePocoInterface( monitor, t, i ) : null;
        }

        TSTypeFile? EnsurePocoInterface( IActivityMonitor monitor, TSTypeFile tsTypedFile, IPocoInterfaceInfo i )
        {
            if( tsTypedFile.TypePart == null )
            {
                // First, we ensure the class so that it appears at the beginning of the file.
                if( EnsurePocoClass( monitor, tsTypedFile.Context, i.Root ) == null )
                {
                    return null;
                }
                // Double check here since EnsurePocoClass may have called us already.
                if( tsTypedFile.TypePart == null )
                {
                    var iPocoFile = tsTypedFile.Context.DeclareTSType( monitor, typeof( IPoco ), requiresFile: true );
                    Debug.Assert( iPocoFile != null, "EnsurePocoClass did the job." );
                    tsTypedFile.File.Imports.EnsureImport( iPocoFile.File, "IPoco" );

                    var b = tsTypedFile.EnsureTypePart();
                    b.AppendDocumentation( monitor, i.PocoInterface );
                    b.Append( "export interface " ).Append( tsTypedFile.TypeName ).Append( " extends IPoco" );
                    foreach( Type baseInterface in i.PocoInterface.GetInterfaces() )
                    {
                        // If the base interface is a "normal" IPoco interface, then we ensure that it is
                        // generated.
                        // If the base interface is "another interface", we'll consider it only if it has been
                        // declared.
                        var baseItf = i.Root.Interfaces.FirstOrDefault( p => p.PocoInterface == baseInterface );
                        if( baseItf != null )
                        {
                            var fInterface = EnsurePocoInterface( monitor, tsTypedFile.Context, baseItf );
                            if( fInterface == null ) return null;
                            b.Append( ", " ).AppendImportedTypeName( fInterface );
                        }
                        else
                        {
                            // If the base interface does not belong to the OtherInterfaces: we skip them (it should be
                            // the IPoco or IClosedPoco).
                            if( i.Root.OtherInterfaces.Contains( baseInterface ) )
                            {
                                // We handle the already declared base interfaces only.
                                var declared = tsTypedFile.Context.FindDeclaredTSType( baseInterface );
                                if( declared != null )
                                {
                                    b.Append( ", " ).AppendImportedTypeName( declared );
                                }
                            }
                        }
                    }
                    b.OpenBlock();
                    bool success = true;
                    foreach( var iP in i.PocoInterface.GetProperties() )
                    {
                        // Is this interface property implemented at the class level?
                        // If not (ExternallyImplemented property) we currently ignore it.
                        IPocoPropertyInfo? p = i.Root.Properties.GetValueOrDefault( iP.Name );
                        if( p != null )
                        {
                            b.AppendDocumentation( monitor, iP )
                             .Append( p.IsReadOnly ? "readonly " : "" )
                             .AppendIdentifier( p.PropertyName )
                             .Append( p.IsNullable ? "?: " : ": " );
                            success &= AppendPocoPropertyTypeQualifier( monitor, tsTypedFile.Context, b, p );
                            b.Append( ";" ).NewLine();
                        }
                    }
                }
            }
            return tsTypedFile;
        }

        static bool AppendPocoPropertyTypeQualifier( IActivityMonitor monitor, TypeScriptContext g, ITSCodePart b, IPocoPropertyInfo p )
        {
            bool success = true;
            bool hasUnions = false;
            foreach( var t in p.PropertyUnionTypes )
            {
                if( hasUnions ) b.Append( "|" );
                hasUnions = true;
                success &= b.AppendComplexTypeName( monitor, g, t );
            }
            if( !hasUnions )
            {
                success &= b.AppendComplexTypeName( monitor, g, p.PropertyNullableTypeTree, withUndefined: false );
            }
            return success;
        }

        static string? GetPropertyTypeScriptType( IActivityMonitor monitor, TSTypeFile file, IPocoPropertyInfo p )
        {
            var b = file.File.CreateDetachedPart();
            return AppendPocoPropertyTypeQualifier( monitor, file.Context, b, p )
                    ? b.ToString()
                    : null;
        }

    }
}
