using CK.Setup;
using System;

namespace CK.TypeScript;

/// <summary>
/// Decorates any class (that can even be static) to specify a library that will be
/// included in the /ck-gen/package.json file.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class TypeScriptImportLibraryAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new TypeScriptImportLibrary attribute.
    /// </summary>
    /// <param name="name">The library name to import.</param>
    /// <param name="versionBound">
    /// The version bound. Can be:
    /// <list type="bullet">
    ///     <item><c>null</c>: the library MUST be configured in the <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.</item>
    ///     <item><c>"*"</c>, <c>""</c> or <c>"&gt;=0.0.0-0"</c>: any version can be used. If no other bounds are specified, the library will be installed with the latest npm version.</item>
    ///     <item>A regular npm version bound like "^0.1.2", "~0.4", ">=7", etc.</item>
    /// </list>
    /// </param>
    /// <param name="dependencyKind">
    /// The kind of dependency. Conflicts are resolved with a simple rule:
    /// PeerDependency &gt; Dependency &gt; DevDependency.
    /// </param>
    public TypeScriptImportLibraryAttribute( string name, string? versionBound, DependencyKind dependencyKind )
        : base( "CK.TypeScript.Engine.TypeScriptImportLibraryAttributeImpl, CK.TypeScript.Engine" )
    {
        Name = name;
        Version = versionBound;
        DependencyKind = dependencyKind;
    }

    /// <summary>
    /// Gets the name of the package name, which will be the string put in the package.json.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the package, which will be used in the package.json.
    /// This bound will be ignored if this library is configured. See <see cref="TypeScriptAspectConfiguration.LibraryVersions"/>.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Dependency kind of the package. Will be used to determine in which list
    /// of the packgage.json the dependency should appear.
    /// </summary>
    public DependencyKind DependencyKind { get; }
}
