using System;

namespace CK.Transform.Core.Tests.Helpers;

public sealed class TestLanguage : TransformLanguage
{
    internal const string _langageName = "Test";

    public TestLanguage()
        : base( _langageName, ".test" )
    {
    }

    protected override LanguageTransformAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new TestTransformAnalyzer( language, new TestAnalyzer() );
    }
}
