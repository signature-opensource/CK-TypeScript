using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.Less.Transform.Tests;

[TestFixture]
public class EnsureImportTests
{
    [TestCase( "n°0",
    """

    """,
    """"
        create <less> transformer
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
        create <less> transformer
        begin
            ensure @import (!reference, less) 's.less';
        end
        """",
        "@import (less) 's.less';"
    )]
    [TestCase( "n°2",
        "@import (once) 's.less';",
    """"
        create <less> transformer
        begin
            ensure @import (multiple) 's.less';
        end
        """",
        "@import (multiple) 's.less';"
    )]
    [TestCase( "n°3",
        "@import (less) 's.less';",
    """"
        create <less> transformer
        begin
            ensure @import (css) 's.less';
        end
        """",
        "@import (css) 's.less';"
    )]
    [TestCase( "n°4",
        "@import (multiple) 's.less';",
    """"
        create <less> transformer
        begin
            ensure @import (once) 's.less';
        end
        """",
        "@import 's.less';"
    )]
    public void merging_imports( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new LessLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

    [TestCase( "n°1", "",
        """
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';

        """ )]
    [TestCase( "n°2",
        """
        @import 'existing.less';
        
        .thing { color: red }
        
        """,
        """
        @import 'existing.less';
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';

        .thing { color: red }
        
        """ )]
    [TestCase( "n°3",
        """
        @import 'A.less';
        @import 'existing.less';
        
        .thing { color: red }
        
        """,
        """
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';
        @import 'existing.less';

        .thing { color: red }
        
        """ )]
    [TestCase( "n°4",
        """
        @import 'C.less';
        @import 'existing.less';
        
        .thing { color: red }
        
        """,
        """
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';
        @import 'existing.less';

        .thing { color: red }
        
        """ )]
    [TestCase( "n°5",
        """
        @import 'existing.less';
        @import 'C.less';
        
        .thing { color: red }
        
        """,
        """
        @import 'existing.less';
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';

        .thing { color: red }
        
        """ )]
    [TestCase( "n°6",
        """
        @import 'existing.less';
        @import 'D.less';
        @import 'B.less';
        
        .thing { color: red }
        
        """,
        """
        @import 'existing.less';
        @import 'A.less';
        @import 'B.less';
        @import 'C.less';
        @import 'D.less';

        .thing { color: red }
        
        """ )]
    public void EnsureOrderedImport_A_B_C_D( string nTest, string source, string result )
    {
        SourceCode code = new LessAnalyzer().ParseOrThrow( source );
        using( var editor = new SourceCodeEditor( TestHelper.Monitor, code ) )
        {
            EnsureImportStatement.EnsureOrderedImports( editor,
                new EnsureImportLine( ImportKeyword.None, ImportKeyword.None, "A.less" ),
                new EnsureImportLine( ImportKeyword.None, ImportKeyword.None, "B.less" ),
                new EnsureImportLine( ImportKeyword.None, ImportKeyword.None, "C.less" ),
                new EnsureImportLine( ImportKeyword.None, ImportKeyword.None, "D.less" )
                );
        }
        code.ToString().ShouldBe( result );
    }
}
