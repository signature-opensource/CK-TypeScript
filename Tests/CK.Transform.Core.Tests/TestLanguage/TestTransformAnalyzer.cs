using System;

namespace CK.Transform.Core.Tests.Helpers;

sealed class TestTransformAnalyzer : LanguageTransformAnalyzer
{
    public TestTransformAnalyzer( TransformerHost.Language language, TestAnalyzer analyzer )
        : base( language, analyzer )
    {
    }

}
