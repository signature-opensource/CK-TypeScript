namespace CK.Setup;

/// <summary>
/// Whether a <see cref="TypeScriptBinPathAspectConfiguration"/> is a client application served by its host.
/// </summary>
public enum ClientKind
{
    /// <summary>
    /// Not a client application: the "ck-gen" folder is generated, the target project is not served by the host
    /// (a library, a test project).
    /// </summary>
    None,

    /// <summary>
    /// The default client application of its host: it is served at the root ("/").
    /// There can be at most one default client application per BinPath.
    /// </summary>
    DefaultApp,

    /// <summary>
    /// A client application served by its host under "/{kebab-name}" (the configuration's
    /// <see cref="TypeScriptBinPathAspectConfiguration.Name"/> in kebab case).
    /// </summary>
    App
}
