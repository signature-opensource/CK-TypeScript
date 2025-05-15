using CK.Setup;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Ensures that <c>@import "<see cref="Path"/>";</c> appears in the application <c>src/styles.less</c>
/// file.
/// <para>
/// Note that since this file is an application file, we limit our modifications to it: this is applied
/// only if the <c>@import "<see cref="Path"/>";</c> doesn't already appear. Once added, it is left as-is. 
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class NgAppStyleImportAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgAppStyleImportAttribute"/>.
    /// </summary>
    /// <param name="path">The path to import.</param>
    public NgAppStyleImportAttribute( string path )
        : base( "CK.TS.Angular.Engine.NgGlobalStyleImportAttributeImpl, CK.TS.Angular.Engine" )
    {
        Path = path;
    }

    /// <summary>
    /// Gets the path to import.
    /// </summary>
    public string Path { get; }

}
