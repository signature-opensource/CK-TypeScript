using CK.Setup;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Imports anything required by a <see cref="NgProviderAttribute"/>.
/// <para>
/// Symbols can be aliased. Note that "UserInfo as Info, User, UserInfo" is valid: the "UserInfo"
/// is available as "Info" but also as "UserInfo".
/// </para>
/// <para>
/// To import the default export from the module, prefix the default name with "default " (order doesn't matter):
/// <code>
/// [NgProviderImport( "AxiosInstance, default axios", LibraryName = "axios" )]
/// </code>
/// Generates the following import:
/// <code>
/// import axios, { AxiosInstance } from 'axios';
/// </code>
/// </para>
/// <para>
/// The default exported name must always be the same: if it has alredy been defined to a different name,
/// an <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class NgProviderImportAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgProviderAttribute"/>.
    /// </summary>
    /// <param name="symbolNames">The type or variable names to import.</param>
    public NgProviderImportAttribute( string symbolNames )
        : base( "CK.TS.Angular.Engine.NgProviderImportAttributeImpl, CK.TS.Angular.Engine" )
    {
        LibraryName = "@local/ck-gen";
        SymbolNames = symbolNames;
    }

    /// <summary>
    /// Gets or sets the library name (like "axios").
    /// Defaults to "@local/ck-gen".
    /// </summary>
    public string LibraryName { get; set; }

    /// <summary>
    /// Gets the type or variable names to import.
    /// </summary>
    public string SymbolNames { get; }

}
