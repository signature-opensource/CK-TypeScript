using CK.Core;
using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;
using System.Linq;

namespace CK.TypeScript.Transform.Tests;

public class TypeScriptParsingTests
{
    [Test]
    public void empty_parsing()
    {
        var a = new TypeScriptAnalyzer();
        var sourceCode = a.ParseOrThrow( "" );
        sourceCode.Spans.ShouldBeEmpty();
        sourceCode.Tokens.ShouldBeEmpty();
    }

    [Test]
    public void unrecognized_token()
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( "🙃" );
        Throw.DebugAssert( result != null && result.FirstError != null );
        result.FirstError.ErrorMessage.ShouldBe( "Unrecognized token." );
    }

    [TestCase( "0" )]
    [TestCase( "1" )]
    [TestCase( "3712" )]
    [TestCase( "0.0" )]
    [TestCase( "3712.42" )]
    [TestCase( ".0" )]
    [TestCase( ".0e10" )]
    [TestCase( ".0e+2" )]
    [TestCase( ".0e-2" )]
    // BigInt notation.
    [TestCase( "1234n" )]
    [TestCase( "0n" )]
    public void successful_number_token( string text )
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( text );
        result.FirstError.ShouldBeNull();
        result.Success.ShouldBeTrue();
        result.SourceCode.Tokens.Count.ShouldBe( 1 );
        var numberToken = result.SourceCode.Tokens[0];
        numberToken.TokenType.ShouldBe( TokenType.GenericNumber );
        numberToken.TextEquals( text );
    }

    [TestCase( "i" )]
    [TestCase( "i0" )]
    // _ is valid and may appear everywhere.
    [TestCase( "_" )]
    [TestCase( "_i_" )]
    [TestCase( "_i42" )]
    [TestCase( "_42" )]
    // $ is valid and may appear everywhere.
    [TestCase( "$" )]
    [TestCase( "$_i" )]
    [TestCase( "$1" )]
    [TestCase( "a$b" )]
    [TestCase( "_$" )]
    // # is for private fields. It can only start an identifier.
    [TestCase( "#i" )]
    [TestCase( "#i0" )]
    // @ support is NOT TypeScript, it is an extension.
    // It can only start an identifier.
    [TestCase( "@i" )]
    [TestCase( "@i0" )]
    public void successful_identifier_token( string text )
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( text );
        result.FirstError.ShouldBeNull();
        result.Success.ShouldBeTrue();
        result.SourceCode.Tokens.Count.ShouldBe( 1 );
        var identifierToken = result.SourceCode.Tokens[0];
        identifierToken.TokenType.ShouldBe( TokenType.GenericIdentifier );
        identifierToken.TextEquals( text );
    }

    [TestCase( "@", TokenType.AtSign )]
    [TestCase( "#", TokenType.Hash )]
    public void not_an_identifier_token( string text, TokenType expected )
    {
        var a = new TypeScriptAnalyzer();
        var result = a.Parse( text );
        result.FirstError.ShouldBeNull();
        result.Success.ShouldBeTrue();
        result.SourceCode.Tokens.Count.ShouldBe( 1 );
        result.SourceCode.Tokens[0].TokenType.ShouldBe( expected );
    }
}
