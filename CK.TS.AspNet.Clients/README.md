# CK.TS.AspNet.Clients

An ASP.NET host serves its client applications (the Angular applications in the browser) under its own origin: the
authentication requires it (OpenID Connect redirections, passkeys), in production as in development. This package does
it from the host's TypeScript configurations, without any per-application code in the host.

## The client applications come from the configuration

A `<TypeScript>` configuration with a `Kind` is a client application (see
[CK.TypeScript](../CK.TypeScript/README.md#client-applications)):

```xml
<TypeScript Name="App" Kind="DefaultApp" DevServerPort="4200" />
<TypeScript Name="MiniApp" Kind="App" DevServerPort="4201" UseDist="true" />
```

The engine generates the `ClientDirectory` of the host from them: each `ClientInfo` has its `Name`, its `RequestPath`
(`/` for the default one, `/mini-app` here), its `ProjectPath` (`Clients/MiniApp`, relative to the host's project), its
`OutputPath` (`dist/MiniApp/browser`), its `TypeFilterName`, its `DevServerPort` and its `UseDist`.

## One call serves them

```csharp
var app = builder.CKBuild( map );
app.UseCris();
app.MapClients();
```

`ClientsApplicationExtensions.MapClients` serves each client application under its request path, the default one last:

- **In Development, by its dev server.** The host starts it (`ng serve --port {DevServerPort}`, through the yarn found in
  or above the client's project) unless something already listens on its port (a `yarn start` launched by hand is used
  as is), and stops it with the host. What the host doesn't handle under the request path is forwarded to it (YARP direct
  forwarding): the host stays the origin, the dev server's live reload goes through it too. The dev server compiles
  before it answers: the first requests may fail.
- **Otherwise, by its built files ("dist")**: the static files of `{ContentRoot}/{ProjectPath}/{OutputPath}`, with a
  fallback to its `index.html` for its client routes (a deep link like `/mini-app/some/route`). A client application that
  is not built is skipped with a warning.

In Development, a client application whose `UseDist` is true uses its built files too: the configuration's `UseDist` is
its default (a client application nobody develops in this host), the host's configuration overrides it for a run:
`Clients:UseDist=true` for all the client applications (a UI test, a backend-only debugging session),
`Clients:{Name}:UseDist=true|false` for one of them (`dotnet run -- --Clients:MiniApp:UseDist=false`).

The real object `ClientsHosting` registers the HTTP forwarder: the host doesn't call `AddHttpForwarder()`.

## Requires.

- [CK.TypeScript](../CK.TypeScript/README.md) for the `ClientDirectory`.
- [Yarp.ReverseProxy](https://github.com/dotnet/yarp) for the forwarding to the dev servers.
