using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.StObj.TypeScript.Engine;

public sealed class TypeScriptFileAttributeImpl : ITSCodeGenerator, IAttributeContextBoundInitializer
{
    readonly TypeScriptFileAttribute _attr;
    readonly Type _target;

    ResourceTypeLocator _resource;
    NormalizedPath _targetPath;

    public TypeScriptFileAttributeImpl( IActivityMonitor monitor, TypeScriptFileAttribute attr, Type type )
    {
        _attr = attr;
        _target = type;
        if( !typeof( TypeScriptPackage ).IsAssignableFrom( type ) )
        {
            monitor.Error( $"[TypeScriptFile] can only decorate a TypeScriptPackage: '{type:N}' is not a TypeScriptPackage." );
        }
    }

    public void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        if( _attr.ResourcePath == null
            || !_attr.ResourcePath.EndsWith( ".ts" )
            || _attr.ResourcePath.Contains( '\\' ) )
        {
            monitor.Error( $"[TypeScriptFile( \"{_attr.ResourcePath}\" )] on '{_target:N}': invalid resource path. It must end with \".ts\" and not contain '\\'." );
        }
        else
        {
            Throw.DebugAssert( owner.Type == _target );
            var packageAttributesImpl = GetPackageAttributesImpl( monitor, owner );
            if( packageAttributesImpl != null )
            {
                var resPath = packageAttributesImpl.GetCKResourceName( monitor, _attr.ResourcePath );
                if( resPath != null )
                {
                    _resource = new ResourceTypeLocator( _target, resPath );
                    _targetPath = _attr.TargetFolderName ?? _target.Namespace!.Replace( '.', '/' );
                    _targetPath = _targetPath.ResolveDots().AppendPart( Path.GetFileName( _attr.ResourcePath ) );
                }
            }
        }
    }

    internal static TypeScriptPackageAttributeImpl? GetPackageAttributesImpl( IActivityMonitor monitor, ITypeAttributesCache owner )
    {
        var r = owner.GetTypeCustomAttributes<TypeScriptPackageAttributeImpl>().SingleOrDefault();
        if( r == null )
        {
            monitor.Error( $"[TypeScriptFile] on '{owner.Type:N}' requires the [TypeScriptPackage] (or a specialization) to also be declared." );
        }
        return r;
    }

    public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

    public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        Throw.DebugAssert( "If initialization failed, we never reach this point.", _resource.IsValid );
        var file = context.Root.Root.CreateResourceFile( in _resource, _targetPath );
        Throw.DebugAssert( ".ts extension has been checked by Initialize.", file is ResourceTypeScriptFile );
        foreach( var tsType in _attr.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( tsType );
        }
        return true;
    }
}
