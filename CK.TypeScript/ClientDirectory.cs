using CK.Core;
using System.Collections.Generic;

namespace CK.TypeScript;

/// <summary>
/// The client applications of this host: its TypeScript configurations whose <see cref="CK.Setup.TypeScriptBinPathAspectConfiguration.Kind"/>
/// is not <see cref="CK.Setup.ClientKind.None"/>. This is generated from the configuration: it is empty when the host has no
/// client application.
/// </summary>
[CK.Setup.ContextBoundDelegation( "CK.Setup.ClientDirectoryImpl, CK.TypeScript.Engine" )]
public abstract class ClientDirectory : ISingletonAutoService
{
    /// <summary>
    /// Infrastructure code not intended to be called by user code.
    /// </summary>
    /// <param name="clients">The client applications.</param>
    protected ClientDirectory( IReadOnlyList<ClientInfo> clients )
    {
        Clients = clients;
    }

    /// <summary>
    /// Gets the client applications. The default one (served at "/"), if any, is the last one.
    /// </summary>
    public IReadOnlyList<ClientInfo> Clients { get; }
}
