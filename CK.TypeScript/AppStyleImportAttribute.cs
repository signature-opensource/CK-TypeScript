using CK.Setup;
using System;

namespace CK.TypeScript;

/// <summary>
/// Ensures that <c>@import "<see cref="ImportPath"/>";</c> appears in the application <c>src/styles.less</c>
/// file.
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class AppStyleImportAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="AppStyleImportAttribute"/>.
    /// </summary>
    /// <param name="importPath">The path to import.</param>
    public AppStyleImportAttribute( string importPath )
        : base( "CK.TypeScript.Engine.AppStyleImportAttributeImpl, CK.TypeScript.Engine" )
    {
        ImportPath = importPath;
    }

    /// <summary>
    /// Gets the path to import.
    /// </summary>
    public string ImportPath { get; }

    /// <summary>
    /// Gets or sets whether the <c>@import '<see cref="ImportPath"/>';</c> must appear after any import
    /// from [AppStyleImport] on children packages.
    /// <para>
    /// This is false by default: this import appears before any children's imports.
    /// </para>
    /// </summary>
    public bool AfterContent { get; set; }

}
