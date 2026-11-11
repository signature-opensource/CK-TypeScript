using CK.Core;

namespace CK.TypeScript;

/// <summary>
/// A client application served by its host: a TypeScript configuration whose <see cref="CK.Setup.TypeScriptBinPathAspectConfiguration.Kind"/>
/// is <see cref="CK.Setup.ClientKind.DefaultApp"/> or <see cref="CK.Setup.ClientKind.App"/>. See <see cref="ClientDirectory"/>.
/// </summary>
public sealed class ClientInfo
{
    /// <summary>
    /// Initializes a new client application description.
    /// </summary>
    /// <param name="name">The <see cref="Name"/>.</param>
    /// <param name="requestPath">The <see cref="RequestPath"/>.</param>
    /// <param name="projectPath">The <see cref="ProjectPath"/>.</param>
    /// <param name="outputPath">The <see cref="OutputPath"/>.</param>
    /// <param name="typeFilterName">The <see cref="TypeFilterName"/>.</param>
    /// <param name="devServerPort">The <see cref="DevServerPort"/>.</param>
    /// <param name="useDist">The <see cref="UseDist"/>.</param>
    /// <param name="facts">The <see cref="Facts"/>, as (key, value) pairs (a fact without value has an empty value).</param>
    public ClientInfo( string name,
                       string requestPath,
                       NormalizedPath projectPath,
                       NormalizedPath outputPath,
                       string typeFilterName,
                       int devServerPort,
                       bool useDist,
                       (string Key, string Value)[] facts )
    {
        Facts = CK.AppIdentity.ClientApplicationFacts.Create( facts );
        DevServerPort = devServerPort;
        UseDist = useDist;
        Name = name;
        RequestPath = requestPath;
        ProjectPath = projectPath;
        OutputPath = outputPath;
        TypeFilterName = typeFilterName;
    }

    /// <summary>
    /// Gets the name of the client application (the name of its TypeScript configuration).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the request path under which the host serves the client application: "/" for the default one,
    /// "/{kebab-name}" for the other ones.
    /// </summary>
    public string RequestPath { get; }

    /// <summary>
    /// Gets the path of the client application's project: relative to the host's project when it is under it,
    /// absolute otherwise.
    /// </summary>
    public NormalizedPath ProjectPath { get; }

    /// <summary>
    /// Gets the path of the built application, relative to the <see cref="ProjectPath"/>:
    /// "dist/{project folder}/browser" (the Angular application builder's output).
    /// </summary>
    public NormalizedPath OutputPath { get; }

    /// <summary>
    /// Gets the name of the type set that the client application exchanges with its host
    /// (see <see cref="CK.Setup.TypeScriptBinPathAspectConfiguration.TypeFilterName"/>).
    /// </summary>
    public string TypeFilterName { get; }

    /// <summary>
    /// Gets the port of the dev server: in Development, the host starts it (unless something already listens on it)
    /// and forwards to it (unless the <see cref="UseDist"/> applies).
    /// </summary>
    public int DevServerPort { get; }

    /// <summary>
    /// Gets whether the built files are served even in Development (the configured default: the host's configuration
    /// can override it for a run).
    /// </summary>
    public bool UseDist { get; }

    /// <summary>
    /// Gets the facts of the client application: the union of the PartyClientFact attributes of its selected TypeScript
    /// packages, computed by the engine.
    /// </summary>
    public CK.AppIdentity.ClientApplicationFacts Facts { get; }

    /// <summary>
    /// Returns "Name (RequestPath)".
    /// </summary>
    /// <returns>A readable string.</returns>
    public override string ToString() => $"{Name} ({RequestPath})";
}
