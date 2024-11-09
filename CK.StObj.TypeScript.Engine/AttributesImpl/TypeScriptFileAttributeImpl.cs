using CK.Core;
using CK.Setup;
using CK.TypeScript.CodeGen;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CK.StObj.TypeScript.Engine;



/// <summary>
/// Creates a TypeScript resource file (from an embedded '.ts' resource).
/// </summary>
public sealed class TypeScriptFileAttributeImpl : TypeScriptPackageAttributeImplExtension
{
    ResourceTypeLocator _resource;
    NormalizedPath _targetPath;

    /// <summary>
    /// Initializes a new <see cref="TypeScriptFileAttributeImpl"/>.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public TypeScriptFileAttributeImpl( IActivityMonitor monitor, TypeScriptFileAttribute attr, Type type )
        : base( attr, type )
    {
        if( string.IsNullOrWhiteSpace( Attribute.ResourcePath ) || !Attribute.ResourcePath.EndsWith( ".ts" ) )
        {
            monitor.Error( $"[TypeScriptFile( \"{Attribute.ResourcePath}\" )] on '{type:N}': invalid resource path. It must end with \".ts\"." );
        }
    }

    public new TypeScriptFileAttribute Attribute => Unsafe.As<TypeScriptFileAttribute>( base.Attribute );

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
        var file = context.Root.Root.CreateResourceFile( in _resource, _targetPath );
        Throw.DebugAssert( ".ts extension has been checked by Initialize.", file is ResourceTypeScriptFile );
        foreach( var tsType in Attribute.TypeNames )
        {
            if( string.IsNullOrWhiteSpace( tsType ) ) continue;
            Unsafe.As<ResourceTypeScriptFile>( file ).DeclareType( tsType );
        }
        return true;
    }

}
