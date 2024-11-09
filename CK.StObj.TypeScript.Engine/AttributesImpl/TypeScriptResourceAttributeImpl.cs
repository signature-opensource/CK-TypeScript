using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.StObj.TypeScript.Engine;

public sealed class TypeScriptResourceAttributeImpl : TypeScriptPackageAttributeImplExtension
{
    ResourceTypeLocator _resource;
    NormalizedPath _targetPath;

    public new TypeScriptResourceAttribute Attribute => Unsafe.As<TypeScriptResourceAttribute>( base.Attribute );

    /// <summary>
    /// Initializes a new <see cref="TypeScriptFileAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptResourceAttributeImpl( IActivityMonitor monitor, TypeScriptResourceAttribute attr, Type type )
        : base( attr, type )
    {
        if( string.IsNullOrWhiteSpace( Attribute.ResourcePath ) )
        {
            monitor.Error( $"[{AttributeName}( \"{Attribute.ResourcePath}\" )] on '{type:N}': invalid resource path. It must not be empty." );
        }
    }

    protected override void OnInitialize( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, ITypeAttributesCache owner )
    {
        if( tsPackage.Resources.TryGetResource( monitor, Attribute.ResourcePath, out _resource ) )
        {
            _targetPath = Attribute.TargetFolder ?? tsPackage.TypeScriptFolder;
            _targetPath = _targetPath.ResolveDots().AppendPart( Path.GetFileName( Attribute.ResourcePath ) );
            tsPackage.RemoveResource( _resource );
        }
    }

    protected internal override bool GenerateCode( IActivityMonitor monitor, TypeScriptPackageAttributeImpl tsPackage, TypeScriptContext context )
    {
        Throw.DebugAssert( "If initialization failed, we never reach this point.", _resource.IsValid );
        context.Root.Root.CreateResourceFile( in _resource, _targetPath );
        return true;
    }

}
