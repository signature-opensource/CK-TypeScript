using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using static CK.Testing.MonitorTestHelper;

namespace CK.Less.Transform.Tests;

[TestFixture]
public class ParsingTests
{
    [Test]
    public void parsing_bootstrap()
    {
        var c = File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "bootstrap.css" ) );
        c.Should().NotBeNull().And.NotBeEmpty();
        var a = new LessAnalyzer();
        var sourceCode = a.ParseOrThrow( c );
        sourceCode.Tokens.Count.Should().BeGreaterThan( 1000 );
    }

    [Test]
    public void parsing_all_ng_zorro()
    {
        var zip = ZipFile.OpenRead( TestHelper.TestProjectFolder.AppendPart( "ng-zorro-antd.all.less.zip" ) );
        using var reader = new StreamReader( zip.Entries[0].Open() );
        var all = reader.ReadToEnd();
        all.Should().NotBeNull().And.NotBeEmpty();
        var a = new LessAnalyzer();
        var sourceCode = a.ParseOrThrow( all );
        sourceCode.Tokens.Count.Should().BeGreaterThan( 1000 );
    }

    [TestCase( "t" )]
    [TestCase( "_" )]
    [TestCase( "@ab" )]
    [TestCase( "@{a}" )]
    [TestCase( "-_" )]
    [TestCase( "--" )]
    [TestCase( "--some-" )]
    [TestCase( "&some" )]
    [TestCase( "&-some&-&@{abcde}" )]
    [TestCase( "\\9" )]
    [TestCase( "\\abcde" )]
    public void generic_identifier_token( string oneToken )
    {
        var a = new LessAnalyzer();
        var sourceCode = a.ParseOrThrow( oneToken );
        sourceCode.Tokens.Count.Should().Be( 1 );
        sourceCode.Tokens[0].TokenType.Should().Be( TokenType.GenericIdentifier );
        sourceCode.Tokens[0].Text.ToString().Should().Be( oneToken );
    }
}
