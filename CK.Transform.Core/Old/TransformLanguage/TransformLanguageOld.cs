using CK.Core;
using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

/// <summary>
/// Abstract factory for a target language <see cref="Analyzer"/> and its associated
/// transfom <see cref="BaseTransformAnalyzer"/>.
/// </summary>
public abstract class TransformLanguageOld
{
    readonly string _languageName;

    /// <summary>
    /// Initializes a language with its name.
    /// </summary>
    /// <param name="languageName">The language name.</param>
    protected TransformLanguageOld( string languageName )
    {
        Throw.CheckNotNullOrWhiteSpaceArgument( languageName );
        _languageName = languageName;
    }

    /// <summary>
    /// Gets the language name.
    /// </summary>
    public string LanguageName => _languageName;

    /// <summary>
    /// Must create a new transform language analyzer.
    /// </summary>
    /// <param name="host">The transformer host.</param>
    /// <returns>A transform language analyzer.</returns>
    internal protected abstract BaseTransformAnalyzer CreateTransformAnalyzer( TransformerHostOld host );

    /// <summary>
    /// Must create a target language analyzer.
    /// </summary>
    /// <returns>The analyzer.</returns>
    internal protected abstract Analyzer CreateTargetAnalyzer();

    /// <summary>
    /// Overridden to return the <see cref="LanguageName"/>.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _languageName;

    /// <summary>
    /// Optional extension point that can handle the "on ...." specifier.
    /// Returns null at this level.
    /// </summary>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="target">A <see cref="RawStringOld"/> or a <see cref="TokenNode"/>.</param>
    /// <returns>A <see cref="NodeScopeBuilder"/>or null.</returns>
    internal protected virtual NodeScopeBuilder? HandleTransformerTarget( IActivityMonitor monitor, AbstractNode target ) => null;

}
