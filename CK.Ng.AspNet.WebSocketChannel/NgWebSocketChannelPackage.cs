using CK.AspNet.WebSocketChannel;
using CK.Core;
using CK.TS.Angular;
using CK.TypeScript;

namespace CK.Ng.AspNet.WebSocketChannel;

/// <summary>
/// Provides the one <c>WSConnection</c> of the application and opens it during the bootstrap, so that
/// the features that share the channel find it ready.
/// <para>
/// A feature that uses the channel requires this package and injects <c>WSConnection</c>: it never
/// opens a socket, it registers a handler for its topic.
/// </para>
/// </summary>
[TypeScriptPackage]
[Requires<WebSocketChannelPackage>]
[TypeScriptFile( "WS_CONNECTION_URLToken.ts", "WS_CONNECTION_URL" )]
[NgProviderImport( "inject", From = "@angular/core" )]
[NgProviderImport( "WSConnection, WS_CONNECTION_URL" )]
[NgProviderImport( "provideWSConnectionSupport", From = "@local/ck-gen/CK/Ng/AspNet/WebSocketChannel/ws-connection-support" )]
[NgProvider( "{ provide: WSConnection, useFactory: () => new WSConnection( inject( WS_CONNECTION_URL, { optional: true } ) ?? '/ws' ) }" )]
[NgProvider( "provideWSConnectionSupport()", "#Support" )]
public class NgWebSocketChannelPackage : TypeScriptPackage
{
}
