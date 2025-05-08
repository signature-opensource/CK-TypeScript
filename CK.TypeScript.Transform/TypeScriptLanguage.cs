using CK.Transform.Core;
using System;

namespace CK.TypeScript.Transform;

/// <summary>
/// Accepts ".ts" file name extension.
/// </summary>
public sealed class TypeScriptLanguage : TransformLanguage
{
    internal const string _languageName = "TypeScript";

    /// <summary>
    /// Initializes a new TypeScriptLanguage instance.
    /// </summary>
    public TypeScriptLanguage()
        : base( _languageName, ".typescript", ".ts" )
    {
    }

    /// <inheritdoc/>
    protected override LanguageTransformAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new TypeScriptTransformStatementAnalyzer( language, new TypeScriptAnalyzer() );
    }


}
