namespace CK.Setup;

/// <summary>
/// Choose between ES6 or CJS module system (or both).
/// <para>See https://stackoverflow.com/a/61215252/190380</para>
/// <para>
/// Depending on this, the /ck-gen/package.json will have
/// <c>"main": "./dist/cjs/index.js"</c>, <c>"module": "./dist/esm/index.js"</c>
/// or both.
/// </para>
/// </summary>
public enum TSModuleSystem
{
    /// <summary>
    /// The /ck-gen/tsconfig.json file contains:
    /// <list type="bullet">
    ///     <item>"outDir": "./dist/esm"</item>
    ///     <item>"module": "ES6"</item>
    /// </list>
    /// </summary>
    ES6,

    /// <summary>
    /// The /ck-gen/tsconfig.json file contains:
    /// <list type="bullet">
    ///     <item>"outDir": "./dist/cjs"</item>
    ///     <item>"module": "CommonJS"</item>
    /// </list>
    /// </summary>
    CJS,

    /// <summary>
    /// The /ck-gen/tsconfig.json is like <see cref="ES6"/>
    /// and a secondary /ck-gen/tsconfig-cjs.json overrides "outDir" and "module".
    /// <para>
    /// This is the default.
    /// </para>
    /// </summary>
    ES6AndCJS,

    /// <summary>
    /// The /ck-gen/tsconfig.json is like <see cref="CJS"/>
    /// and a secondary /ck-gen/tsconfig-es6.json overrides "outDir" and "module".
    /// </summary>
    CJSAndES6,

    /// <summary>
    /// Current default is <see cref="ES6AndCJS"/>.
    /// </summary>
    Default = ES6AndCJS
}
