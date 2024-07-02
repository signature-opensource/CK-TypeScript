using CK.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Extends <see cref="ICKomposableAppBuilder"/>.
    /// </summary>
    public static class CKomposableAppBuilderExtensions
    {
        /// <summary>
        /// Ensures that at least one <see cref="TypeScriptBinPathAspectConfiguration"/> exists in the given <paramref name="binPathName"/>
        /// and configures it to target the conventionnal "<see cref="ICKomposableAppBuilder.GetHostFolderPath"/>/Client" path.
        /// <para>
        /// By default, <see cref="TypeScriptBinPathAspectConfiguration.EnsureTestSupport"/> is false.
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
            tsBinPathAspect.AutoInstallVSCodeSupport = true;
            tsBinPathAspect.AutoInstallYarn = true;
            tsBinPathAspect.GitIgnoreCKGenFolder = true;
            tsBinPathAspect.TargetProjectPath = builder.GetHostFolderPath().AppendPart( "Client" );
            return tsBinPathAspect;
        }
    }
}
