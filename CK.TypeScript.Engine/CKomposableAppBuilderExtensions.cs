namespace CK.Setup;

/// <summary>
/// Extends <see cref="ICKomposableAppBuilder"/>.
/// </summary>
public static class CKomposableAppBuilderExtensions
{
    /// <summary>
    /// Ensures that the <see cref="TypeScriptAspectConfiguration"/> and at least one <see cref="TypeScriptBinPathAspectConfiguration"/>
    /// exist in the given <paramref name="binPathName"/>.
    /// <para>
    /// The <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/> is left as-is: when empty, the engine derives it
    /// from the <see cref="BinPathConfiguration.ProjectPath"/> (the host folder of the builder), "{ProjectPath}/Clients/{Name}" for
    /// a named configuration (see <see cref="TypeScriptBinPathAspectConfiguration.Name"/>).
    /// </para>
    /// </summary>
    /// <param name="builder">This builder.</param>
    /// <param name="binPathName">The <see cref="BinPathConfiguration.Name"/> to consider.</param>
    /// <returns>The <see cref="TypeScriptBinPathAspectConfiguration"/>.</returns>
    public static TypeScriptBinPathAspectConfiguration EnsureDefaultTypeScriptAspectConfiguration( this ICKomposableAppBuilder builder, string binPathName = "First" )
    {
        builder.EngineConfiguration.EnsureAspect<TypeScriptAspectConfiguration>();
        var binPath = builder.EngineConfiguration.FindRequiredBinPath( binPathName );
        return binPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
    }
}
