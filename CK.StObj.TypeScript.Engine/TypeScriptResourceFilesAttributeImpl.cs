using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

public sealed class TypeScriptResourceFilesAttributeImpl : ITSCodeGenerator, IAttributeContextBoundInitializer
{
    readonly TypeScriptResourceFilesAttribute _attr;
    readonly Type _target;
    ImmutableArray<ResourceTypeLocator> _allRes;
    int _prefixLength;
    NormalizedPath _targetPath;

    public TypeScriptResourceFilesAttributeImpl( TypeScriptResourceFilesAttribute attr, Type target )
    {
        _attr = attr;
        _target = target;
    }

    public void Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
    {
        var packageAttributesImpl = TypeScriptFileAttributeImpl.GetPackageAttributesImpl( monitor, owner );
        if( packageAttributesImpl != null
            && packageAttributesImpl.TryGetResourceTypePath( monitor, out var resTypePath ) )
        {
            string prefix = $"ck@{resTypePath}/";
            var resNames = _target.Assembly.GetSortedResourceNames2().GetPrefixedStrings( prefix );
            if( resNames.Length == 0 )
            {
                monitor.Warn( $"Unable to find at least one file for [TypeScriptContentFiles] on type '{_target:N}'." );
                _allRes = ImmutableArray<ResourceTypeLocator>.Empty;
            }
            else
            {
                var b = ImmutableArray.CreateBuilder<ResourceTypeLocator>( resNames.Length );
                foreach( var n in resNames.Span )
                {
                    b.Add( new ResourceTypeLocator( _target, n ) );
                }
                _allRes = b.MoveToImmutable();
            }
            _prefixLength = prefix.Length;
            _targetPath = _attr.TargetFolderName ?? _target.Namespace!.Replace( '.', '/' );
            _targetPath = _targetPath.ResolveDots();
        }
    }

    public bool Initialize( IActivityMonitor monitor, ITypeScriptContextInitializer initializer ) => true;

    public bool OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    public bool OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    public bool StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
    {
        Throw.DebugAssert( "If Initialize failed, we cannot reach this point.", !_allRes.IsDefault );
        foreach( ResourceTypeLocator o in _allRes )
        {
            var targetFileName = _targetPath.Combine( o.ResourceName.Substring( _prefixLength ) );
            context.Root.Root.CreateResourceFile( o, targetFileName );
        }
        return true;
    }
}
