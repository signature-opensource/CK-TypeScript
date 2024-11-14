namespace CK.Setup;

/// <summary>
/// <see cref="TypeScriptBinPathAspectConfiguration.YarnInstall"/> possible values.
/// </summary>
public enum YarnInstallOption
{
    /// <summary>
    /// Yarn will be installed (from an embedded resource in the engine) and upgraded if the current version is lower than
    /// the embedded one.
    /// <para>
    /// This is the default.
    /// </para>
    /// </summary>
    AutoUpgrade,

    /// <summary>
    /// Yarn will be automatically installed (from an embedded resource in the engine)
    /// if not found in <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/> or above.
    /// </summary>
    AutoInstall,

    /// <summary>
    /// No Yarn installation.
    /// <para>
    /// If no yarn can be found in <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/> or above and this is set to false,
    /// no TypeScript build will be done (as if <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/> was set to <see cref="CKGenIntegrationMode.None"/>).
    /// </para>
    /// </summary>
    None
}
