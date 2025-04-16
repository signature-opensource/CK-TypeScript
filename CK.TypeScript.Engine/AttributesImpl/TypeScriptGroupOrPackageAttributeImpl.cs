using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/> and <see cref="TypeScriptGroupAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptGroup/PackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptGroupOrPackageAttributeImpl : IAttributeContextBoundInitializer, ITSCodeGeneratorAutoDiscovery
{
    readonly Attribute _attr;
    readonly Type _type;
    readonly List<TypeScriptGroupOrPackageAttributeImplExtension> _extensions;
    readonly bool _isGroup;
    NormalizedPath _typeScriptFolder;
    // This is here only to support RegisterTypeScriptType registration...
    // This is bad and must be refactored.
    [AllowNull] ITypeAttributesCache _owner;

    /// <summary>
    /// Gets the folder of this package. Defaults to the namespace of the decorated type unless
    /// specified by <see cref="TypeScriptPackageAttribute.TypeScriptFolder"/>.
    /// </summary>
    public NormalizedPath TypeScriptFolder => _typeScriptFolder;

    /// <summary>
    /// Enables specializations to specify the <see cref="TypeScriptFolder"/>.
    /// </summary>
    /// <param name="folder">The new folder.</param>
    protected void SetTypeScriptFolder( NormalizedPath folder ) => _typeScriptFolder = folder;

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public Attribute Attribute => _attr;

    public bool IsGroup => _isGroup;

    /// <summary>
    /// Gets the decorated type.
    /// </summary>
    public Type DecoratedType => _type;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptGroupOrPackageAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptGroupOrPackageAttributeImpl( IActivityMonitor monitor, Attribute attr, Type type )
    {
        _attr = attr;
        _type = type;

        string attrTypeName;
        string? folder;
        if( attr is TypeScriptGroupAttribute g )
        {
            folder = g.TypeScriptFolder;
            attrTypeName = "Group";
            _isGroup = true;
        }
        else
        {
            if( attr is not TypeScriptPackageAttribute p )
            {
                throw new ArgumentException( "Attribute type can only be TypeScriptGroupAttribute or TypeScriptPackageAttribute." );
            }
            folder = p.TypeScriptFolder;
            attrTypeName = "Package";
        }
        if( !(_isGroup ? typeof( TypeScriptGroup ) : typeof( TypeScriptPackage ) ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScript{attrTypeName}] can only decorate a TypeScript{attrTypeName}: '{type:N}' is not a TypeScript{attrTypeName}." );
        }
        _extensions = new List<TypeScriptGroupOrPackageAttributeImplExtension>();
        // Initializes TypeScriptFolder.
        Throw.DebugAssert( type.Namespace != null );
        if( string.IsNullOrWhiteSpace( folder ) )
        {
            _typeScriptFolder = type.Namespace.Replace( '.', '/' );
        }
        else
        {
            _typeScriptFolder = new NormalizedPath( folder );
            if( _typeScriptFolder.IsRooted )
            {
                monitor.Warn( $"[TypeScript{attrTypeName}] on '{type:C}': TypeScriptFolder is rooted, this is useless and removed." );
                _typeScriptFolder = _typeScriptFolder.With( NormalizedPathRootKind.None );
            }
        }
    }

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        _owner = owner;
    }

    internal void AddExtension( TypeScriptGroupOrPackageAttributeImplExtension e )
    {
        _extensions.Add( e );
    }

    internal bool HandleRegisterTypeScriptTypeAttributes( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        bool success = true;
        // Handle the RegisterTypeScriptTypeAttribute.
        bool overrideError = false;
        foreach( var r in _owner.GetTypeCustomAttributes<RegisterTypeScriptTypeAttribute>() )
        {
            success &= initializer.EnsureRegister( monitor, r.Type, mustBePocoType: false, attr =>
            {
                // A Register cannot override.
                if( attr != null )
                {
                    if( r.TypeName != attr.TypeName
                       || r.FileName != attr.FileName
                       || r.Folder != attr.Folder
                       || r.SameFileAs != attr.SameFileAs
                       || r.SameFolderAs != attr.SameFolderAs )
                    {
                        monitor.Error( $"[RegisterTypeScriptType] on '{_owner.Type:N}' overrides current '{r.Type:C}' configuration." );
                        overrideError = true;
                        // We MAY handle override here.
                        // But, first, it means that this must occur once the ResPackage are available (ResourceSpaceDataBuilder
                        // has done the topological sort of the ResPackageDescriptor).
                        // And... Is this a good idea?
                        // 
                        // monitor.Warn( $"[RegisterTypeScriptType] on '{_owner.Type:N}' overrides current '{r.Type:C}' configuration." );
                        // return attr.ApplyOverride( r );
                    }
                    return attr;
                }
                return new TypeScriptTypeAttribute( r );
            } );
        }
        return success && !overrideError;
    }

    internal protected virtual bool ConfigureResDescriptor( IActivityMonitor monitor,
                                                            TypeScriptContext context,
                                                            ResSpaceConfiguration spaceBuilder )
    {
        var d = spaceBuilder.RegisterPackage( monitor, DecoratedType, _typeScriptFolder );
        if( d == null ) return false;
        d.IsGroup = _isGroup;
        // Handles barrel.
        if( d.RemoveCodeHandledResource( "index.ts", out var barrel ) )
        {
            var targetPath = _typeScriptFolder.AppendPart( "index.ts" );
            context.Root.Root.FindOrCreateResourceFile( in barrel, targetPath );
            monitor.Trace( $"Exported barrel '{targetPath}'." );
        }
        return OnConfiguredDescriptor( monitor, context, spaceBuilder, d );
    }

    protected virtual bool OnConfiguredDescriptor( IActivityMonitor monitor,
                                                   TypeScriptContext context,
                                                   ResSpaceConfiguration spaceBuilder,
                                                   ResPackageDescriptor d )
    {
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.OnConfiguredDescriptor( monitor, this, context, d, spaceBuilder );
        }
        return success;
    }
}
