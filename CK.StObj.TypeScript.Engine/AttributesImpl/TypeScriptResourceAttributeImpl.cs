using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Creates a TypeScript resource file (from an embedded '.ts' resource) in the TypeScriptContext's Root folder.
/// This is stateless: the factory is the code generator.
/// </summary>
public sealed class TypeScriptResourceAttributeImpl : IAttributeContextBoundInitializer, ITSCodeGeneratorFactory, ITSCodeGenerator
{
    readonly TypeScriptResourceAttribute _attr;
    readonly Type _target;

    ResourceTypeLocator _resource;
    NormalizedPath _targetPath;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptFileAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptResourceAttributeImpl( IActivityMonitor monitor, TypeScriptResourceAttribute attr, Type type )
    {
        _attr = attr;
        _target = type;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptResource] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }
        if( string.IsNullOrWhiteSpace( _attr.ResourcePath ) )
        {
            monitor.Error( $"[TypeScriptResource( \"{_attr.ResourcePath}\" )] on '{_target:N}': invalid resource path. It must not be empty." );
        }
    }

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        Throw.DebugAssert( owner.Type == _target );
        var packageAttributesImpl = GetPackageAttributesImpl( monitor, owner );
        if( packageAttributesImpl != null
            && packageAttributesImpl.Resources.TryGetResource( monitor, _attr.ResourcePath, out _resource ) )
        {
            _targetPath = _attr.TargetFolderName ?? packageAttributesImpl.TypeScriptFolder;
            _targetPath = _targetPath.ResolveDots().AppendPart( Path.GetFileName( _attr.ResourcePath ) );
            packageAttributesImpl.RemoveResource( _resource );
        }
    }

    internal static TypeScriptPackageAttributeImpl? GetPackageAttributesImpl( IActivityMonitor monitor, ITypeAttributesCache owner )
    {
        var r = owner.GetTypeCustomAttributes<TypeScriptPackageAttributeImpl>().SingleOrDefault();
        if( r == null )
        {
            monitor.Error( $"[TypeScriptResource] on '{owner.Type:N}' requires the [TypeScriptPackage] (or a specialization) to also be declared." );
        }
        return r;
    }

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return this;
    }

    bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        Throw.DebugAssert( "If initialization failed, we never reach this point.", _resource.IsValid );
        context.Root.Root.CreateResourceFile( in _resource, _targetPath );
        return true;
    }

}
