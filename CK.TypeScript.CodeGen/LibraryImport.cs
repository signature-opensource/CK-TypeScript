using CSemVer;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.CodeGen;

/// <summary>
/// Represent an external library that the generated code depends on.
/// <para>
/// LibraryImport are keyed by <see cref="Name"/> in <see cref="LibraryManager"/>. The <see cref="Version"/>
/// is fixed by configuration xor is the minimal version bound of the code provided versions under the
/// control of <see cref="LibraryManager.IgnoreVersionsBound"/>.
/// </para>
/// </summary>
public sealed class LibraryImport
{
    readonly PackageDependency _packageDependency;
    IReadOnlyCollection<LibraryImport> _impliedDependencies;
    bool _isUsed;

    internal LibraryImport( PackageDependency dependency,
                            IReadOnlyCollection<LibraryImport> impliedDependencies )
    {
        _packageDependency = dependency;
        _impliedDependencies = impliedDependencies;
    }

    /// <summary>
    /// Name of the package name, which will be the string put in the package.json.
    /// </summary>
    public string Name => _packageDependency.Name;

    /// <summary>
    /// Version of the package, which will be used in the package.json.
    /// This version can be settled by the configuration. See <see cref="LibraryManager.LibraryVersionConfiguration"/>.
    /// </summary>
    public SVersionBound Version => _packageDependency.Version;

    /// <summary>
    /// Dependency kind of the package. Will be used to determine in which list
    /// of the packgage.json the dependency should appear.
    /// </summary>
    public DependencyKind DependencyKind => _packageDependency.DependencyKind;

    /// <summary>
    /// Gets a set of dependencies that must be available whenever this one is.
    /// </summary>
    public IReadOnlyCollection<LibraryImport> ImpliedDependencies => _impliedDependencies;

    /// <summary>
    /// Gets or sets whether this library is actually used: <see cref="ITSFileImportSection.EnsureImportFromLibrary(LibraryImport, string, string[])"/>
    /// sets it to true.
    /// <para>
    /// This trims the libraries: a library can be registered but none of its types
    /// may be used. When a library is not used, it shouldn't appear in the package.json.
    /// For some libraries (typically <see cref="DependencyKind.DevDependency"/>), this can be set to true directly. Note that once
    /// set to true, it never transition back to false.
    /// </para>
    /// </summary>
    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            if( !_isUsed && value )
            {
                _isUsed = true;
                foreach( var d in ImpliedDependencies )
                {
                    d.IsUsed = true;
                }
            }
        }
    }

    internal PackageDependency PackageDependency => _packageDependency;

    internal void Update( IEnumerable<LibraryImport> implied )
    {
        if( implied.Any() )
        {
            _impliedDependencies = _impliedDependencies.Concat( implied ).Distinct().ToArray();
        }
    }
}
