using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.TypeScript.LiveEngine;


static class StateSerializer
{
    public static int CurrentVersion = 0;

    internal static void WriteResourceContainer( CKBinaryWriter w,
                                                 CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                                 IResourceContainer container,
                                                 CKBinaryWriter.ObjectPool<AssemblyResourceContainer>? assemblyPool )
    {
        Throw.DebugAssert( container is EmptyResourceContainer or AssemblyResourceContainer );
        if( containerPool.MustWrite( container ) )
        {
            w.Write( container.DisplayName );
            w.Write( container.ResourcePrefix );
            if( assemblyPool != null )
            {
                var a = container as AssemblyResourceContainer;
                if( assemblyPool.MustWrite( a ) )
                {
                    Throw.DebugAssert( a != null );
                    w.Write( a.AssemblyResources.AssemblyName );
                }
            }
        }
    }

    internal static IResourceContainer ReadResourceContainer( CKBinaryReader r,
                                                              CKBinaryReader.ObjectPool<IResourceContainer> containerPool,
                                                              CKBinaryReader.ObjectPool<AssemblyResourceContainer>? assemblyPool )
    {
        var state = containerPool.TryRead( out var result );
        if( !state.Success )
        {
            var displayName = r.ReadString();
            var resourcePrefix = r.ReadString();
            if( assemblyPool == null )
            {
                result = state.SetReadResult( new FileSystemResourceContainer( resourcePrefix, displayName ) );
            }
            else
            {
                var assemblyState = assemblyPool.TryRead( out var assemblyResult );
                if( assemblyState.Success )
                {
                    Throw.DebugAssert( assemblyResult != null );
                    result = state.SetReadResult( assemblyResult );
                }
                else
                {
                    var a = Assembly.Load( r.ReadString() );
                    assemblyResult = a.GetResources().CreateCKResourceContainer( resourcePrefix, displayName );
                    assemblyState.SetReadResult( assemblyResult );
                    result = state.SetReadResult( assemblyResult );
                }
            }
        }
        Throw.DebugAssert( "We never serialize a null container.", result != null );
        return result;
    }

    internal static void WriteResourceLocator( CKBinaryWriter w,
                                               CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                               ResourceLocator locator,
                                               CKBinaryWriter.ObjectPool<AssemblyResourceContainer>? assemblyPool )
    {
        Throw.DebugAssert( locator.IsValid );
        WriteResourceContainer( w, containerPool, locator.Container, assemblyPool );
        w.Write( locator.ResourceName );
    }

    internal static ResourceLocator ReadResourceLocator( CKBinaryReader r,
                                                         CKBinaryReader.ObjectPool<IResourceContainer> containerPool,
                                                         CKBinaryReader.ObjectPool<AssemblyResourceContainer>? assemblyPool )
    {
        var c = ReadResourceContainer( r, containerPool, assemblyPool );
        return new ResourceLocator( c, r.ReadString() );
    }


    internal static void WriteLiveState( CKBinaryWriter w,
                                         LiveStatePathContext pathContext,
                                         string watchRoot,
                                         IReadOnlySet<NormalizedCultureInfo> activeCultures,
                                         List<RegularPackageRef> regularPackages,
                                         List<LocalPackageRef> localPackages,
                                         AssetsBuilder assets )
    {
        w.Write( CurrentVersion );
        w.Write( pathContext.TargetProjectPath );
        w.Write( watchRoot );
        w.WriteNonNegativeSmallInt32( activeCultures.Count );
        foreach( var culture in activeCultures )
        {
            w.Write( culture.Name );
        }
        // We take no risk here by using the pools to write resource containers instead
        // of explicitly handling the provided container arrays directly.
        var containerPool = new CKBinaryWriter.ObjectPool<IResourceContainer>( w );
        w.WriteNonNegativeSmallInt32( localPackages.Count );
        foreach( var p in localPackages )
        {
            // We write EmptyResourceContainer: this is enough to restore
            // FileSystemResourceContainer when reading.
            WriteResourceContainer( w, containerPool, p.Resources, null );  
            w.Write( p.TypeScriptFolder.Path );
        }
        var assemblyPool = new CKBinaryWriter.ObjectPool<AssemblyResourceContainer>( w );
        w.WriteNonNegativeSmallInt32( regularPackages.Count );
        foreach( var p in regularPackages )
        {
            WriteResourceContainer( w, containerPool, p.Resources, assemblyPool );
            w.Write( p.TypeScriptFolder.Path );
        }
        // Writes the assets.
        assets.WriteState( w, containerPool, assemblyPool );
    }

