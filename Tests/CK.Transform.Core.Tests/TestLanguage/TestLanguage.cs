namespace CK.Transform.Core.Tests;

public sealed class TestLanguage : TransformLanguage
{
    internal const string _langageName = "Test";

    public TestLanguage()
        : base( _langageName, ".test" )
    {
    }

    protected override TransformLanguageAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new TestTransformAnalyzer( language, new TestAnalyzer() );
    }
}
