using CK.StObj.TypeScript;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Required decoration of <see cref="NgProvider"/>.
/// </summary>
/// <typeparam name="T">The TypeScriptPackage to which this provider belongs.</typeparam>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public sealed class NgProviderAttribute<T> : NgProviderAttribute where T : TypeScriptPackage
{
    /// <summary>
    /// Initializes a new <see cref="NgProviderAttribute{T}"/> with a single provider definition.
    /// </summary>
    /// <param name="code">Provider definition code.</param>
    /// <param name="sourceName">Optional source name suffix added to the C# name of the decorated <see cref="NgProvider"/> class.</param>
    public NgProviderAttribute( string code, string? sourceName = null )
        : base( typeof( T ), [(code, sourceName)] )
    {
    }

    /// <summary>
    /// Initializes a new <see cref="NgProviderAttribute{T}"/> with one or more provider definitions.
    /// </summary>
    /// <param name="providerDefinitions">See <see cref="NgProviderAttribute{T}(string,string)"/>.</param>
    public NgProviderAttribute( params (string Code, string? SourceName)[] providerDefinitions )
        : base( typeof( T ), providerDefinitions )
    {
    }
}
