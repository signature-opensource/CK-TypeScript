using CK.Core;
using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// The compile time model of the client feature of a TypeScript framework ("NgApp": its <c>[CompileConfiguration&lt;TModel&gt;]</c>):
/// what every TypeScript client needs to be built. The App builder reads it from the feature's section of the client
/// ("Local:PartyClient:{Name}:NgApp"); the TypeScript contributor makes it the <see cref="TypeScriptBinPathAspectConfiguration"/>
/// of the client. Its keys are compile time keys: the run time ignores them.
/// </summary>
[ConfigurationModel]
public sealed class TypeScriptClientCompileConfiguration : IConfigurationModel
{
    /// <summary>
    /// Gets the request path under which the party serves the client: "/{kebab-name}" by default ("MiniApp" is served
    /// under "/mini-app"), "/" for the default client. It must start with a '/' and is unique among the clients of the App.
    /// </summary>
    public string? RequestPath { get; init; }

    /// <summary>
    /// Gets the port of the dev server of the client: in Development, the host starts the dev server on this port (unless
    /// something already listens on it) and forwards to it. Unique in the App.
    /// </summary>
    public int? DevServerPort { get; init; }

    /// <summary>
    /// Gets the TypeScript target project path that contains the "ck-gen" folder: "{ProjectPath}/Clients/{Name}" by default
    /// (see <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/>).
    /// </summary>
    public string? TargetProjectPath { get; init; }

    /// <summary>
    /// Gets the TypeScript version to use when it is not known by the target project.
    /// </summary>
    public string? DefaultTypeScriptVersion { get; init; }

    /// <summary>
    /// Gets how the "ck-gen" folder is integrated in the target project.
    /// </summary>
    public CKGenIntegrationMode? IntegrationMode { get; init; }

    /// <summary>
    /// Gets whether yarn is installed in the target project.
    /// </summary>
    public YarnInstallOption? InstallYarn { get; init; }

    /// <summary>
    /// Gets whether the "ck-gen" folder is ignored by git (defaults to true).
    /// </summary>
    public bool? GitIgnoreCKGenFolder { get; init; }

    /// <summary>
    /// Gets whether jest is installed in the target project.
    /// </summary>
    public bool? AutoInstallJest { get; init; }

    /// <summary>
    /// Gets whether the TypeScript project references are enabled.
    /// </summary>
    public bool? EnableTSProjectReferences { get; init; }

    /// <summary>
    /// Gets the default culture of the client ("en" by default).
    /// </summary>
    public string? DefaultCulture { get; init; }

    /// <summary>
    /// Gets the active cultures of the client (a list, or a comma separated string).
    /// </summary>
    public IReadOnlyList<string>? ActiveCultures { get; init; }

    /// <summary>
    /// Gets the name of the type filter of the client: "TypeScript{Name}" by default.
    /// </summary>
    public string? TypeFilterName { get; init; }

    /// <summary>
    /// Gets the paths of the barrels (the "index.ts" files) to generate, relative to the "ck-gen" folder.
    /// </summary>
    public IReadOnlyList<string>? Barrels { get; init; }

    /// <summary>
    /// Gets the TypeScript settings of types of the client (the type set of the client says which types it has: these
    /// settings only configure their TypeScript).
    /// </summary>
    public IReadOnlyList<TypeScriptTypeCompileConfiguration>? Types { get; init; }
}

/// <summary>
/// The TypeScript settings of a type (or of the types that match a pattern) of a client: see <see cref="TypeScriptClientCompileConfiguration.Types"/>.
/// The same settings as the ones of <see cref="CK.TypeScript.TypeScriptTypeAttribute"/>.
/// </summary>
[ConfigurationModel]
public sealed class TypeScriptTypeCompileConfiguration : IConfigurationModel
{
    /// <summary>
    /// Gets the assembly qualified name of the type, or a pattern of type names.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the TypeScript name of the type.
    /// </summary>
    public string? TypeName { get; init; }

    /// <summary>
    /// Gets the folder of the TypeScript file of the type, relative to the "ck-gen" folder.
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Gets the name of the TypeScript file of the type.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the assembly qualified name of a type whose TypeScript file holds this one.
    /// </summary>
    public string? SameFileAs { get; init; }

    /// <summary>
    /// Gets the assembly qualified name of a type whose TypeScript folder holds this one.
    /// </summary>
    public string? SameFolderAs { get; init; }
}
