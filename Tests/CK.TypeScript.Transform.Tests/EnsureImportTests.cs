using CK.Core;
using CK.Transform.Core;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class EnsureImportTests
{
    [TestCase(
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
    [TestCase(
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
    [TestCase(
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
        import { RouterOutlet } from '@angular/router';
        import DefaultImport, { A as B } from './someFile';
        const data = 'data';
        """
    )]
    [TestCase(
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
    [TestCase(
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
    public void adding_new_import( string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().Should().Be( result );
    }

}
