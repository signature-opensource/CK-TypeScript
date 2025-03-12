using NUnit.Framework;
using System.Linq;

namespace CK.EmbeddedResources.Globalization.Tests;

[TestFixture]
public class CombinationTests
{
    [Test]
    public void no_override_on_default()
    {
        var p = new CodeGenResourceContainer( "P" );
        var d1 = new CodeGenResourceContainer( "D1" );
        var d2 = new CodeGenResourceContainer( "D2" );

        d1.AddText( "locales/default.jsonc", """{ "K1": "v1" }""" );
        d1.AddText( "locales/default.jsonc", """{ "K2": "v2" }""" );
    }
}
