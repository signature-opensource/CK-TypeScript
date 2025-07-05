namespace CK.Transform.Core.Tests;

using CK.Core;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

public sealed class TestLanguage : TransformLanguage
{
    internal const string _langageName = "Test";
    readonly bool _useSourceSpanOperator;

    // The Live serialization requires TransformLanguage to have a default
    // public constructor. Even if we don't use this here, we follow the rule.
    public TestLanguage()
        : base( _langageName, ".test" )
    {
    }

    /// <summary>
    /// When <paramref name="useSourceSpanOperator"/> is false, <see cref="BraceSpan"/>
    /// and <see cref="BracketSpan"/> source span are not emitted.
    /// Instead, the language uses Enclosed operators: the behavior must be exactly the same.
    /// </summary>
    /// <param name="useSourceSpanOperator"></param>
    public TestLanguage( bool useSourceSpanOperator )
        : base( _langageName, ".test" )
    {
        _useSourceSpanOperator = useSourceSpanOperator;
    }

    protected override TransformLanguageAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new TestTransformAnalyzer( language, new TestAnalyzer( _useSourceSpanOperator ) );
    }

    public static void StandardTest( string source, string transformer, string result )
    {
        using( TestHelper.Monitor.OpenInfo( "Testing with SourceSpan (braces, brackets, parens)." ) )
        {
            var h = new TransformerHost( new TestLanguage( useSourceSpanOperator: true ) );
            var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
            var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
            sourceCode.ToString().ShouldBe( result );
        }
        using( TestHelper.Monitor.OpenInfo( "Testing with enclosed spans (braces, brackets, parens)." ) )
        {
            var h = new TransformerHost( new TestLanguage( useSourceSpanOperator: false ) );
            var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
            var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
            sourceCode.ToString().ShouldBe( result );
        }
    }

}
