using CK.Core;
using CK.Setup;
using System;
using System.Linq;

namespace CK.Testing;

public static partial class TSTestHelperExtensions
{
    /// <summary>
    /// Ensures that a <see cref="TypeScriptAspect"/> is available in <see cref="EngineConfiguration.Aspects"/> and that
    /// this <see cref="BinPathConfiguration"/> contains a <see cref="TypeScriptBinPathAspectConfiguration"/> configured
    /// for the <paramref name="targetProjectPath"/>.
    /// </summary>
    /// <param name="binPath">This BinPath configuration to configure.</param>
    /// <param name="targetProjectPath">
    /// Obtained from <see cref="GetTypeScriptInlineTargetProjectPath(IBasicTestHelper, string?)"/> or one of
    /// the other similar methods.
    /// </param>
    /// <param name="tsTypes">The types to generate in TypeScript.</param>
    /// <returns>The configured <see cref="TypeScriptBinPathAspectConfiguration"/>.</returns>
    public static TypeScriptBinPathAspectConfiguration EnsureTypeScriptConfigurationAspect( this BinPathConfiguration binPath,
                                                                                            NormalizedPath targetProjectPath,
                                                                                            params Type[] tsTypes )
    {
        Throw.CheckNotNullArgument( binPath );
        binPath.Owner?.EnsureAspect<TypeScriptAspectConfiguration>();
        var tsBinPathAspect = binPath.EnsureAspect<TypeScriptBinPathAspectConfiguration>();
        return ApplyTestConfiguration( tsBinPathAspect, targetProjectPath, tsTypes );
    }

    /// <summary>
    /// Applies the options driven by <paramref name="targetProjectPath"/> to this <see cref="TypeScriptBinPathAspectConfiguration"/>.
    /// </summary>
    /// <param name="tsBinPathAspect">This configuration.</param>
    /// <param name="targetProjectPath">
    /// Obtained from <see cref="GetTypeScriptInlineTargetProjectPath(IBasicTestHelper, string?)"/> or one of
    /// the other similar methods.
    /// </param>
    /// <param name="tsTypes">The types to generate in TypeScript.</param>
    /// <returns>This configuration.</returns>
    public static TypeScriptBinPathAspectConfiguration ApplyTestConfiguration( this TypeScriptBinPathAspectConfiguration tsBinPathAspect,
                                                                               NormalizedPath targetProjectPath,
                                                                               Type[] tsTypes )
    {
        Throw.CheckArgument( targetProjectPath.Parts.Count > 2 );
        var testMode = targetProjectPath.Parts[^2] switch
        {
            "TSGeneratedOnly" => GenerateMode.GenerateOnly,
            "TSInlineTests" => GenerateMode.Inline,
            _ => Throw.ArgumentException<GenerateMode>( nameof( targetProjectPath ), $"""
                                                        Unsupported target project path: '{targetProjectPath}'.
                                                        $"The target path must be obtained with TestHelper methods:
                                                        - GetTypeScriptGeneratedOnlyTargetProjectPath()
                                                        - GetTypeScriptInlineTargetProjectPath() 
                                                        """ )
        };
        tsBinPathAspect.TargetProjectPath = targetProjectPath;
        if( testMode == GenerateMode.GenerateOnly )
        {
            tsBinPathAspect.AutoInstallJest = false;
            tsBinPathAspect.InstallYarn = YarnInstallOption.None;
            tsBinPathAspect.IntegrationMode = CKGenIntegrationMode.None;
        }
        tsBinPathAspect.OldTypes.Clear();
        tsBinPathAspect.OldTypes.AddRange( tsTypes.Select( t => new TypeScriptTypeConfiguration( t ) ) );
        return tsBinPathAspect;
    }


}
