using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.Setup.PocoJson;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

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
    public partial class PocoCodeGenerator : ITSCodeGenerator
    {
        readonly TSPocoTypeMap _pocoTypeMap;

        internal PocoCodeGenerator( IPocoTypeSystem pocoTypeSystem, TSPocoTypeMap pocoTypeMap )
        {
            PocoTypeSystem = pocoTypeSystem;
            _pocoTypeMap = pocoTypeMap;
        }

        /// <summary>
        /// Gets the type system.
        /// </summary>
        public IPocoTypeSystem PocoTypeSystem { get; }

        /// <summary>
        /// If the type is a <see cref="IPoco"/>, the <see cref="INamedPocoType."/> sets the type name
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
        /// <returns>True on success, false on error (errors must be logged): always true here.</returns>
        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                  ITSTypeFileBuilder builder,
                                                  TypeScriptAttribute attr )
        {
            var t = PocoTypeSystem.FindObliviousType( builder.Type );
            if( t != null )
            {
                // Even if we didn't call DeclareTSType because a type is not exchangeable, when a TypeScriptAttribute has been declared
                // on a IPoco, we have a type to handle: we use the CancelGeneration to definitely condemn this type.
                if( !t.IsExchangeable )
                {
                    builder.CancelGeneration = false;
                }
                else
                {
                    // We are interested only in named records, abstract and concrete IPoco and the Guid
                    // for which a small wrapper is generated.
                    if( builder.Type == typeof( Guid ) )
                    {
                        builder.Finalizer = GenerateGuid;
                    }
                    else if( t.Kind == PocoTypeKind.Record || t.Kind == PocoTypeKind.IPoco || t.Kind == PocoTypeKind.AbstractIPoco )
                    {
                        // For IPoco, if the TypeName is not set (which should almost never be set),
                        // we remove the 'I' if this is the interface name.
                        if( t.Kind == PocoTypeKind.IPoco )
                        {
                            if( attr.TypeName == null )
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
                                attr.TypeName = TypeScriptContext.SafeNameChars( typeName );
                            }
                        }
                        builder.Finalizer = t switch
                        {
                            IRecordPocoType r => ( monitor, file ) => GenerateRecord( monitor, file, r ),
                            IPrimaryPocoType p => ( monitor, file ) => GeneratePrimaryPoco( monitor, file, p ),
                            IAbstractPocoType a => ( monitor, file ) => GenerateAbstractPoco( monitor, file, a ),
                            _ => Throw.NotSupportedException<Func<IActivityMonitor, TSTypeFile, bool>>()
                        };
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

        bool GenerateRecord( IActivityMonitor monitor, TSTypeFile file, IRecordPocoType r )
        {
            var part = CreateTypePart( monitor, file );
            if( part == null ) return false;
            part.Append( "export class " ).Append( file.TypeName )
                .OpenBlock();
            return true;
        }

        bool GeneratePrimaryPoco( IActivityMonitor monitor, TSTypeFile file, IPrimaryPocoType p )
        {
            var part = CreateTypePart( monitor, file );
            if( part == null ) return false;
            part.Append( "export class " ).Append( file.TypeName )
                .CreatePart( out var interfaces )
                .OpenBlock()
                .CreatePart( out var fields )
                .CreatePart( out var properties )
                .Append( "public constructor()" ).OpenBlock()
                .CreatePart( out var defaultCtor )
                .CloseBlock()
                .CreatePart( out var body );
            bool atLeastOne = false;
            foreach( var a in p.AbstractTypes )
            {
                if( atLeastOne ) interfaces.Append( ", " );
                else
                {
                    interfaces.Append( " implements " );
                    atLeastOne = true;
                }
                interfaces.Append( _pocoTypeMap.GetTSPocoType( monitor, a ) );
            }
            foreach( var f in p.Fields )
            {
                // Skips any non exchangeable fields.
                if( !p.IsExchangeable ) continue;

                Debug.Assert( f.FieldAccess != PocoFieldAccessKind.ReadOnly, "Readonly fields are non exchangeable." );

                var tsFieldType = _pocoTypeMap.GetTSPocoType( monitor, f.Type );
                // Creates the backing field.
                fields.Append( "private " );
                if( f.FieldAccess == PocoFieldAccessKind.MutableCollection || f.FieldAccess == PocoFieldAccessKind.IsByRef )
                {
                    // If it can be readonly, it should be.
                    fields.Append( "readonly " );
                }
                fields.Append( f.PrivateFieldName ).Space().Append( tsFieldType ).Append( ";" ).NewLine();

                defaultCtor.Append( "this." ).Append( f.PrivateFieldName ).Append( " = " );
                // If the field has a default value, use it. 
                if( f.HasOwnDefaultValue )
                {
                    Debug.Assert( f.DefaultValueInfo.DefaultValue != null );
                    defaultCtor.Append( f.DefaultValueInfo.DefaultValue.SimpleValue );
                }
                else
                {
                    defaultCtor.Append( tsFieldType.DefaultValueSource );
                }

                // Creates the property.
                if( f.FieldAccess == PocoFieldAccessKind.IsByRef )
                {
                    // A ref property is only the return of the ref backing field.
                    tB.Append( "public ref " ).Append( f.Type.CSharpName ).Space().Append( f.Name )
                      .Append( " => ref " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();
                }
                else
                {
                    // The getter is always the same.
                    tB.Append( "public " ).Append( f.Type.CSharpName ).Space().Append( f.Name );
                    if( f.FieldAccess != PocoFieldAccessKind.HasSetter )
                    {
                        Debug.Assert( f.FieldAccess == PocoFieldAccessKind.MutableCollection || f.FieldAccess == PocoFieldAccessKind.ReadOnly );
                        // Readonly and MutableCollection doesn't require the "get".
                        // This expose a public (read only) property that is required for MutableCollection but
                        // a little bit useless for pure ReadOnly. However we need an implementation of the property
                        // declared on the interface (we could have generated explicit implementations here but it
                        // would be overcomplicated).
                        tB.Append( " => " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();
                    }
                    else
                    {
                        // For writable properties we need the get/set. 
                        tB.OpenBlock()
                          .Append( "get => " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();

                        tB.Append( "set" )
                            .OpenBlock();
                        // Always generate the null check.
                        if( !f.Type.IsNullable && !f.Type.Type.IsValueType )
                        {
                            tB.Append( "Throw.CheckNotNullArgument( value );" ).NewLine();
                        }
                        // UnionType: check against the allowed types.
                        if( f.Type is IUnionPocoType uT )
                        {
                            Debug.Assert( f.Type.Kind == PocoTypeKind.UnionType );
                            // Generates the "static Type[] _vXXXAllowed" array.
                            fieldPart.Append( "static readonly Type[] " ).Append( f.PrivateFieldName ).Append( "Allowed = " )
                                     .AppendArray( uT.AllowedTypes.Select( u => u.Type ) ).Append( ";" ).NewLine();

                            if( f.Type.IsNullable ) tB.Append( "if( value != null )" ).OpenBlock();

                            // Generates the check.
                            tB.Append( "Type tV = value.GetType();" ).NewLine()
                              .Append( "if( !" ).Append( f.PrivateFieldName ).Append( "Allowed" )
                              .Append( ".Any( t => t.IsAssignableFrom( tV ) ) )" )
                                .OpenBlock()
                                .Append( "Throw.ArgumentException( $\"Unexpected Type '{tV.ToCSharpName()}' in UnionType. Allowed types are: '" )
                                .Append( uT.AllowedTypes.Select( tU => tU.CSharpName ).Concatenate( "', '" ) )
                                .Append( "'.\");" )
                                .CloseBlock();

                            if( f.Type.IsNullable ) tB.CloseBlock();
                        }
                        tB.Append( f.PrivateFieldName ).Append( " = value;" )
                            .CloseBlock()
                          .CloseBlock();
                    }
                }
                // Finally, provide an explicit implementations of all the declared properties
                // that are not satisfied by the final property type.
                foreach( IExtPropertyInfo prop in family.PropertyList[f.Index].DeclaredProperties )
                {
                    if( prop.Type != f.Type.Type )
                    {
                        if( prop.Type.IsByRef )
                        {
                            tB.Append( "ref " ).Append( prop.TypeCSharpName ).Space()
                                .Append( prop.DeclaringType.ToCSharpName() ).Append( "." ).Append( f.Name ).Space()
                                .Append( " => ref " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();
                        }
                        else if( prop.Type != f.Type.Type )
                        {
                            tB.Append( prop.TypeCSharpName ).Space()
                              .Append( prop.DeclaringType.ToCSharpName() ).Append( "." ).Append( f.Name ).Space()
                              .Append( " => " ).Append( f.PrivateFieldName ).Append( ";" ).NewLine();

                        }
                    }
                }
            }

            return true;
        }

        bool GenerateAbstractPoco( IActivityMonitor monitor, TSTypeFile file, IAbstractPocoType a )
        {
            var part = CreateTypePart( monitor, file );
            if( part == null ) return false;
            part.Append( "export interface " ).Append( file.TypeName )
                .OpenBlock()
                .CreatePart( out var body );
            return true;
        }

        ITSKeyedCodePart? CreateTypePart( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart != null )
            {
                monitor.Error( $"Type part for '{file.Type:C}' has been initialized by another generator." );
                return null;
            }
            return file.EnsureTypePart();
        }

        bool GenerateGuid( IActivityMonitor monitor, TSTypeFile file )
        {
            if( file.TypePart == null )
            {
                // Let the part open (use the "}\n" default closer so that this can be extended.
                file.EnsureTypePart().Append( @"
/**
* Simple immutable encapsulation of a string. No check is currently done on the 
* value format but it should be in the '00000000-0000-0000-0000-000000000000' form.
*/
export class Guid {

    /**
    * The empty Guid is '00000000-0000-0000-0000-000000000000'.
    */
    public static readonly empty : Guid = new Guid('00000000-0000-0000-0000-000000000000');
    
    constructor(public readonly value: string) {     
    }

    get [Symbol.toStringTag]() {
        return this.value;
        }

    public toJSON() : string {
        return this.value;
    }
" );
            }
            return true;
        }
    }
}
