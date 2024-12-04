using CK.Transform.Core;

namespace CK.Transform.TransformLanguage;

public abstract class TransformLanguage
{
    readonly string _languageName;

    protected TransformLanguage( string languageName )
    {
        _languageName = languageName;
    }

    public string LanguageName => _languageName;

    internal protected abstract BaseTransformAnalyzer CreateTransformAnalyzer();

    internal protected abstract Analyzer CreateTargetAnalyzer();

    public override string ToString() => _languageName;
}