    internal static LiveState? ReadLiveState( IActivityMonitor monitor,
                                              CKBinaryReader r,
                                              LiveStatePathContext pathContext )
    {
        int v = r.ReadInt32();
        if( v != CurrentVersion )
        {
            monitor.Error( $"Invalid version '{v}', expected '{CurrentVersion}'." );
            return null;
        }
        var targetProjectPath = r.ReadString();
        if( pathContext.TargetProjectPath != targetProjectPath )
        {
            monitor.Error( $"Invalid paths. Expected '{pathContext.TargetProjectPath}', got '{targetProjectPath}'." );
            return null;
        }
        var watchRoot = r.ReadString();
        var activeCultures = new HashSet<NormalizedCultureInfo>();
        int count = r.ReadNonNegativeSmallInt32();
        while( count-- > 0 )
        {
            activeCultures.Add( NormalizedCultureInfo.EnsureNormalizedCultureInfo( r.ReadString() ) );
        }
        // Reads the LocalPackages. They have been written as EmptyResourceContainer + TypeScriptFolder.
        // We read them back as FileSystemResourceContainer + TypeScriptFolder.
        var containerPool = new CKBinaryReader.ObjectPool<IResourceContainer>( r );
        ImmutableArray<LocalPackage> localPackages;
        count = r.ReadNonNegativeSmallInt32();
        if( count == 0 )
        {
            localPackages = ImmutableArray<LocalPackage>.Empty;
        }
        else
        {
            var b = ImmutableArray.CreateBuilder<LocalPackage>( count );
            while( count-- > 0 )
            {
                var resources = ReadResourceContainer( r, containerPool, null );
                Throw.DebugAssert( resources is FileSystemResourceContainer );
                b.Add( new LocalPackage( Unsafe.As<FileSystemResourceContainer>( resources ), r.ReadString() ) );
            }
            localPackages = b.MoveToImmutable();
        }
        // Now reads the regular packages.
        var assemblyPool = new CKBinaryReader.ObjectPool<AssemblyResourceContainer>( r );
        ImmutableArray<RegularPackage> regularPackages;
        count = r.ReadNonNegativeSmallInt32();
        if( count == 0 )
        {
            regularPackages = ImmutableArray<RegularPackage>.Empty;
        }
        else
        {
            var b = ImmutableArray.CreateBuilder<RegularPackage>( count );
            while( count-- > 0 )
            {
                var resources = ReadResourceContainer( r, containerPool, assemblyPool );
                Throw.DebugAssert( resources is AssemblyResourceContainer );
                b.Add( new RegularPackage( Unsafe.As<AssemblyResourceContainer>( resources ), r.ReadString() ) );
            }
            regularPackages = b.MoveToImmutable();
        }

        var result = new LiveState( pathContext, watchRoot, activeCultures, localPackages, regularPackages );
        if( !result.LoadExtensions( monitor, r, containerPool, assemblyPool ) )
        {
            result = null;
        }
        return result;
    }

    internal static bool WriteFile( IActivityMonitor monitor, NormalizedPath filePath, Action<IActivityMonitor,CKBinaryWriter> write )
    {
        try
        {
            using( var file = File.Create( filePath ) )
            using( var w = new CKBinaryWriter( file ) )
            {
                write( monitor, w );
            }
            return true;
        }
        catch( Exception e )
        {
            monitor.Error( e );
            return false;
        }
    }

    internal static T? ReadFile<T>( IActivityMonitor monitor, NormalizedPath filePath, Func<IActivityMonitor,CKBinaryReader,T?> read ) where T : class
    {
        try
        {
            if( !File.Exists( filePath ) )
            {
                monitor.Error( $"Missing '{filePath}' file." );
                return null;
            }
            using( var file = File.OpenRead( filePath ) )
            using( var r = new CKBinaryReader( file ) )
            {
                return read( monitor, r );
            }
        }
        catch( Exception e )
        {
            monitor.Error( e );
            return null;
        }
    }

}
