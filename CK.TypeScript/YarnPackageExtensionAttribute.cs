using CK.Setup;
using System;

namespace CK.TypeScript;

/// <summary>
/// Adds a dependency or a peer dependency to the manifest of a third-party package: an entry of the
/// <c>packageExtensions</c> of the application's <c>.yarnrc.yml</c> file.
/// <para>
/// A package extension adds an edge that a package doesn't declare: for instance, ng-zorro-antd uses rxjs (through
/// @angular/cdk) but declares it neither as a dependency nor as a peer, and without the peer, Yarn Plug'n'Play creates
/// a second instance of its CDK. To redirect a dependency that a package declares to another version, use the
/// <see cref="PackageResolutionAttribute"/>. Both combine: a resolution also redirects a dependency added by a package
/// extension, never a peer (a peer is provided by the dependent package).
/// </para>
/// <para>
/// This attribute must decorate a <c>[TypeScriptPackage]</c> (or a specialization). When two packages declare different
/// ranges for the same selector, kind and name, the versions are merged like the versions of the libraries (the more
/// specific range wins, non overlapping ranges are an error unless <see cref="TypeScriptAspectConfiguration.IgnoreVersionsBound"/>
/// is set); when a range is not a version range (a protocol, a tag), the last declaration in the topological order of the
/// packages wins.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class YarnPackageExtensionAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new YarnPackageExtension attribute.
    /// </summary>
    /// <param name="selector">
    /// The package whose manifest is extended, as a descriptor with a range: <c>"ng-zorro-antd@*"</c>.
    /// The range is required (a bare package name is an invalid descriptor for Yarn) and a range that matches no installed
    /// version is silently ignored by Yarn: <c>@*</c> should be used unless a later version of the package fixes its manifest.
    /// </param>
    /// <param name="kind">
    /// The kind of the added edge: <see cref="DependencyKind.Dependency"/> (<c>dependencies</c>) or
    /// <see cref="DependencyKind.PeerDependency"/> (<c>peerDependencies</c> only: unlike the
    /// <see cref="TypeScriptImportLibraryAttribute"/>, no dev dependency is added).
    /// <see cref="DependencyKind.DevDependency"/> is an error: Yarn has no dev dependencies in a package extension
    /// (the dev dependencies of an installed package are never installed).
    /// </param>
    /// <param name="name">The name of the added package: <c>"rxjs"</c>.</param>
    /// <param name="range">The range of the added package: <c>"*"</c> for a peer that forwards the application's version.</param>
    public YarnPackageExtensionAttribute( string selector, DependencyKind kind, string name, string range )
        : base( "CK.TypeScript.Engine.YarnPackageExtensionAttributeImpl, CK.TypeScript.Engine" )
    {
        Selector = selector;
        Kind = kind;
        Name = name;
        Range = range;
    }

    /// <summary>
    /// Gets the descriptor of the extended package (<c>"ng-zorro-antd@*"</c>).
    /// </summary>
    public string Selector { get; }

    /// <summary>
    /// Gets the kind of the added edge.
    /// </summary>
    public DependencyKind Kind { get; }

    /// <summary>
    /// Gets the name of the added package.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the range of the added package.
    /// </summary>
    public string Range { get; }

    /// <summary>
    /// Gets or sets an optional reason. It is written in the comment that precedes the entry in the <c>.yarnrc.yml</c>
    /// file, after the name of the decorated type.
    /// </summary>
    public string? Reason { get; set; }
}
