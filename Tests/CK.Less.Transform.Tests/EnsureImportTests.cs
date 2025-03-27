using CK.Core;
using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.Less.Transform.Tests;

[TestFixture]
public class EnsureImportTests
{
    [TestCase( "n°0",
    """

    """,
    """"
        create less transformer
        begin
            ensure @import 's.less';
        end
        """",
    """
        @import 's.less';

        """
    )]
    [TestCase( "n°1",
        "@import (reference) 's.less';",
    """"
        create less transformer
        begin
            ensure @import (!reference, less) 's.less';
        end
        """",
        "@import (less) 's.less';"
    )]
    [TestCase( "n°2",
        "@import (once) 's.less';",
    """"
        create less transformer
        begin
            ensure @import (multiple) 's.less';
        end
        """",
        "@import (multiple) 's.less';"
    )]
    [TestCase( "n°3",
        "@import (less) 's.less';",
    """"
        create less transformer
        begin
            ensure @import (css) 's.less';
        end
        """",
        "@import (css) 's.less';"
    )]
    [TestCase( "n°4",
        "@import (multiple) 's.less';",
    """"
        create less transformer
        begin
            ensure @import (once) 's.less';
        end
        """",
        "@import 's.less';"
    )]
    public void merging_imports( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new LessLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }
}
