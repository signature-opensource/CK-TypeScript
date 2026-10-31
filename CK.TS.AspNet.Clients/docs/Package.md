Serves the client applications of an ASP.NET host (its TypeScript configurations that are client applications) under
the host's origin: `app.MapClients()` serves their built files in production, and forwards to their dev server in
Development (`Clients:{Name}:DevServer`). The list comes from the generated `ClientDirectory` (CK.TypeScript).
