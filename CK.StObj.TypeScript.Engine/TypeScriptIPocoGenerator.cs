using CK.CodeGen;
using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.StObj.TypeScript.Engine
{
    /// <summary>
    /// Handles TypeScript generation of <see cref="IPoco"/> by reproducing the IPoco interfaces
    /// in the TypeScript world.
    /// This generator doesn't generate any IPoco by default: they must be "asked" to be generated
    /// by instantiating a <see cref="TSTypeFile"/> for the IPoco interface.
    /// </summary>
    /// <remarks>
    /// This code generator is directly added by the <see cref="TypeScriptAspect"/>, it is not initiated by an
    /// attribute like other code generators (typically thanks to a <see cref="ContextBoundDelegationAttribute"/>).
    /// </remarks>
    internal class TypeScriptIPocoGenerator : ITSCodeGenerator
    {
        readonly IPocoSupportResult _poco;

        internal TypeScriptIPocoGenerator( IPocoSupportResult poco )
        {
            _poco = poco;
        }

        public bool ConfigureTypeScriptAttribute( IActivityMonitor monitor,
                                                  TypeScriptGenerator generator,
                                                  Type type,
                                                  TypeScriptAttribute attr,
                                                  IList<ITSCodeGeneratorType> generatorTypes,
                                                  ref Func<IActivityMonitor, TSTypeFile, bool>? finalizer )
        {
            bool isPoco = typeof( IPoco ).IsAssignableFrom( type );
            if( isPoco )
            {
                Func<IActivityMonitor, TSTypeFile, bool>? final = null;
                if( type.IsClass )
                {
                    var pocoClass = _poco.Roots.FirstOrDefault( root => root.PocoClass == type );
                    if( pocoClass != null )
                    {
                        final = ( m, f ) => EnsurePocoClass( m, f, pocoClass ) != null;
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
                        final = ( m, f ) => EnsurePocoInterface( m, f, itf ) != null;
                    }
                    else
                    {
                        monitor.Warn( $"Type {type} extends IPoco but cannot be found in the registered interfaces. It is ignored." );
                    }
                }
                if( final != null )
                {
                    if( finalizer != null )
                    {
                        monitor.Warn( $"A TypeScript finalizer is already associated. Composing it." );
                        var captured = finalizer;
                        finalizer = ( m, f ) => captured( m, f ) && final( m, f );
                    }
                    else
                    {
                        finalizer = final;
                    }
                }
            }
            return true;
        }

        bool ITSCodeGenerator.GenerateCode( IActivityMonitor monitor, TypeScriptGenerator context ) => true;

        TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TypeScriptGenerator g, IPocoRootInfo root )
        {
            return EnsurePocoClass( monitor, g.GetTSTypeFile( monitor, root.PocoClass ), root );
        }

        TSTypeFile? EnsurePocoClass( IActivityMonitor monitor, TSTypeFile tsTypedFile, IPocoRootInfo root )
        {
            if( tsTypedFile.TypePart == null )
            {
                var b = tsTypedFile.EnsureTypePart();
                b.Append( "export class " ).Append( tsTypedFile.TypeName ).Append( " extends" );
                foreach( IPocoInterfaceInfo i in root.Interfaces )
                {
                    var itf = EnsurePocoInterface( monitor, tsTypedFile.TypeScriptGenerator, i );
                    if( itf == null ) return null;
                    tsTypedFile.File.Imports.EnsureImport( itf.TypeName, itf.File );
                    b.Append( " " ).Append( itf.TypeName );
                }
                b.OpenBlock();
            }
            return tsTypedFile;
        }

        TSTypeFile? EnsurePocoInterface( IActivityMonitor monitor, TypeScriptGenerator g, IPocoInterfaceInfo i )
        {
            return EnsurePocoInterface( monitor, g.GetTSTypeFile( monitor, i.PocoInterface ), i );
        }

        TSTypeFile? EnsurePocoInterface( IActivityMonitor monitor, TSTypeFile tsTypedFile, IPocoInterfaceInfo i )
        {
            if( tsTypedFile.TypePart == null )
            {
                var b = tsTypedFile.EnsureTypePart();
                b.Append( "export interface " ).Append( tsTypedFile.TypeName );
                bool hasInterface = false;
                foreach( Type baseInterface in i.PocoInterface.GetInterfaces() )
                {
                    var baseItf = i.Root.Interfaces.FirstOrDefault( p => p.PocoInterface == baseInterface );
                    if( baseItf == null ) continue;
                    if( !hasInterface )
                    {
                        b.Append( " extends " );
                        hasInterface = true;
                    }
                    else b.Append( ", " );
                    var fInterface = EnsurePocoInterface( monitor, tsTypedFile.TypeScriptGenerator, baseItf );
                    if( fInterface == null ) return null;
                    b.Append( fInterface.TypeName );
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
                        success &= AppendProperty( monitor, tsTypedFile, b, p );
                    }
                }
            }
            return tsTypedFile;
        }

        bool AppendProperty( IActivityMonitor monitor, TSTypeFile tsTypedFile, ITSCodePart b, IPocoPropertyInfo p )
        {
            bool success = true;
            b.Append( tsTypedFile.TypeScriptGenerator.ToIdentifier( p.PropertyName ) ).Append( p.IsEventuallyNullable ? "?: " : ": " );
            bool hasUnions = false;
            foreach( var (t, nullInfo) in p.PropertyUnionTypes )
            {
                if( hasUnions ) b.Append( "|" );
                hasUnions = true;
                success &= AppendTypeName( monitor, tsTypedFile, b, t );
            }
            if( !hasUnions )
            {
                success &= AppendTypeName( monitor, tsTypedFile, b, p.PropertyNullableTypeTree, withUndefined: false );
            }
            b.Append( ";" ).NewLine();
            return success;
        }

        bool AppendTypeName( IActivityMonitor monitor, TSTypeFile tsTypedFile, ITSCodePart b, NullableTypeTree type, bool withUndefined = true )
        {
            bool success = true;
            var t = type.Type;
            if( t.IsArray )
            {
                b.Append( "Array<" );
                success &= AppendTypeName( monitor, tsTypedFile, b, type.SubTypes[0] );
                b.Append( ">" );
            }
            else if( type.Kind.IsTupleType() )
            {
                b.Append( "[" );
                foreach( var s in type.SubTypes )
                {
                    success &= AppendTypeName( monitor, tsTypedFile, b, s );
                }
                b.Append( "]" );
            }
            else if( t.IsGenericType )
            {
                var tDef = t.GetGenericTypeDefinition();
                if( type.SubTypes.Count == 2 && (tDef == typeof( IDictionary<,> ) || tDef == typeof( Dictionary<,> )) )
                {
                    b.Append( "Map<" );
                    success &= AppendTypeName( monitor, tsTypedFile, b, type.SubTypes[0] );
                    b.Append( "," );
                    success &= AppendTypeName( monitor, tsTypedFile, b, type.SubTypes[1] );
                    b.Append( ">" );
                }
                else if( type.SubTypes.Count == 1 )
                {
                    if( tDef == typeof( ISet<> ) || tDef == typeof( HashSet<> ) )
                    {
                        b.Append( "Set<" );
                        success &= AppendTypeName( monitor, tsTypedFile, b, type.SubTypes[0] );
                        b.Append( ">" );
                    }
                    else if( tDef == typeof( IList<> ) || tDef == typeof( List<> ) )
                    {
                        b.Append( "Array<" );
                        success &= AppendTypeName( monitor, tsTypedFile, b, type.SubTypes[0] );
                        b.Append( ">" );
                    }
                }
                else
                {
                    monitor.Error( $"Unhandled type '{t.FullName}' for TypeScript generation." );
                    return false;
                }
            }
            else if( t == typeof( int ) || t == typeof( float ) || t == typeof( double ) ) b.Append( "number" );
            else if( t == typeof( bool ) ) b.Append( "boolean" );
            else if( t == typeof( string ) ) b.Append( "string" );
            else if( t == typeof( object ) ) b.Append( "object" );
            else
            {
                var other = tsTypedFile.TypeScriptGenerator.GetTSTypeFile( monitor, t );
                tsTypedFile.File.Imports.EnsureImport( other.TypeName, other.File );
                b.Append( other.TypeName );
            }
            if( withUndefined && type.Kind.IsNullable() ) b.Append( "|undefined" );
            return success;
        }

        bool AppendTypeName( IActivityMonitor monitor, TSTypeFile tsTypedFile, ITSCodePart b, Type t )
        {
            bool success = true;
            if( t.IsArray )
            {
                b.Append( "Array<" );
                success &= AppendTypeName( monitor, tsTypedFile, b, t.GetElementType()! );
                b.Append( ">" );
            }
            else if( t.IsValueTuple() )
            {
                b.Append( "[" );
                foreach( var s in t.GetGenericArguments() )
                {
                    success &= AppendTypeName( monitor, tsTypedFile, b, s );
                }
                b.Append( "]" );
            }
            else if( t.IsGenericType )
            {
                var tDef = t.GetGenericTypeDefinition();
                if( tDef == typeof( IDictionary<,> ) || tDef == typeof( Dictionary<,> ) )
                {
                    var args = t.GetGenericArguments();
                    b.Append( "Map<" );
                    success &= AppendTypeName( monitor, tsTypedFile, b, args[0] );
                    b.Append( "," );
                    success &= AppendTypeName( monitor, tsTypedFile, b, args[1] );
                    b.Append( ">" );
                }
                else if( tDef == typeof( ISet<> ) || tDef == typeof( HashSet<> ) )
                {
                    b.Append( "Set<" );
                    success &= AppendTypeName( monitor, tsTypedFile, b, t.GetGenericArguments()[0] );
                    b.Append( ">" );
                }
                else if( tDef == typeof( IList<> ) || tDef == typeof( List<> ) )
                {
                    b.Append( "Array<" );
                    success &= AppendTypeName( monitor, tsTypedFile, b, t.GetGenericArguments()[0] );
                    b.Append( ">" );
                }
                else
                {
                    monitor.Error( $"Unhandled type '{t.FullName}' for TypeScript generation." );
                    return false;
                }
            }
            else if( t == typeof( int ) || t == typeof( float ) || t == typeof( double ) ) b.Append( "number" );
            else if( t == typeof( bool ) ) b.Append( "boolean" );
            else if( t == typeof( string ) ) b.Append( "string" );
            else if( t == typeof( object ) ) b.Append( "unknown" );
            else
            {
                var other = tsTypedFile.TypeScriptGenerator.GetTSTypeFile( monitor, t );
                tsTypedFile.File.Imports.EnsureImport( other.TypeName, other.File );
                b.Append( other.TypeName );
            }
            return success;
        }

    }
}
