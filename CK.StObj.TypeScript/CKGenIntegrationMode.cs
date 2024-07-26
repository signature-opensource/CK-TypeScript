namespace CK.Setup
{
    /// <summary>
    /// Defines <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/>.
    /// <para>
    /// An interesting integration mode is missing here: TSProjectReference would use <see cref="https://www.typescriptlang.org/docs/handbook/project-references.html"/>.
    /// Unfortunately this is not yet supported by Jest (https://github.com/kulshekhar/ts-jest/issues/1648) nor by Angular (https://github.com/angular/angular/issues/37276).
    /// Hopefully this will be supported one day.
    /// </para>
    /// </summary>
    public enum CKGenIntegrationMode
    {
        /// <summary>
        /// The "@local/ck-gen" is an alias to the /ck-gen/src (or to /ck-gen if <see cref="TypeScriptBinPathAspectConfiguration.UseSrcFolder"/> is false)
        /// folder defined in the parent's <c>tsConfig.json</c>:
        /// <code>
        /// "compilerOptions": {
        ///     "paths": {
        ///        "@local/ck-gen/*": ["./ck-gen/src/*"]
        ///     },
        /// </code>
        /// In this mode no /ck-gen/tsConfig.json is created, the /ck-gen doesn't need to be built.
        /// <para>
        /// This is the default.
        /// </para>
        /// </summary>
        Inline,

        /// <summary>
        /// <see cref="UseSrcFolder"/> is necessarily true.
        /// The "@local/ck-gen" is an alias to the /ck-gen/dist folder defined in the parent's <c>tsConfig.json</c>:
        /// <code>
        /// "compilerOptions": {
        ///     "paths": {
        ///        "@local/ck-gen": ["./ck-gen/dist"]
        ///     },
        /// </code>
        /// In this mode, the /ck-gen/tsConfig.json only emits declarations (the *.d.ts files) and their maps (the *.d.ts.map files).
        /// The /ck-gen folder MUST be built to be usable. 
        /// </summary>
        TSPath,

        /// <summary>
        /// <see cref="UseSrcFolder"/> is necessarily true.
        /// The "@local/ck-gen" is a yarn workspace inside its parent application with its package.json and its tsConfig.json
        /// (see <see cref="TypeScriptBinPathAspectConfiguration.ModuleSystem"/>).
        /// </summary>
        NpmPackage,

        /// <summary>
        /// Generated sources are saved in <see cref="TypeScriptBinPathAspectConfiguration.TargetCKGenPath"/> but no further processing
        /// is done. The only other options that are considered are <see cref="TypeScriptBinPathAspectConfiguration.UseSrcFolder"/>, <see cref="TypeScriptBinPathAspectConfiguration.GitIgnoreCKGenFolder"/>
        /// and <see cref="TypeScriptBinPathAspectConfiguration.CKGenBuildMode"/>.
        /// </summary>
        None

    }

}
