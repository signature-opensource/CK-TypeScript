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
        public bool ConfigureBuilder( IActivityMonitor monitor,
                                      TypeScriptContext context,
                                      TSGeneratedTypeBuilder builder )
        {
            var t = PocoTypeSystem.FindObliviousType( builder.Type );
            if( t != null )
            {
                // We are interested only in named records, abstract and concrete IPoco.
                if( t.Kind == PocoTypeKind.Record || t.Kind == PocoTypeKind.IPoco || t.Kind == PocoTypeKind.AbstractIPoco )
                {
                    // For IPoco, if the TypeName is not set (which should almost never be set),
                    // we remove the 'I' if this is the interface name.
                    if( t.Kind == PocoTypeKind.IPoco )
                    {
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
                    }
                    builder.Implementor = t switch
                    {
                        IRecordPocoType r => ( monitor, tsType ) => GenerateRecord( monitor, tsType, r ),
                        IPrimaryPocoType p => ( monitor, tsType ) => GeneratePrimaryPoco( monitor, tsType, p ),
                        IAbstractPocoType a => ( monitor, tsType ) => GenerateAbstractPoco( monitor, tsType, a ),
                        _ => Throw.NotSupportedException<TSCodeGenerator>()
                    };
                }
            }
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

        bool GeneratePrimaryPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IPrimaryPocoType p )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.Append( "export class " ).Append( tsType.TypeName )
                .CreatePart( out var interfaces )
                .OpenBlock()
                .CreatePart( out var fields )
                .Append( "public constructor()" ).OpenBlock()
                .CreatePart( out var defaultCtor )
                .CloseBlock()
                .CreatePart( out var properties )
                .CreatePart( out var body );
            bool atLeastOne = false;
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

        bool GenerateAbstractPoco( IActivityMonitor monitor, ITSGeneratedType tsType, IAbstractPocoType a )
        {
            var part = CreateTypePart( monitor, tsType );
            if( part == null ) return false;
            part.Append( "export interface " ).Append( tsType.TypeName )
                .OpenBlock()
                .CreatePart( out var body );
            return true;
        }

        ITSKeyedCodePart? CreateTypePart( IActivityMonitor monitor, ITSGeneratedType tsType )
        {
            if( tsType.TypePart != null )
            {
                monitor.Error( $"Type part for '{tsType.Type:C}' has been initialized by another generator." );
                return null;
            }
            return tsType.EnsureTypePart();
        }

        public bool Initialize( IActivityMonitor monitor, TypeScriptContext context )
        {
            return true;
        }
    }
}
