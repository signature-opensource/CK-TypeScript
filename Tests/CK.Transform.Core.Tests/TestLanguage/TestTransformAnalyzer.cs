using System;

namespace CK.Transform.Core.Tests.Helpers;

sealed class TestTransformAnalyzer : TransformLanguageAnalyzer
{
    public TestTransformAnalyzer( TransformerHost.Language language, TestAnalyzer analyzer )
        : base( language, analyzer )
    {
    }

}
