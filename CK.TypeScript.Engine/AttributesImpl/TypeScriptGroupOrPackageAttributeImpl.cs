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

    /// <summary>
    /// Gets whether this is <see cref="IResourceGroup"/> rather than a <see cref="IResourcePackage"/>.
    /// </summary>
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
                        // But, first, it means that this must occur once the ResPackage are available (ResSpaceDataBuilder
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

    /// <summary>
    /// Registers the package in the <see cref="ResSpaceConfiguration"/> and handles
    /// the "index.ts" barrel file if it exists.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceBuilder">The resource space builder.</param>
    /// <returns>True on success, false on error.</returns>
    internal protected virtual bool CreateResPackageDescriptor( IActivityMonitor monitor,
                                                                TypeScriptContext context,
                                                                ResSpaceConfiguration spaceBuilder )
    {
        var d = spaceBuilder.RegisterPackage( monitor, spaceBuilder.TypeCache.Get( DecoratedType ), _typeScriptFolder );
        if( d == null ) return false;
        d.IsGroup = _isGroup;
        return OnCreateResPackageDescriptor( monitor, context, spaceBuilder, d );
    }

    /// <summary>
    /// Called by <see cref="CreateResPackageDescriptor(IActivityMonitor, TypeScriptContext, ResSpaceConfiguration)"/>.
    /// Calls all <see cref="TypeScriptGroupOrPackageAttributeImplExtension.OnConfiguredDescriptor(IActivityMonitor, TypeScriptContext, TypeScriptGroupOrPackageAttributeImpl, ResSpaceConfiguration, ResPackageDescriptor)"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="spaceBuilder">The resource space builder.</param>
    /// <param name="d">The package descriptor for this package.</param>
    /// <returns>True on success, false on error.</returns>
    protected virtual bool OnCreateResPackageDescriptor( IActivityMonitor monitor,
                                                         TypeScriptContext context,
                                                         ResSpaceConfiguration spaceBuilder,
                                                         ResPackageDescriptor d )
    {
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.OnConfiguredDescriptor( monitor, context, this, spaceBuilder, d );
        }
        return success;
    }

    /// <summary>
    /// Handles the "index.ts" barrel file if it exists.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="package">The associated ResPackage.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected virtual bool OnResPackageAvailable( IActivityMonitor monitor,
                                                           TypeScriptContext context,
                                                           ResSpaceData spaceData,
                                                           ResPackage package )
    {
        // Handles barrel.
        if( spaceData.RemoveCodeHandledResource( package, "index.ts", out var barrel ) )
        {
            var targetPath = _typeScriptFolder.AppendPart( "index.ts" );
            context.Root.Root.FindOrCreateResourceFile( barrel, targetPath );
            monitor.Trace( $"Manual barrel '{targetPath}' is moved to the <Code> container." );
        }
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.OnResPackageAvailable( monitor, context, this, spaceData, package );
        }
        return success;
    }

    /// <summary>
    /// a [ReaDI] method called when the final <see cref="ResSpace"/> is available.
    /// <para>
    /// At this level, dispatches the call to <see cref="TypeScriptGroupOrPackageAttributeImplExtension.OnResSpaceAvailable(IActivityMonitor, TypeScriptContext, TypeScriptGroupOrPackageAttributeImpl, ResSpace, ResPackage)"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="context">The context.</param>
    /// <param name="resPackage">The associated ResPackage.</param>
    /// <param name="resSpace">The available ResSpace.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    internal protected virtual bool OnResSpaceAvailable( IActivityMonitor monitor,
                                                         TypeScriptContext context,
                                                         ResPackage resPackage,
                                                         ResSpace resSpace )
    {
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.OnResSpaceAvailable( monitor, context, this, resSpace, resPackage );
        }
        return success;
    }
}
