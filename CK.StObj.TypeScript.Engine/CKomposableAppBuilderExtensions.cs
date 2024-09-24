using CK.Core;
using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup;

/// <summary>
/// Extends <see cref="ICKomposableAppBuilder"/>.
/// </summary>
public static class CKomposableAppBuilderExtensions
{
    /// <summary>
    /// Ensures that at least one <see cref="TypeScriptBinPathAspectConfiguration"/> exists in the given <paramref name="binPathName"/>.
    /// If <see cref="TypeScriptBinPathAspectConfiguration.TargetProjectPath"/> is empty, configures it to target the
    /// conventional "<see cref="ICKomposableAppBuilder.GetHostFolderPath"/>/Client" path.
    /// <para>
    /// Sets <see cref="TypeScriptBinPathAspectConfiguration.AutoInstallYarn"/> and <see cref="TypeScriptBinPathAspectConfiguration.GitIgnoreCKGenFolder"/> to true.
    /// </para>
    /// </summary>
    /// <param name="builder">This builder.</param>
    /// <param name="binPathName">The <see cref="BinPathConfiguration.Name"/> to consider.</param>
    /// <returns>The <see cref="TypeScriptBinPathAspectConfiguration"/>.</returns>
    public static TypeScriptBinPathAspectConfiguration EnsureDefaultTypeScriptAspectConfiguration( this ICKomposableAppBuilder builder, string binPathName = "First" )
    {
        var binPath = builder.EngineConfiguration.FindRequiredBinPath( binPathName );
        var tsAspect = builder.EngineConfiguration.EnsureAspect<TypeScriptAspectConfiguration>();
        var tsBinPathAspect = builder.EngineConfiguration.FirstBinPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        tsBinPathAspect.AutoInstallYarn = true;
        tsBinPathAspect.GitIgnoreCKGenFolder = true;
        if( tsBinPathAspect.TargetProjectPath.IsEmptyPath )
        {
            tsBinPathAspect.TargetProjectPath = builder.GetHostFolderPath().AppendPart( "Client" );
        }
        return tsBinPathAspect;
    }
}
