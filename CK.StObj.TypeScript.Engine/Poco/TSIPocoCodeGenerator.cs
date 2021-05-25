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
        readonly IPocoSupportResult _poco;

        internal TSIPocoCodeGenerator( IPocoSupportResult poco )
        {
            _poco = poco;
        }

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
        /// <returns></returns>
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
                    var pocoClass = _poco.Roots.FirstOrDefault( root => root.PocoClass == type );
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
                    if( _poco.AllInterfaces.TryGetValue( type, out IPocoInterfaceInfo? itf ) )
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
                        monitor.Warn( $"Type {type} extends IPoco but cannot be found in the registered interfaces. It is ignored." );
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

        TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TypeScriptContext g, IPocoRootInfo root )
        {
            var t = g.DeclareTSType( monitor, root.PocoClass, requiresFile: true );
            return t != null ? EnsurePocoClass( monitor, t, root ) : null;
        }

        TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TSTypeFile tsTypedFile, IPocoRootInfo root )
        {
            if( tsTypedFile.TypePart == null )
            {
                // Creates a part at the top of the file for the implementation class.
                var b = tsTypedFile.EnsureTypePart();

                // Generates the signature 
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
                var createParamList = new List<TypeScriptVarType>( root.PropertyList.Count );
                foreach( var p in root.PropertyList )
                {
                    var propName = tsTypedFile.Context.Root.ToIdentifier( p.PropertyName );
                    var propType = GetPropertyTypeScriptType( monitor, tsTypedFile.Context, tsTypedFile.File, p );
                    if( propType == null ) return null;
                    var paramName = TypeScriptRoot.ToIdentifier( propName, false );

                    // Unifies the documentations from possibly more than one property declarations
                    // into a documentation text (without the stars).
                    var docElements = XmlDocumentationReader.GetDocumentationFor( monitor, p.DeclaredProperties );
                    var propComment = new DocumentationBuilder( withStars: false ).AppendDocumentation( b.File, docElements ).GetFinalText();
                    var paramComment = RemoveGetsOrSetsPrefix( propComment );

                    var prop = new TypeScriptPocoPropertyInfo( p, propType, propName, paramName, propComment, paramComment );
                    if( p.AutoInstantiated )
                    {
                        // The property is new'ed: its DefaultValue has been set to "new propType()". This works perfectly for
                        // (I)List (=> Array), (I)Set (=> Set) and (I)Dictionary (=> Map) but not for Poco: the interface's name
                        // must be replaced by its implementation class' name.
                        IPocoInterfaceInfo? iPoco = _poco.Find( p.PropertyType );
                        if( iPoco != null )
                        {
                            var c = tsTypedFile.Context.DeclareTSType( monitor, iPoco.Root.PocoClass, requiresFile: true );
                            if( c == null ) return null;
                            tsTypedFile.File.Imports.EnsureImport( c.File, c.TypeName );
                            prop.Property.DefaultValue = $"new {c.TypeName}()";
                        }
                    }
                    propList.Add( prop );

                    // If the property is new'ed, we don't expect a parameter.
                    if( !p.AutoInstantiated )
                    {
                        createParamList.Add( new TypeScriptVarType( paramName, propType ) { Optional = p.IsEventuallyNullable, Comment = paramComment } );
                    }
                }

                var pocoClass = new TypeScriptPocoClass( tsTypedFile.TypeName, b, root, propList, createParamList );
                var h = PocoGenerating;
                if( h != null )
                {
                    var ev = new PocoGeneratingEventArgs( monitor, tsTypedFile, pocoClass );
                    h.Invoke( this, ev );
                    if( ev.HasError )
                    {
                        monitor.Error( "Error occurred while raising PocoGenerating event." );
                        return null;
                    }
                }

                pocoClass.AppendProperties( b );
                pocoClass.AppendCreateMethod( b );
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
                    var b = tsTypedFile.EnsureTypePart();
                    b.AppendDocumentation( monitor, i.PocoInterface );
                    b.Append( "export interface " ).Append( tsTypedFile.TypeName );
                    bool hasInterface = false;
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
                            AddBaseInterface( b, ref hasInterface, fInterface );
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
                                    b.AppendImportedTypeName( declared );
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
                             .AppendIdentifier( p.PropertyName );
                            success &= AppendPocoPropertyTypeQualifier( monitor, tsTypedFile.Context, b, p );
                            b.Append( ";" ).NewLine();
                        }
                    }
                }
            }
            return tsTypedFile;

            static void AddBaseInterface( ITSKeyedCodePart b, ref bool hasInterface, TSTypeFile fInterface )
            {
                if( !hasInterface )
                {
                    b.Append( " extends " );
                    hasInterface = true;
                }
                else b.Append( ", " );
                b.AppendImportedTypeName( fInterface );
            }
        }

        bool AppendPocoPropertyTypeQualifier( IActivityMonitor monitor, TypeScriptContext g, ITSCodePart b, IPocoPropertyInfo p )
        {
            bool success = true;
            b.Append( p.IsEventuallyNullable ? "?: " : ": " );
            bool hasUnions = false;
            foreach( var (t, nullInfo) in p.PropertyUnionTypes )
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

        static string? GetPropertyTypeScriptType( IActivityMonitor monitor, TypeScriptContext g, TypeScriptFile file, IPocoPropertyInfo p )
        {
            var b = file.CreateDetachedPart();
            bool hasUnions = false;
            foreach( var (t, nullInfo) in p.PropertyUnionTypes )
            {
                if( hasUnions ) b.Append( "|" );
                hasUnions = true;
                if( !b.AppendComplexTypeName( monitor, g, t ) ) return null;
            }
            if( !hasUnions )
            {
                if( !b.AppendComplexTypeName( monitor, g, p.PropertyNullableTypeTree, withUndefined: false ) ) return null;
            }
            return b.ToString();
        }

    }
}
