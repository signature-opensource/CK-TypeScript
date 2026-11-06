using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implements <see cref="PackageResolutionAttribute"/>: checks the declaration and registers it in the <see cref="TypeScriptContext"/>.
/// </summary>
public sealed class PackageResolutionAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public PackageResolutionAttributeImpl( PackageResolutionAttribute attr, Type type )
        : base( attr, type )
    {
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new PackageResolutionAttribute Attribute => Unsafe.As<PackageResolutionAttribute>( base.Attribute );

    internal protected override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                            TypeScriptContext context,
                                                            TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                            ResSpaceData spaceData,
                                                            ResPackage package )
    {
        var a = Attribute;
        if( string.IsNullOrWhiteSpace( a.Pattern ) || string.IsNullOrWhiteSpace( a.Version ) )
        {
            monitor.Error( $"[PackageResolution( \"{a.Pattern}\", \"{a.Version}\" )] on '{Type:N}': the pattern and the version must not be empty." );
            return false;
        }
        context.AddPackageResolution( package.Resources.Index, a, Type.ToCSharpName() );
        return true;
    }
}
