using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.EmbeddedResources;

/// <summary>
/// A CodeStoreResources is composed of 2 containers: a mutable <see cref="Code"/> container
/// in which new code generated resources can be added and an immutable <see cref="Store"/>
/// container that contains statically defined resources.
/// <para>
/// The notion of "final" resource is meaningless at this level: these resources must be analyzed, combined
/// in any way by resource handlers. What matters here is that Code generation is able to generate such "resources"
/// until <see cref="CodeGenResourceContainer.Close()"/> is called. 
/// </para>
/// <para>
/// Code is able to read the resources from the Store before adding Code resources: it follows that, when applicable,
/// Code resources must be considered as a potential "override" of the resources in the Store and this is why
/// <see cref="GetSingleResource(IActivityMonitor, ReadOnlySpan{char})"/> and <see cref="GetSingleFolder(IActivityMonitor, ReadOnlySpan{char})"/>
/// are supported but how resources are "combined" together (and even whether thay are "combinable") is not known here.
/// </para>
/// </summary>
/// <remarks>
/// This class is not directly serializable: the <see cref="CodeStoreResources(IResourceContainer,IResourceContainer)"/> constructor
/// must be called with deserialized containers.
/// </remarks>
[SerializationVersion( 0 )]
public sealed class CodeStoreResources
{
    readonly IResourceContainer _code;
    readonly IResourceContainer _store;

    /// <summary>
    /// Initializes a new <see cref="CodeStoreResources"/> with an explicit Code and Store container.
    /// Both of them are required but both of them may be a <see cref="EmptyResourceContainer"/>.
    /// They cannot be the same instance except if they are both EmptyResourceContainer.
    /// </summary>
    /// <param name="code">The code container. <see cref="IResourceContainer.IsValid"/> must be true.</param>
    /// <param name="store">
    /// The store container.
    /// <see cref="IResourceContainer.IsValid"/> must be true.
    /// It must not be a <see cref="CodeGenResourceContainer"/>.
    /// </param>
    public CodeStoreResources( IResourceContainer code, IResourceContainer store )
    {
        Throw.CheckArgument( code != null && code.IsValid );
        Throw.CheckArgument( store != null && store.IsValid && store is not CodeGenResourceContainer );
        Throw.CheckArgument( code != store || (code is EmptyResourceContainer && store is EmptyResourceContainer) );
        _code = code;
        _store = store;
    }

    /// <summary>
    /// Initializes a new <see cref="CodeStoreResources"/> with a Store container and an automatically
    /// created <see cref="CodeGenResourceContainer"/> for <see cref="Code"/> with a <see cref="IResourceContainer.DisplayName"/>
    /// derived from the store display name.
    /// </summary>
    /// <param name="store">The store container. It must not be a <see cref="CodeGenResourceContainer"/>.</param>
    public CodeStoreResources( IResourceContainer store )
    {
        Throw.CheckNotNullArgument( store );
        Throw.CheckArgument( store is not CodeGenResourceContainer );
        _store = store;
        var n = store is EmptyResourceContainer e ? e.NonDisabledDisplayName : store.DisplayName;
        _code = new CodeGenResourceContainer( $"[CodeGen] {n}" );
    }

    /// <summary>
    /// Gets a code generated resource container: this container is deemed to hold
    /// resources generated by code.
    /// <para>
    /// This container's content is considered mutable. It is often a writable container
    /// like a <see cref="CodeGenResourceContainer"/> or a <see cref="FileSystemResourceContainer"/>
    /// but it may also be an immutable <see cref="EmptyResourceContainer"/>: there is no constraint at this level.
    /// </para>
    /// </summary>
    public IResourceContainer Code => _code;

    /// <summary>
    /// Gets a resource container that holds stored resources. It is considered immutable. It is typically
    /// a <see cref="AssemblyResourceContainer"/> but it can be a mutable one like a <see cref="FileSystemResourceContainer"/>
    /// and in this case, its content should not be modified.
    /// <para>
    /// This can never be a <see cref="CodeGenResourceContainer"/>.
    /// </para>
    /// </summary>
    public IResourceContainer Store => _store;

    /// <summary>
    /// Tries to get a resource from <see cref="Code"/> if it exists and from <see cref="Store"/> otherwise.
    /// <para>
    /// If Code resource exists and overwrites the Store's one, a <see cref="LogLevel.Trace"/> is emitted: this is
    /// not a warning as the code should have been handled the Store resource when generating its replacement.
    /// </para>
    /// <para>
    /// If the resource doesn't exist, <see cref="ResourceLocator.IsValid"/> is false.
    /// </para>
    /// </summary>
    /// <param name="resourceName">
    /// The resource name (without their <see cref="IResourceContainer.ResourcePrefix"/>).
    /// Can contain any sub folder prefix.
    /// </param>
    /// <returns>The resource locator that may not be valid.</returns>
    public ResourceLocator GetSingleResource( IActivityMonitor monitor, ReadOnlySpan<char> resourceName )
    {
        var r = _code.GetResource( resourceName );
        if( r.IsValid )
        {
            var fromStore = _store.GetResource( resourceName );
            if( fromStore.IsValid )
            {
                monitor.Trace( $"Resource {r} overrides {fromStore}." );
            }
            return r;
        }
        return _store.GetResource( resourceName );
    }

    /// <summary>
    /// Tries to get a folder from <see cref="Code"/> if it exists and from <see cref="Store"/> otherwise.
    /// <para>
    /// If the resource doesn't exist, <see cref="ResourceLocator.IsValid"/> is false.
    /// </para>
    /// </summary>
    /// <param name="folderName">
    /// The resource folder name (without the <see cref="IResourceContainer.ResourcePrefix"/>).
    /// Can contain any sub folder prefix.
    /// </param>
    /// <returns>The resource folder that may not be valid.</returns>
    public ResourceFolder GetSingleFolder( IActivityMonitor monitor, ReadOnlySpan<char> folderName )
    {
        var r = _code.GetFolder( folderName );
        if( r.IsValid )
        {
            var fromStore = _store.GetFolder( folderName );
            if( fromStore.IsValid )
            {
                monitor.Trace( $"Resource {r} overrides {fromStore}." );
            }
            return r;
        }
        return _store.GetFolder( folderName );
    }

    /// <summary>
    /// Overridden to return <see cref="Store"/> and <see cref="Code"/> display names.
    /// </summary>
    /// <returns>The Code and Store display names.</returns>
    public override string ToString() => $"{Store} and {Code}";

}
