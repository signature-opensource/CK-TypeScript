using CK.Setup;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Ensures that <c>@import "<see cref="ImportPath"/>";</c> appears in the application <c>src/styles.less</c>
/// file.
/// <para>
/// Note that since this file is an application file, we limit our modifications to it: this is applied
/// only if the <c>@import "<see cref="ImportPath"/>";</c> doesn't already appear. Once added, it is left as-is. 
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class NgAppStyleImportAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgAppStyleImportAttribute"/>.
    /// </summary>
    /// <param name="importPath">The path to import.</param>
    public NgAppStyleImportAttribute( string importPath )
        : base( "CK.TS.Angular.Engine.NgAppStyleImportAttributeImpl, CK.TS.Angular.Engine" )
    {
        ImportPath = importPath;
    }

    /// <summary>
    /// Gets the path to import.
    /// </summary>
    public string ImportPath { get; }

    /// <summary>
    /// Gets or sets whether the <c>@import '<see cref="ImportPath"/>';</c> must appear after any import
    /// from [NgAppStyleImport] on children packages.
    /// <para>
    /// This is false by default: this import appears before any children's imports.
    /// </para>
    /// </summary>
    public bool AfterContent { get; set; }

}
