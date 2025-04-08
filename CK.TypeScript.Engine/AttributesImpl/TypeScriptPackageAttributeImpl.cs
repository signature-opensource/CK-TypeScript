using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implementation class of the <see cref="TypeScriptPackageAttribute"/>.
/// <para>
/// This must be used as the base class of specialized TypeScriptPackageAttribute implementations.
/// </para>
/// </summary>
public class TypeScriptPackageAttributeImpl : IAttributeContextBoundInitializer, ITSCodeGeneratorAutoDiscovery
{
    readonly TypeScriptPackageAttribute _attr;
    readonly Type _type;
    readonly List<TypeScriptPackageAttributeImplExtension> _extensions;
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
    public TypeScriptPackageAttribute Attribute => _attr;

    /// <summary>
    /// Gets the decorated type.
    /// </summary>
    public Type DecoratedType => _type;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptPackageAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">Required monitor.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptPackageAttributeImpl( IActivityMonitor monitor, TypeScriptPackageAttribute attr, Type type )
    {
        _attr = attr;
        _type = type;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptPackage] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }

        _extensions = new List<TypeScriptPackageAttributeImplExtension>();
        // Initializes TypeScriptFolder.
        Throw.DebugAssert( type.Namespace != null );
        if( string.IsNullOrWhiteSpace( attr.TypeScriptFolder ) )
        {
            _typeScriptFolder = type.Namespace.Replace( '.', '/' );
        }
        else
        {
            _typeScriptFolder = new NormalizedPath( attr.TypeScriptFolder );
            if( _typeScriptFolder.IsRooted )
            {
                monitor.Warn( $"[TypeScriptPackage] on '{type:C}': TypeScriptFolder is rooted, this is useless and removed." );
                _typeScriptFolder = _typeScriptFolder.With( NormalizedPathRootKind.None );
            }
        }
    }

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        _owner = owner;
    }

    internal void AddExtension( TypeScriptPackageAttributeImplExtension e )
    {
        _extensions.Add( e );
    }

    /// <summary>
    /// Called once the <see cref="ITSCodeGeneratorFactory"/> have created their <see cref="ITSCodeGenerator"/>.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="initializer">The TypeScriptContext initializer.</param>
    /// <returns>True on success, false otherwise (errors must be logged).</returns>
    internal bool InitializeTypeScriptPackage( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
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

    internal protected virtual bool ConfigureResPackage( IActivityMonitor monitor,
                                                      TypeScriptContext context,
                                                      ResourceSpaceConfiguration spaceBuilder )
    {
        var d = spaceBuilder.RegisterPackage( monitor, DecoratedType, _typeScriptFolder );
        if( d == null ) return false;
        return OnConfiguredPackage( monitor, context, spaceBuilder, d );
    }

    protected virtual bool OnConfiguredPackage( IActivityMonitor monitor,
                                                TypeScriptContext context,
                                                ResourceSpaceConfiguration spaceBuilder,
                                                ResPackageDescriptor d )
    {
        bool success = true;
        foreach( var e in _extensions )
        {
            success &= e.OnConfiguredPackage( monitor, this, context, d, spaceBuilder );
        }
        return success;
    }
}
