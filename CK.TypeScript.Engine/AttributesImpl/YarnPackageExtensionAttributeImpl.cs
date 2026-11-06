using CK.Core;
using CK.Setup;
using System;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.Engine;

/// <summary>
/// Implements <see cref="YarnPackageExtensionAttribute"/>: checks the declaration and registers it in the <see cref="TypeScriptContext"/>.
/// </summary>
public sealed class YarnPackageExtensionAttributeImpl : TypeScriptGroupOrPackageAttributeImplExtension
{
    /// <summary>
    /// Initializes a new implementation.
    /// </summary>
    /// <param name="attr">The attribute.</param>
    /// <param name="type">The decorated type.</param>
    public YarnPackageExtensionAttributeImpl( YarnPackageExtensionAttribute attr, Type type )
        : base( attr, type )
    {
    }

    /// <summary>
    /// Gets the attribute.
    /// </summary>
    public new YarnPackageExtensionAttribute Attribute => Unsafe.As<YarnPackageExtensionAttribute>( base.Attribute );

    internal protected override bool OnResPackageAvailable( IActivityMonitor monitor,
                                                            TypeScriptContext context,
                                                            TypeScriptGroupOrPackageAttributeImpl tsPackage,
                                                            ResSpaceData spaceData,
                                                            ResPackage package )
    {
        var a = Attribute;
        var decl = $"[YarnPackageExtension( \"{a.Selector}\", DependencyKind.{a.Kind}, \"{a.Name}\", \"{a.Range}\" )] on '{Type:N}'";
        bool success = true;
        if( string.IsNullOrWhiteSpace( a.Selector )
            || string.IsNullOrWhiteSpace( a.Name )
            || string.IsNullOrWhiteSpace( a.Range ) )
        {
            monitor.Error( $"{decl}: the selector, the name and the range must not be empty." );
            return false;
        }
        var (name, range) = YarnSettings.SplitSelector( a.Selector );
        if( name.Length == 0 || range.Length == 0 )
        {
            monitor.Error( $"{decl}: the selector must be a package name followed by a range (\"{name}@*\"): Yarn rejects a selector without range." );
            success = false;
        }
        if( a.Kind is not (DependencyKind.Dependency or DependencyKind.PeerDependency) )
        {
            monitor.Error( $"{decl}: a package extension can add a Dependency or a PeerDependency only (Yarn has no dev dependencies in a package extension: the dev dependencies of an installed package are never installed)." );
            success = false;
        }
        if( success )
        {
            context.AddYarnPackageExtension( package.Resources.Index, a, Type.ToCSharpName() );
        }
        return success;
    }
}
