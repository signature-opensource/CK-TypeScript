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
using System.Threading.Tasks;

namespace CK.StObj.TypeScript.Engine;

public sealed class TypeScriptFileAttributeImpl : ITSCodeGenerator, IAttributeContextBoundInitializer
{
    readonly TypeScriptFileAttribute _attr;
    readonly Type _target;
    ResourceTypeLocator _resource;
    NormalizedPath _targetPath;

    public TypeScriptFileAttributeImpl( TypeScriptFileAttribute attr, Type target )
    {
        _attr = attr;
        _target = target;
    }

    public void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        if( _attr.ResourcePath == null
            || !_attr.ResourcePath.EndsWith( ".ts" )
            || _attr.ResourcePath.Contains( '\\' ) )
        {
            monitor.Error( $"[TypeScriptFile( \"{_attr.ResourcePath}\" )] on '{_target}': invalid resource path. It must end with \".ts\" and not contain '\\'." );
        }
        else
        {
            _resource = new ResourceTypeLocator( _target, "ck@" + _attr.ResourcePath );
            _targetPath = _attr.TargetFolderName ?? _target.Namespace!.Replace( '.', '/' );
            _targetPath = _targetPath.ResolveDots().AppendPart( Path.GetFileName( _attr.ResourcePath ) );
        }
    }

    public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

    public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
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
