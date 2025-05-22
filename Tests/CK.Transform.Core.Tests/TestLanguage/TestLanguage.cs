namespace CK.Transform.Core.Tests;

using CK.Core;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

public sealed class TestLanguage : TransformLanguage
{
    internal const string _langageName = "Test";
    readonly bool _useSourceSpanBraceAndBrackets;

    // The Live serailization requires TransformLanguage to have a default
    // public constructor. Even if we don't use this here, we follow the rule.
    public TestLanguage()
        : base( _langageName, ".test" )
    {
    }

    /// <summary>
    /// When <paramref name="useSourceSpanBraceAndBrackets"/> is false, <see cref="BraceSpan"/>
    /// and <see cref="BracketSpan"/> source span are not emitted.
    /// Instead, the language uses Enclosed operators: the behavior must be exactly the same.
    /// </summary>
    /// <param name="useSourceSpanBraceAndBrackets"></param>
    public TestLanguage( bool useSourceSpanBraceAndBrackets )
        : base( _langageName, ".test" )
    {
        _useSourceSpanBraceAndBrackets = useSourceSpanBraceAndBrackets;
    }

    protected override TransformLanguageAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new TestTransformAnalyzer( language, new TestAnalyzer( _useSourceSpanBraceAndBrackets ) );
    }

    public static void StandardTest( string source, string transformer, string result )
    {
        using( TestHelper.Monitor.OpenInfo( "Testing with SourceSpan (braces and brackets)." ) )
        {
            var h = new TransformerHost( new TestLanguage( useSourceSpanBraceAndBrackets: true ) );
            var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
            var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
            sourceCode.ToString().ShouldBe( result );
        }
        using( TestHelper.Monitor.OpenInfo( "Testing with enclosed spans (braces and brackets)." ) )
        {
            var h = new TransformerHost( new TestLanguage( useSourceSpanBraceAndBrackets: false ) );
            var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
            var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
            sourceCode.ToString().ShouldBe( result );
        }
    }

}
