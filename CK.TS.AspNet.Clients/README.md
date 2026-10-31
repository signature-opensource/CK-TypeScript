# CK.TS.AspNet.Clients

An ASP.NET host serves its client applications (the Angular applications in the browser) under its own origin: the
authentication requires it (OpenID Connect redirections, passkeys), in production as in development. This package does
it from the host's TypeScript configurations, without any per-application code in the host.

## The client applications come from the configuration

A `<TypeScript>` configuration with a `Kind` is a client application (see
[CK.TypeScript](../CK.TypeScript/README.md#client-applications)):

```xml
<TypeScript Name="App" Kind="DefaultApp" />
<TypeScript Name="MiniApp" Kind="App" />
```

The engine generates the `ClientDirectory` of the host from them: each `ClientInfo` has its `Name`, its `RequestPath`
(`/` for the default one, `/mini-app` here), its `ProjectPath` (`Clients/MiniApp`, relative to the host's project), its
`OutputPath` (`dist/MiniApp/browser`) and its `TypeFilterName`.

## One call serves them

```csharp
var app = builder.CKBuild( map );
app.UseCris();
app.MapClients();
```

`ClientsApplicationExtensions.MapClients` serves each client application under its request path, the default one last:

- **Its built files**: the static files of `{ContentRoot}/{ProjectPath}/{OutputPath}`, with a fallback to its `index.html`
  for its client routes (a deep link like `/mini-app/some/route`). A client application that is not built is skipped
  with a warning.
- **Its dev server, in Development**: when `Clients:{Name}:DevServer` is configured (typically on the command line,
  `--Clients:MiniApp:DevServer=http://localhost:4201`), what the host doesn't handle under the request path is forwarded
  to it (YARP direct forwarding). The host stays the origin: the dev server's live reload goes through it too.

The real object `ClientsHosting` registers the HTTP forwarder: the host doesn't call `AddHttpForwarder()`.

## Requires.

- [CK.TypeScript](../CK.TypeScript/README.md) for the `ClientDirectory`.
- [Yarp.ReverseProxy](https://github.com/dotnet/yarp) for the forwarding to the dev servers.
