using CK.EmbeddedResources;
using CK.Setup;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// Handles a <see cref="ResourceSpaceCollector"/> to topologically sort its configured <see cref="ResPackageDescriptor"/>
/// to produce a <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>.
/// </summary>
public sealed class ResourceSpaceDataBuilder
{
    readonly IReadOnlyDictionary<object, ResPackageDescriptor> _packageIndex;
    readonly IReadOnlyCollection<ResPackageDescriptor> _packages;
    readonly int _localPackageCount;
    readonly int _typedPackageCount;
    readonly IResourceContainer? _generatedCodeContainer;
    readonly string? _appResourcesLocalPath;

    public ResourceSpaceDataBuilder( ResourceSpaceCollector collector )
    {
        _packageIndex = collector.PackageIndex;
        _packages = collector.Packages;
        _localPackageCount = collector.LocalPackageCount;
        _generatedCodeContainer = collector.GeneratedCodeContainer;
        _appResourcesLocalPath = collector.AppResourcesLocalPath;
        _typedPackageCount = collector.TypedPackageCount;
    }

    /// <summary>
    /// Produces the <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>
    /// toplogically sorted. 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The space data on success, null otherwise.</returns>
    public ResourceSpaceData? Build( IActivityMonitor monitor )
    {
        var sortResult = DependencySorter<ResPackageDescriptor>.OrderItems( monitor,
                                                                            _packages,
                                                                            discoverers: null );
        if( !sortResult.IsComplete )
        {
            sortResult.LogError( monitor );
            return null;
        }
        Throw.DebugAssert( sortResult.SortedItems != null );
        Throw.DebugAssert( "No items, only containers (and maybe groups).",
                            sortResult.SortedItems.All( s => s.IsGroup || s.IsGroupHead ) );

        // The "<Code>" package is the first package and represents the generated code.
        // It is empty (no child) and only contains the generated code as AfterResources by design.
        // This package is not local as it is not bound to any local path.
        // All packages that require no other package require it.
        // Note: We could choose the BeforeResources to hold the code generated container, this wouldn't
        //       change anything.
        static ResPackage CreateCodePackage( IResourceContainer? generatedCodeContainer )
        {
            var codeContainer = generatedCodeContainer ?? new EmptyResourceContainer( "Empty <Code>", isDisabled: false );
            var noHeadRes = new EmptyResourceContainer( "<Code>", isDisabled: true );
            // Code package has no content and is by construction the first package: its index and
            // the indexes of its resources are known.
            return new ResPackage( "<Code>",
                                   defaultTargetPath: default,
                                   idxBeforeResources: 0,
                                   beforeResources: new CodeStoreResources( noHeadRes, noHeadRes ),
                                   idxAfterResources: 1,
                                   afterResources: new CodeStoreResources( codeContainer, noHeadRes ),
                                   localPath: null,
                                   isGroup: false,
                                   type: null,
                                   requires: ImmutableArray<ResPackage>.Empty,
                                   children: ImmutableArray<ResPackage>.Empty,
                                   index: 0 );
        }

        // The "<App>" package is the last package and represents the Application being setup.
        // It is empty (no child) and only contains BeforeResources with an empty Code resources
        // container disabled by design and a Store FileSystemResourceContainer on the AppResourcesLocalPath.
        // It is a local package by design except if AppResourcesLocalPath is null.
        // It requires all packages that are not required by other packages.
        // It is initialized below after the others (once we know its requirements) and the indexes.
        // Note: We could choose the AfterResources to hold the app resources, this wouldn't
        //       change anything.
        static ResPackage CreateAppPackage( ref string? appLocalPath,
                                            ImmutableArray<ResPackage> appRequires,
                                            int index )
        {
            IResourceContainer appResStore;
            if( appLocalPath != null )
            {
                appResStore = new FileSystemResourceContainer( appLocalPath, "<App>" );
                appLocalPath = appResStore.ResourcePrefix;
            }
            else
            {
                appResStore = new EmptyResourceContainer( "Empty <Code>", isDisabled: false );
            }
            var noAppRes = new EmptyResourceContainer( "<App>", isDisabled: true );
            var appPackage = new ResPackage( "<App>",
                                             defaultTargetPath: default,
                                             idxBeforeResources: 2 * index,
                                             beforeResources: new CodeStoreResources( noAppRes, appResStore ),
                                             idxAfterResources: 2 * index + 1,
                                             afterResources: new CodeStoreResources( noAppRes, noAppRes ),
                                             localPath: appLocalPath,
                                             isGroup: false,
                                             type: null,
                                             requires: appRequires,
                                             children: ImmutableArray<ResPackage>.Empty,
                                             index );
            return appPackage;
        }

        // We have <Code> + _packages.Count + <App> total packages.
        var bAll = ImmutableArray.CreateBuilder<ResPackage>( _packages.Count + 2 );
        // We have 2 * (<Code> + _packages.Count + <App>) total IResPackageResources (the After and Before CodeStoreResources).
        // We don't use a list/builder here because indexes are provided by the SortedItems.
        var allPackageResources = new IResPackageResources[ 2 * bAll.Capacity ];
        // The final size of the index:
        // - 5 keys (FullName, Code and Store for Resources and AfterResources) per ResPackageDescriptor.
        // - Plus the type for the typed package.
        // - Plus the name of the "<Code>" and "<App>" packages.
        // This is by default. _appResourcesLocalPath and _generatedCodeContainer can add 2 more keys.
        var packageIndexSize = _packages.Count * 5 + _typedPackageCount + 2;
        // If the AppResourcesLocalPath is specified, then the tailPackage is a local package
        // and the package index has one more entry for the app local FileSystemResourceContainer.
        var localCount = _localPackageCount;
        if( _appResourcesLocalPath != null )
        {
            ++packageIndexSize;
            ++localCount;
        }
        var bLocal = ImmutableArray.CreateBuilder<ResPackage>( localCount );
        // We can initialize now the package index.
        if( _generatedCodeContainer != null ) ++packageIndexSize;
        var packageIndex = new Dictionary<object, ResPackage>( packageIndexSize );

        // Initialize the ResourceSpaceData instance on our mutable index.
        var space = new ResourceSpaceData( packageIndex );

        // Create the code package and adds it where it must be.
        ResPackage codePackage = CreateCodePackage( _generatedCodeContainer );
        bAll.Add( codePackage );
        packageIndex.Add( codePackage.FullName, codePackage );
        if( _generatedCodeContainer != null )
        {
            packageIndex.Add( _generatedCodeContainer, codePackage );
        }

        // Because ResPackage.Requires hash set is read only, we can share a unique instance
        // of the requires for the packages that need it.
        ImmutableArray<ResPackage> requiresCode = [codePackage];

        var bAppRequirements = ImmutableArray.CreateBuilder<ResPackage>();
        foreach( var s in sortResult.SortedItems )
        {
            Throw.DebugAssert( s.IsGroup == (s.HeadForGroup != null) );
            if( s.HeadForGroup != null )
            {
                ResPackageDescriptor d = s.Item;
                // Close the CodeGen resources.
                if( d.Resources.Code is CodeGenResourceContainer c1 ) c1.Close();
                if( d.AfterContentResources.Code is CodeGenResourceContainer c2 ) c2.Close();
                Throw.DebugAssert( d.Resources.Store is not CodeGenResourceContainer
                                   && d.AfterContentResources.Store is not CodeGenResourceContainer );

                Throw.DebugAssert( "A child cannot be required and a requirement cannot be a child.",
                                   !s.Requires.Intersect( s.Children ).Any() );
                // Requirements and children have already been indexed.
                ImmutableArray<ResPackage> requires = s.Requires.Any()
                                                        ? s.Requires.Select( s => packageIndex[s.Item] ).ToImmutableArray()
                                                        : requiresCode;
                ImmutableArray<ResPackage> children = s.Children.Select( s => packageIndex[s.Item] ).ToImmutableArray();
                var p = new ResPackage( d.FullName,
                                        d.DefaultTargetPath,
                                        s.HeadForGroup.Index,
                                        d.Resources,
                                        s.Index,
                                        d.AfterContentResources,
                                        d.LocalPath,
                                        d.IsGroup,
                                        d.Type,
                                        requires,
                                        children,
                                        bAll.Count );
                // The 5 or 6 indexes.
                packageIndex.Add( p.FullName, p );
                packageIndex.Add( p.BeforeResources.Resources.Store, p );
                packageIndex.Add( p.BeforeResources.Resources.Code, p );
                packageIndex.Add( p.AfterResources.Resources.Store, p );
                packageIndex.Add( p.AfterResources.Resources.Code, p );
                if( p.Type != null )
                {
                    packageIndex.Add( p.Type, p );
                }
                // Enlist the package resources.
                allPackageResources[p.BeforeResources.Index] = p.BeforeResources;
                allPackageResources[p.AfterResources.Index] = p.AfterResources;
                // Rank is 1-based. Rank = 1 is for the head of the Group.
                if( s.Rank == 2 )
                {
                    bAppRequirements.Add( p );
                }
                if( p.IsLocalPackage )
                {
                    bLocal.Add( p );
                }
            }
        }
        // We now can initialize the "<App>" package.
        // The FileSystemResourceContainer normalizes the path (ends with Path.DirectorySeparator)
        // and this must be normalized!
        string? appLocalPath = _appResourcesLocalPath;
        var appPackage = CreateAppPackage( ref appLocalPath, bAppRequirements.DrainToImmutable(), bAll.Count );
        packageIndex.Add( appPackage.FullName, appPackage );
        if( appLocalPath != null )
        {
            packageIndex.Add( appLocalPath, appPackage );
            bLocal.Add( appPackage );
        }
        Throw.DebugAssert( "Expected size reached.", packageIndex.Count == packageIndexSize );
        space._packages = bAll.MoveToImmutable();
        space._localPackages = bLocal.MoveToImmutable();
        space._allPackageResources = ImmutableCollectionsMarshal.AsImmutableArray( allPackageResources );
        return space;

    }
}
