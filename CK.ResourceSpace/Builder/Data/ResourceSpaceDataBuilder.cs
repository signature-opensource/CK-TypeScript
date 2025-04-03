using CK.EmbeddedResources;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CK.Core;

/// <summary>
/// Handles a <see cref="ResourceSpaceCollector"/>: topologically sorts its configured <see cref="ResPackageDescriptor"/>
/// to produce a <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>.
/// </summary>
public sealed class ResourceSpaceDataBuilder
{
    readonly ResourceSpaceCollector _collector;
     
    public ResourceSpaceDataBuilder( ResourceSpaceCollector collector )
    {
        _collector = collector;
    }

    /// <summary>
    /// Produces the <see cref="ResourceSpaceData"/> with its final <see cref="ResPackage"/>
    /// toplogically sorted. 
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The space data on success, null otherwise.</returns>
    public ResourceSpaceData? Build( IActivityMonitor monitor )
    {
        if( !_collector.CloseRegistrations( monitor ) )
        {
            return null;
        }
        var sortResult = DependencySorter<ResPackageDescriptor>.OrderItems( monitor,
                                                                            _collector.Packages,
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
        static ResPackage CreateCodePackage( ResPackageDataCacheBuilder dataCacheBuilder, IResourceContainer? generatedCodeContainer )
        {
            var noHeadRes = new EmptyResourceContainer( "<Code>", isDisabled: true );
            var codeContainer = generatedCodeContainer ?? new EmptyResourceContainer( "Empty <Code>", isDisabled: false );
            // Code package has no content and is by construction the first package: its index and
            // the indexes of its resources are known.
            return new ResPackage( dataCacheBuilder,
                                   "<Code>",
                                   defaultTargetPath: default,
                                   idxBeforeResources: 0,
                                   beforeResources: noHeadRes,
                                   idxAfterResources: 1,
                                   afterResources: codeContainer,
                                   isGroup: false,
                                   type: null,
                                   requires: ImmutableArray<ResPackage>.Empty,
                                   children: ImmutableArray<ResPackage>.Empty,
                                   index: 1 );
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
                                            ResPackageDataCacheBuilder dataCacheBuilder,
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
            int idxResources = 2 * (index - 1); 
            var appPackage = new ResPackage( dataCacheBuilder,
                                             "<App>",
                                             defaultTargetPath: default,
                                             idxBeforeResources: idxResources,
                                             beforeResources: appResStore,
                                             idxAfterResources: idxResources + 1,
                                             afterResources: noAppRes,
                                             isGroup: false,
                                             type: null,
                                             requires: appRequires,
                                             children: ImmutableArray<ResPackage>.Empty,
                                             index );
            return appPackage;
        }

        var descriptorPackageCount = _collector.Packages.Count;
        var generatedCodeContainer = _collector.GeneratedCodeContainer;
        string? appLocalPath = _collector.AppResourcesLocalPath;
        var localCount = _collector.LocalPackageCount;

        // We have 1 + <Code> + descriptorPackageCount + <App> total packages.
        // The [0] index is null! and is an invalid index: ResPackage.Index is one-based.
        var bAll = ImmutableArray.CreateBuilder<ResPackage>( descriptorPackageCount + 3 );
        // We have 2 * (<Code> + descriptorPackageCount + <App>) total IResPackageResources (the After and Before).
        // We don't use a list/builder here because indexes are provided by the SortedItems.
        // IResPackageResources.Index is zero based.
        var allPackageResources = new IResPackageResources[ 2 * (descriptorPackageCount+2) ];
        // The final size of the package index:
        // - 1 FullName per ResPackageDescriptor.
        // - Plus the type for the typed package.
        // - Plus the name of the "<Code>" and "<App>" packages.
        var packageIndexSize = descriptorPackageCount + _collector.TypedPackageCount + 2;
        // The final size of the _resourceIndex (the IResourceContainer to IResPackageResources map
        // for Code and Store for BeforeResources and AfterResources.
        // This is by default. AppResourcesLocalPath and GeneratedCodeContainer can add 2 more keys.
        var resourceIndexSize = descriptorPackageCount * 4;
        if( generatedCodeContainer != null ) ++resourceIndexSize;
        // If the AppResourcesLocalPath is specified, then the tailPackage is a local package
        // and the resource index has one more entry for the app local FileSystemResourceContainer.
        if( appLocalPath != null )
        {
            ++resourceIndexSize;
            ++localCount;
        }
        var bLocal = ImmutableArray.CreateBuilder<ResPackage>( localCount );
        // We can initialize now the packageIndex and the resourceIndex.
        var packageIndex = new Dictionary<object, ResPackage>( packageIndexSize );
        var resourceIndex = new Dictionary<IResourceContainer, IResPackageResources>( resourceIndexSize );

        // Initialize the ResourceSpaceData instance on our mutable packageIndex.
        var space = new ResourceSpaceData( _collector.CKGenPath, _collector.LiveStatePath, packageIndex );

        // ResPackageDataCache builder is used a vehicle to transmit the resourceIndex that will be filled
        // below to all the ResPackage (to avoid yet another constructor parameter).
        var dataCacheBuilder = new ResPackageDataCacheBuilder( descriptorPackageCount,
                                                               _collector.LocalPackageCount,
                                                               appLocalPath != null,
                                                               resourceIndex );

       // Create the code package and adds it where it must be (at [1], after the null [0]).
        ResPackage codePackage = CreateCodePackage( dataCacheBuilder, generatedCodeContainer );
        bAll.Add( null! );
        bAll.Add( codePackage );
        packageIndex.Add( codePackage.FullName, codePackage );
        Throw.DebugAssert( codePackage.Resources.Index == 0 );
        allPackageResources[0] = codePackage.Resources;
        Throw.DebugAssert( codePackage.ResourcesAfter.Index == 1 );
        allPackageResources[1] = codePackage.ResourcesAfter;
        if( generatedCodeContainer != null )
        {
            Throw.DebugAssert( codePackage.ResourcesAfter.Resources == generatedCodeContainer );
            resourceIndex.Add( generatedCodeContainer, codePackage.ResourcesAfter );
        }

        // This is the common requirements of all ResPackage that have no requirement.
        ImmutableArray<ResPackage> requiresCode = [codePackage];
        // The Watch root is the longest common parent of all the ResPackage.LocalPath.
        // We compute it if and only if the LiveStatePath is not ResourceSpaceCollector.NoLiveState.
        string? watchRoot = null;

        var bAppRequirements = ImmutableArray.CreateBuilder<ResPackage>();
        foreach( var s in sortResult.SortedItems )
        {
            Throw.DebugAssert( s.IsGroup == (s.HeadForGroup != null) );
            if( s.HeadForGroup != null )
            {
                ResPackageDescriptor d = s.Item;
                // Close the CodeGen resources (if they are code generated).
                if( d.Resources is CodeGenResourceContainer c1 ) c1.Close();
                if( d.AfterResources is CodeGenResourceContainer c2 ) c2.Close();

                Throw.DebugAssert( "A child cannot be required and a requirement cannot be a child.",
                                   !s.Requires.Intersect( s.Children ).Any() );
                // Requirements and children have already been indexed.
                ImmutableArray<ResPackage> requires = s.Requires.Any()
                                                        ? s.Requires.Select( s => packageIndex[s.Item] ).ToImmutableArray()
                                                        : requiresCode;
                ImmutableArray<ResPackage> children = s.Children.Select( s => packageIndex[s.Item] ).ToImmutableArray();
                var p = new ResPackage( dataCacheBuilder,
                                        d.FullName,
                                        d.DefaultTargetPath,
                                        s.HeadForGroup.Index + 2,
                                        d.Resources,
                                        s.Index + 2,
                                        d.AfterResources,
                                        d.IsGroup,
                                        d.Type,
                                        requires,
                                        children,
                                        bAll.Count );
                // Index it.
                packageIndex.Add( p.FullName, p );
                if( p.Type != null )
                {
                    packageIndex.Add( p.Type, p );
                }
                resourceIndex.Add( p.Resources.Resources, p.Resources );
                resourceIndex.Add( p.ResourcesAfter.Resources, p.ResourcesAfter );
                // Enlist the package resources.
                allPackageResources[p.Resources.Index] = p.Resources;
                allPackageResources[p.ResourcesAfter.Index] = p.ResourcesAfter;
                // Track the watch root.
                var local = p.Resources.LocalPath ?? p.ResourcesAfter.LocalPath;
                if( local != null && _collector.LiveStatePath != ResourceSpaceCollector.NoLiveState )
                {
                    if( watchRoot == null )
                    {
                        watchRoot = Path.GetDirectoryName( local ) + Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        watchRoot = CommonParentPath( watchRoot, local );
                    }
                }
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
        var appPackage = CreateAppPackage( ref appLocalPath, dataCacheBuilder, bAppRequirements.DrainToImmutable(), bAll.Count );
        bAll.Add( appPackage );
        packageIndex.Add( appPackage.FullName, appPackage );
        Throw.DebugAssert( appPackage.Resources.Index == allPackageResources.Length - 2 );
        allPackageResources[^2] = appPackage.Resources;
        Throw.DebugAssert( appPackage.ResourcesAfter.Index == allPackageResources.Length - 1 );
        allPackageResources[^1] = appPackage.ResourcesAfter;
        if( appLocalPath != null )
        {
            // If we have no packages, watchRoot is null: we'll only
            // watch the "<App> folder.
            watchRoot ??= appLocalPath;
            resourceIndex.Add( appPackage.Resources.Resources, appPackage.Resources );
            bLocal.Add( appPackage );
        }
        Throw.DebugAssert( "Expected size reached.", packageIndex.Count == packageIndexSize );
        Throw.DebugAssert( "Expected size reached.", resourceIndex.Count == resourceIndexSize );
        var packages = bAll.MoveToImmutable();
        space._packages = packages;
        space._exposedPackages = new OneBasedArray( packages );
        space._localPackages = bLocal.MoveToImmutable();
        Throw.DebugAssert( allPackageResources.All( r => r != null ) );
        space._allPackageResources = ImmutableCollectionsMarshal.AsImmutableArray( allPackageResources );
        space._codePackage = codePackage;
        space._appPackage = appPackage;
        Throw.DebugAssert( _collector.LiveStatePath == ResourceSpaceCollector.NoLiveState
                           || (space._localPackages.Length != 0) == (watchRoot != null) );

        space._watchRoot = watchRoot;
        // The space is initialized with all its packages.
        // The ReachablePackageCacheBuilder has collected all the possible Reachable packages, we can now
        // compute the aggregation sets.
        space._resPackageDataCache = dataCacheBuilder.Build( monitor, packages );
        return space;
    }

    static string CommonParentPath( string path1, string path2 )
    {
        string[] p1 = path1.Split( Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries );
        string[] p2 = path2.Split( Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries );
        int len = p1.Length;
        if( len > p2.Length )
        {
            (p1, p2) = (p2, p1);
            len = p1.Length;
        }
        int iCommon = 0;
        while( iCommon < len && p1[iCommon] == p2[iCommon] ) ++iCommon;
        return iCommon == 0
                 ? string.Empty
                 : iCommon == len
                    ? path1
                    : string.Join( Path.DirectorySeparatorChar, p1.Take( iCommon ) ) + Path.DirectorySeparatorChar;
    }
}

