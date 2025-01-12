using CK.Core;
using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class EnsureImportTests
{
    [TestCase( "n°1",
    """
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import DefaultImport from './someFile';
            ensure import { A as B } from './someOtherFile';
        end
        """",
    """
        import DefaultImport from './someFile';
        import { A as B } from './someOtherFile';
        const data = 'data';
        """
    )]
    [TestCase( "n°2",
    """
        import { RouterOutlet } from '@angular/router';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import DefaultImport from './someFile';
            ensure import { A as B } from './someOtherFile';
        end
        """",
    """
        import { RouterOutlet } from '@angular/router';
        import DefaultImport from './someFile';
        import { A as B } from './someOtherFile';
        const data = 'data';
        """
    )]
    [TestCase( "n°3",
    """
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import DefaultImport from './someFile';
            ensure import { A as B } from './someFile';
        end
        """",
    """
        import DefaultImport, { A as B } from './someFile';
        const data = 'data';
        """
    )]
    [TestCase( "n°4",
    """
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import * as NS from './someFile';
            ensure import { A as B } from './someFile';
        end
        """",
    """
        import * as NS from './someFile';
        import { A as B } from './someFile';
        const data = 'data';
        """
    )]
    [TestCase( "n°5",
    """
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import type { A } from './someFile';
            ensure import { A } from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import { A } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°6",
    """
        import DefaultImport from './someFile';
        import { A } from './someFile';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import { B } from './someFile';
            ensure import { C as D } from './someFile';
            ensure import {E as F,
                           G as H,
                           I,J,K,L} from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import DefaultImport from './someFile';
        import { A, B, C as D, E as F, G as H, I, J, K, L } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°7",
    """
        import DefaultImport from './someFile';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import { A } from './someFile';
            ensure import { B } from './someFile';
            ensure import { C as D } from './someFile';
            ensure import {E as F,
                           G as H,
                           I,J,K,L} from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import DefaultImport, { A, B, C as D, E as F, G as H, I, J, K, L } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°8",
    """
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import DefaultImport from './someFile';
            ensure import { A } from './someFile';
            ensure import { B } from './someFile';
            ensure import { C as D } from './someFile';
            ensure import {E as F,
                           G as H,
                           I,J,K,L} from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import DefaultImport, { A, B, C as D, E as F, G as H, I, J, K, L } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°9", // We keep type statement unchanged and create a new statement for regular named import.
    """
        import type { A } from './someFile';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import { B } from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import type { A } from './someFile';
        import { B } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°10",
    """
        import { type A } from './someFile';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import { B } from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import { type A, B } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    [TestCase( "n°11",
    """
        import { type A } from './someFile';
        const data = 'data';
        """,
    """"
        create typescript transformer
        begin
            ensure import { A } from './someFile';
            ensure import { X } from './X';
        end
        """",
    """
        import { A } from './someFile';
        import { X } from './X';
        const data = 'data';
        """
    )]
    public void adding_new_import( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().Should().Be( result );
    }

}
