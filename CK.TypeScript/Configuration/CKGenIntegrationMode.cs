namespace CK.Setup;

/// <summary>
/// Defines <see cref="TypeScriptBinPathAspectConfiguration.IntegrationMode"/>.
/// <para>
/// An interesting integration mode is missing here: TSProjectReference would use https://www.typescriptlang.org/docs/handbook/project-references.html.
/// Unfortunately this is not yet supported by Jest (https://github.com/kulshekhar/ts-jest/issues/1648) nor by Angular (https://github.com/angular/angular/issues/37276).
/// Hopefully this will be supported one day.
/// </para>
/// </summary>
public enum CKGenIntegrationMode
{
    /// <summary>
    /// The "@local/ck-gen" is an alias to the ck-gen/ folder:
    /// <code>
    /// "compilerOptions": {
    ///     "paths": {
    ///        "@local/ck-gen": ["./ck-gen"],
    ///        "@local/ck-gen/*": ["./ck-gen/*"]
    ///     },
    /// </code>
    /// <para>
    /// This is the default.
    /// </para>
    /// </summary>
    Inline,

    /// <summary>
    /// Generated sources are saved in "ck-gen/" but no further processing
    /// is done. The only other option that is considered is <see cref="TypeScriptBinPathAspectConfiguration.GitIgnoreCKGenFolder"/>.
    /// </summary>
    None

}
