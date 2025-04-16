using CK.Setup;
using CK.TypeScript;
using System;

namespace CK.TS.Angular;

/// <summary>
/// Adds a provider registration to a <see cref="TypeScriptPackage"/>.
/// <para>
/// Required dependencies must be added with one or more <see cref="NgProviderImportAttribute"/>.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
public sealed class NgProviderAttribute : ContextBoundDelegationAttribute
{
    /// <summary>
    /// Initializes a new <see cref="NgProviderAttribute"/> with a single provider definition.
    /// </summary>
    /// <param name="providerCode">Provider definition code.</param>
    /// <param name="sourceNameSuffix">Optional source name suffix added to the C# name of the decorated <see cref="TypeScriptPackage"/> class.</param>
    public NgProviderAttribute( string providerCode, string? sourceNameSuffix = null )
        : base( "CK.TS.Angular.Engine.NgProviderAttributeImpl, CK.TS.Angular.Engine" )
    {
        ProviderCode = providerCode;
        SourceNameSuffix = sourceNameSuffix;
    }

    /// <summary>
    /// Gets the providers definition code.
    /// </summary>
    public string ProviderCode { get; }

    /// <summary>
    /// Gets an optional suffix added to the C# name of the decorated <see cref="TypeScriptPackage"/> class.
    /// This string identifies the provider registration and always starts with the C# name of the decorated <see cref="TypeScriptPackage"/> class.
    /// </summary>
    public string? SourceNameSuffix { get; }
}
