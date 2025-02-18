using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace CK.TypeScript.LiveEngine;


static class StateSerializer
{
    public static int CurrentVersion = 0;

    internal static void WriteResourceContainer( CKBinaryWriter w,
                                                 CKBinaryWriter.ObjectPool<IResourceContainer> containerPool,
                                                 IResourceContainer container,
                                                 CKBinaryWriter.ObjectPool<AssemblyResourceContainer>? assemblyPool )
    {
        Throw.DebugAssert( container != null );
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
                result = state.SetReadResult( new EmptyResourceContainer( displayName, resourcePrefix ) );
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
                                         List<LocalPackageRef> localPackages )
    {
        w.Write( CurrentVersion );
        w.Write( pathContext.TargetProjectPath );
        w.Write( watchRoot );
        w.WriteNonNegativeSmallInt32( activeCultures.Count );
        foreach( var culture in activeCultures )
        {
            w.Write( culture.Name );
        }
        w.WriteNonNegativeSmallInt32( localPackages.Count );
        foreach( var p in localPackages )
        {
            w.Write( p.LocalResPath );
            w.Write( p.DisplayName );
        }
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
        ImmutableArray<LocalPackage> localPackages = default;
        count = r.ReadNonNegativeSmallInt32();
        if( count > 0 )
        {
            var b = ImmutableArray.CreateBuilder<LocalPackage>( count );
            while( count-- > 0 )
            {
                b.Add( new LocalPackage( r.ReadString(), r.ReadString() ) );
            }
            localPackages = b.MoveToImmutable();
        }
        return new LiveState( pathContext, watchRoot, activeCultures, localPackages );
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
