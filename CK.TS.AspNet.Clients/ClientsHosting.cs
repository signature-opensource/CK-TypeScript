using CK.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CK.TypeScript;

/// <summary>
/// Registers what <see cref="ClientsApplicationExtensions.MapClients"/> requires (the HTTP forwarder that reaches the
/// dev servers): the hosts don't call <c>ReverseProxyServiceCollectionExtensions.AddHttpForwarder()</c>.
/// </summary>
public sealed class ClientsHosting : IRealObject
{
    void ConfigureServices( StObjContextRoot.ServiceRegister services )
    {
        services.Services.AddHttpForwarder();
    }
}
