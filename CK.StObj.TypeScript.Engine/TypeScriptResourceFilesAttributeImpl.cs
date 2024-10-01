using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.Collections.Immutable;
using System.Reflection;

namespace CK.StObj.TypeScript.Engine;

/// <summary>
/// Creates multiple resources file (from embedded resources) in the TypeScriptContext's Root folder.
/// This is stateless: the factory is the code generator.
/// </summary>
public sealed class TypeScriptResourceFilesAttributeImpl : IAttributeContextBoundInitializer, ITSCodeGeneratorFactory, ITSCodeGenerator
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

    void IAttributeContextBoundInitializer.Initialize( IActivityMonitor monitor, ITypeAttributesCache owner, MemberInfo m, Action<Type> alsoRegister )
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

    ITSCodeGenerator? ITSCodeGeneratorFactory.CreateTypeScriptGenerator( IActivityMonitor monitor, ITypeScriptContextInitializer initializer )
    {
        return this;
    }

    bool ITSCodeGenerator.OnResolveObjectKey( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromObjectEventArgs e ) => true;

    bool ITSCodeGenerator.OnResolveType( IActivityMonitor monitor, TypeScriptContext context, RequireTSFromTypeEventArgs builder ) => true;

    bool ITSCodeGenerator.StartCodeGeneration( IActivityMonitor monitor, TypeScriptContext context )
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
