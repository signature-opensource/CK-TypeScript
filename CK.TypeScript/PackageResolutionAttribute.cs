using CK.Setup;
using System;

namespace CK.TypeScript;

/// <summary>
/// Redirects a package to a version for the whole application: an entry of the <c>resolutions</c> of the application's
/// <c>package.json</c> file.
/// <para>
/// A resolution redirects a dependency that a manifest declares. To add a dependency or a peer that a package doesn't
/// declare, use the <see cref="YarnPackageExtensionAttribute"/>. Both combine: a resolution also redirects a dependency
/// added by a package extension, never a peer (a peer is provided by the dependent package).
/// </para>
/// <para>
/// This attribute must decorate a <c>[TypeScriptPackage]</c> (or a specialization). The engine owns the resolutions that
/// are declared: it always writes the declared version, replacing the value of an existing entry; the other entries of
/// the <c>resolutions</c> are kept. When two packages declare different versions for the same pattern, the versions are
/// merged like the versions of the libraries (the more specific bound wins, non overlapping bounds are an error unless
/// <see cref="TypeScriptAspectConfiguration.IgnoreVersionsBound"/> is set); when a version is not a version range (a
/// protocol such as <c>npm:</c> or <c>patch:</c>, a tag), the last declaration in the topological order of the packages wins.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class PackageResolutionAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new PackageResolution attribute.
    /// </summary>
    /// <param name="pattern">
    /// The descriptor to redirect, as a key of the Yarn <c>resolutions</c>: a package name (<c>"@yarnpkg/core"</c>)
    /// or a narrower pattern (<c>"parent/package"</c>, <c>"package@npm:^7"</c>).
    /// </param>
    /// <param name="version">The version to use: an exact version, a range or a protocol (<c>npm:</c>, <c>patch:</c>...).</param>
    public PackageResolutionAttribute( string pattern, string version )
        : base( "CK.TypeScript.Engine.PackageResolutionAttributeImpl, CK.TypeScript.Engine" )
    {
        Pattern = pattern;
        Version = version;
    }

    /// <summary>
    /// Gets the redirected descriptor pattern.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the version to use.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets or sets an optional reason. The <c>package.json</c> has no comments: it is logged with the entry.
    /// </summary>
    public string? Reason { get; set; }
}
