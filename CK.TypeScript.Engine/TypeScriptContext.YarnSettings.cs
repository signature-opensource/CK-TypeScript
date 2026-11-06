using CK.Core;
using CK.TypeScript;
using System.Collections.Generic;
using System.Linq;

namespace CK.Setup;

public sealed partial class TypeScriptContext // Yarn settings support.
{
    // Collectors for [YarnPackageExtension] and [PackageResolution]: (package index, declaration order, attribute, source).
    List<(int Order, int Seq, YarnPackageExtensionAttribute Attr, string Source)>? _yarnPackageExtensions;
    List<(int Order, int Seq, PackageResolutionAttribute Attr, string Source)>? _packageResolutions;

    /// <summary>
    /// Called by YarnPackageExtensionAttributeImpl.
    /// </summary>
    internal void AddYarnPackageExtension( int order, YarnPackageExtensionAttribute attr, string source )
    {
        _yarnPackageExtensions ??= new();
        _yarnPackageExtensions.Add( (order, _yarnPackageExtensions.Count, attr, source) );
    }

    /// <summary>
    /// Called by PackageResolutionAttributeImpl.
    /// </summary>
    internal void AddPackageResolution( int order, PackageResolutionAttribute attr, string source )
    {
        _packageResolutions ??= new();
        _packageResolutions.Add( (order, _packageResolutions.Count, attr, source) );
    }

    YarnSettings? CreateYarnSettings( IActivityMonitor monitor )
    {
        using( monitor.OpenTrace( "Merging the Yarn package extensions and resolutions declared by the packages." ) )
        {
            return YarnSettings.Create( monitor,
                                        (_yarnPackageExtensions ?? []).OrderBy( e => e.Order ).ThenBy( e => e.Seq ).Select( e => (e.Attr, e.Source) ),
                                        (_packageResolutions ?? []).OrderBy( e => e.Order ).ThenBy( e => e.Seq ).Select( e => (e.Attr, e.Source) ),
                                        _tsRoot.LibraryManager.IgnoreVersionsBound );
        }
    }
}
