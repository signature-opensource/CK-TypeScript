using System;
using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// The TypeScript packages selected for each TypeScript configuration of a BinPath group (by its name), published by the
/// <see cref="TypeScriptAspect"/> once its contexts are prepared: the <see cref="ClientDirectoryImpl"/> waits for it to
/// compute the facts of the client applications.
/// </summary>
sealed class TypeScriptPackageSelection
{
    readonly Dictionary<string, IReadOnlyList<Type>> _packages;

    public TypeScriptPackageSelection( Dictionary<string, IReadOnlyList<Type>> packages )
    {
        _packages = packages;
    }

    /// <summary>
    /// Gets the types of the selected TypeScript packages (and groups) of a configuration. Empty when it is unknown.
    /// </summary>
    /// <param name="configurationName">The TypeScript configuration name.</param>
    /// <returns>The selected package types.</returns>
    public IReadOnlyList<Type> GetPackages( string configurationName ) => _packages.GetValueOrDefault( configurationName ) ?? [];
}
