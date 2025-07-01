namespace CK.Setup;

/// <summary>
/// Extends <see cref="ICKomposableAppBuilder"/>.
/// </summary>
public static class CKomposableAppBuilderExtensions
{
    /// <summary>
    /// Ensures that at least one <see cref="TypeScriptBinPathAspectConfiguration"/> exists in the given <paramref name="binPathName"/>.
    /// If <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/> is empty, configures it to target the
    /// conventional "<see cref="ICKomposableAppBuilder.GetHostFolderPath">HostFolderPath</see>/<see cref="ICKomposableAppBuilder.ApplicationName">ApplicationName</see>.Web" path.
    /// </summary>
    /// <param name="builder">This builder.</param>
    /// <param name="binPathName">The <see cref="BinPathConfiguration.Name"/> to consider.</param>
    /// <returns>The <see cref="TypeScriptBinPathAspectConfiguration"/>.</returns>
    public static TypeScriptBinPathAspectConfiguration EnsureDefaultTypeScriptAspectConfiguration( this ICKomposableAppBuilder builder, string binPathName = "First" )
    {
        builder.EngineConfiguration.EnsureAspect<TypeScriptAspectConfiguration>();
        var binPath = builder.EngineConfiguration.FindRequiredBinPath( binPathName );
        var tsBinPathAspect = binPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        if( tsBinPathAspect.TargetProjectPath.IsEmptyPath )
        {
            tsBinPathAspect.TargetProjectPath = builder.GetHostFolderPath().AppendPart( builder.ApplicationName + ".Web" );
        }
        return tsBinPathAspect;
    }
}
