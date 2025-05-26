using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Default <see cref="IResPackageDescriptorResolver"/> implementation.
/// <para>
/// Only allows interfaces to be abstractions and doesn't any selection rule.
/// </para>
/// </summary>
public sealed class DefaultResPackageDescriptorResolver : IResPackageDescriptorResolver
{
    /// <summary>
    /// Simple implementation:
    /// <list type="number">
    ///     <item>First tries to find the exact <paramref name="targetType"/> in the <paramref name="packageIndex"/>.</item>
    ///     <item>If not found, allows only interfaces to be abstractions: assignable types are searched.</item>
    ///     <item>Unresolved type is not an error but a warning (and <paramref name="result"/> is null).</item>
    /// </list>
    /// When wore than one satisfying type is found, this is an error.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="packageIndex">The <see cref="ResSpaceCollector.PackageIndex"/>.</param>
    /// <param name="relName">The relationship name ("Package").</param>
    /// <param name="targetType">The target type to resolve.</param>
    /// <param name="decoratedType">The type that declares the relationships.</param>
    /// <param name="result">The resolved package if any.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    public bool TryFindSinglePackageDescriptorByType( IActivityMonitor monitor,
                                                      IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex,
                                                      string relName,
                                                      Type targetType,
                                                      Type decoratedType,
                                                      out ResPackageDescriptor? result )
    {
        if( packageIndex.TryGetValue( targetType, out result ) )
        {
            return true;
        }
        if( !targetType.IsInterface )
        {
            monitor.Warn( $"""
                [{relName}<{targetType:N}>] on type '{decoratedType:N}' skipped. Type is not registered
                and '{targetType:C}' is not an interface.
                """ );
            return true;
        }
        var candidates = packageIndex.Keys
                                     .OfType<Type>()
                                     .Where( targetType.IsAssignableFrom )
                                     .ToArray();
        if( candidates.Length == 1 )
        {
            result = packageIndex[candidates[0]];
            return true;
        }
        if( candidates.Length > 1 )
        {
            monitor.Error( $"""
                    [{relName}<{targetType:N}>] on type '{decoratedType:N}' skipped: more than one registered types satisfy '{targetType:C}':
                    {candidates.Select( t => t.ToCSharpName() ).Concatenate()}'.
                    """ );
            return false;
        }
        monitor.Warn( $"""
                    [{relName}<{targetType:N}>] on type '{decoratedType:N}' skipped: unable to find a type registered
                    in this ResourceSpace that is a '{targetType:C}'.
                    """ );
        return true;
    }
    /// <summary>
    /// Simple implementation: if the exact <paramref name="targetType"/> is not found in <paramref name="packageIndex"/>
    /// and the target type is an interface, any registered type assignable to it are collected.
    /// <para>
    /// When no types are resolved, this not an error, just a warning.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="packageIndex">The <see cref="ResSpaceCollector.PackageIndex"/>.</param>
    /// <param name="relName">The relationship name.</param>
    /// <param name="targetType">The target type to resolve.</param>
    /// <param name="decoratedType">The type that declares the relationships.</param>
    /// <param name="collector">The result collector.</param>
    /// <returns>True on success, false on error. Errors must be logged.</returns>
    public bool TryFindMultiplePackageDescriptorByType( IActivityMonitor monitor,
                                                        IReadOnlyDictionary<object, ResPackageDescriptor> packageIndex,
                                                        string relName,
                                                        Type targetType,
                                                        Type decoratedType,
                                                        Action<ResPackageDescriptor> collector )
    {
        if( packageIndex.TryGetValue( targetType, out var trivialTesult ) )
        {
            collector( trivialTesult );
            return true;
        }
        if( !targetType.IsInterface )
        {
            monitor.Warn( $"""
                [{relName}<{targetType:N}>] on type '{decoratedType:N}' skipped. Type is not registered
                and '{targetType:C}' is not an interface.
                """ );
            return true;
        }
        var candidates = packageIndex.Keys
                                 .OfType<Type>()
                                 .Where( targetType.IsAssignableFrom );
        int count = 0;
        foreach( var t in candidates )
        {
            collector( packageIndex[t] );
            count++;
        }
        if( count == 0 )
        {
            monitor.Warn( $"""
                            [{relName}<{targetType:N}>] on type '{decoratedType:N}' skipped: unable to find a a type registered
                            in this ResourceSpace that is a '{targetType:C}'.
                            """ );
        }
        return true;
    }
}

