using CK.Transform.TransformLanguage;
using FluentAssertions;
using NUnit.Framework;

namespace CK.Transform.Core.Tests;

[TestFixture]
public class TransformerParsingTests
{
    [Test]
    public void Empty_minimal_function()
    {
        var h = new TransformerHost();
        var f = h.Parse( "create transform transformer as begin end" );
        f.Statements.Should().HaveCount( 0 );
    }

}
