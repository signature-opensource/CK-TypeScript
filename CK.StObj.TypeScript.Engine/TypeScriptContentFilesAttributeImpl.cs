using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

public sealed class TypeScriptContentFilesAttributeImpl : ITSCodeGenerator, IAttributeContextBoundInitializer
{
    readonly TypeScriptContentFilesAttribute _attr;
    readonly Type _target;
    List<ResourceTypeLocator>? _allRes;
    int _prefixLength;
    NormalizedPath _targetPath;

    public TypeScriptContentFilesAttributeImpl( TypeScriptContentFilesAttribute attr, Type target )
    {
        _attr = attr;
        _target = target;
    }

    public void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        if( string.IsNullOrWhiteSpace( _attr.ResourcePathPrefix )
            || _attr.ResourcePathPrefix.Contains( '\\' ) )
        {
            monitor.Error( $"[TypeScriptContentFiles( \"{_attr.ResourcePathPrefix}\" )] on '{_target}': must not be empty nor contain '\\'." );
            return;
        }
        string prefix = "ck@" + _attr.ResourcePathPrefix;
        if( prefix[^1] != '/' ) prefix += '/';
        var ressources = _target.Assembly.GetSortedResourceNames();
        // TODO: replace this with dichotomic lookup (waiting for immutable array instead of IReadOnlyList).
        _allRes = ressources.Where( n => n.Length > prefix.Length && n.StartsWith( prefix, StringComparison.Ordinal ) )
                            .Select( n => new ResourceTypeLocator( _target, n ) )
                            .ToList();
        if( _allRes.Count == 0 )
        {
            monitor.Warn( $"Unable to find at least one file for [TypeScriptContentFiles( \"{_attr.ResourcePathPrefix}\")] on type '{_target:N}'." );
        }
        // One cannot use the _attr.ResourcePathPrefix to remove the resource path prefix: it may or not end with the /.
        _prefixLength = prefix.Length;
        _targetPath = _attr.TargetFolderName ?? _target.Namespace!.Replace( '.', '/' );
        _targetPath = _targetPath.ResolveDots();
    }

    public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

    public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        Throw.DebugAssert( _allRes != null );
        foreach( ResourceTypeLocator o in _allRes )
        {
            var targetFileName = _targetPath.Combine( o.ResourceName.Substring( _prefixLength ) );
            context.Root.Root.CreateResourceFile( o, targetFileName );
        }
        return true;
    }
}
