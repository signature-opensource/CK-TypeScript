namespace CK.Transform.Core.Tests;

sealed class TestTransformAnalyzer : TransformLanguageAnalyzer
{
    public TestTransformAnalyzer( TransformerHost.Language language, TestAnalyzer analyzer )
        : base( language, analyzer )
    {
    }

}
