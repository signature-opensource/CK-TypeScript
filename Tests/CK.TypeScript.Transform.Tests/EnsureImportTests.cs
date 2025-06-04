using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;


[TestFixture]
public class EnsureImportTests
{
    [TestCase( "n°0",
        "",
        """"
        create <ts> transformer
        begin
            ensure import { A } from './someFile';
        end
        """",
        """
        import { A } from './someFile';

        """
    )]
    [TestCase( "n°1",
        """
        const data = 'data';
        """,
        """"
        create <ts> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <typescript> transformer
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
        create <ts> transformer
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
    [TestCase( "n°12",
        """
        import { MyCommand, TestCommand, FirstCommand } from '@local/ck-gen';
        """,
    """"
        create <ts> transformer
        begin
            ensure import { TestCommand } from '@local/ck-gen';
        end
        """",
    """
        import { MyCommand, TestCommand, FirstCommand } from '@local/ck-gen';
        """
    )]
    public void merging_imports( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

    [TestCase( "n°1",
    """
        import { Some } from '@local/ck-gen';
        """,
        """"
        create <ts> transformer
        begin
            ensure import { Some } from '@local/ck-gen/Better';
        end
        """",
        """
        import { Some } from '@local/ck-gen/Better';

        """
    )]
    [TestCase( "n°2",
    """
        import { Some, Other, Orphan } from '@local/ck-gen';

        some code;
        """,
        """"
        create <ts> transformer
        begin
            ensure import { Some } from '@local/ck-gen/Better';
            ensure import { Other } from '@local/ck-gen/Better';
        end
        """",
        """
        import { Orphan } from '@local/ck-gen';
        import { Some, Other } from '@local/ck-gen/Better';
        
        some code;
        """
    )]
    [TestCase( "n°3",
    """
        import { Some, Other, Orphan } from '@local/ck-gen';

        some other code;
        """,
        """"
        create <ts> transformer
        begin
            ensure import { Some } from '@local/ck-gen/Better';
            ensure import { Other } from '@local/ck-gen/Better';
        end
        """",
        """
        import { Orphan } from '@local/ck-gen';
        import { Some, Other } from '@local/ck-gen/Better';
        
        some other code;
        """
    )]
    [TestCase( "n°4",
    """
        import { Some, Other } from '@local/ck-gen';
        import { Another as X, AndY } from '@local/ck-gen';
        """,
        """"
        create <ts> transformer
        begin
            ensure import { Some } from '@local/ck-gen/Better';
            ensure import { Other } from '@local/ck-gen/AnotherBetter';
            ensure import { Another as X } from '@local/ck-gen/AnotherBetter';
            ensure import { AndY } from '@local/ck-gen/Better';
        end
        """",
        """
        import { Some, AndY } from '@local/ck-gen/Better';
        import { Other, Another as X } from '@local/ck-gen/AnotherBetter';
        
        """
    )]
    [TestCase( "n°5",
    """
        import { Some, Other } from './Up/Folder';
        import { Another as X, AndY } from './Up/Folder';
        """,
        """"
        create <ts> transformer
        begin
            ensure import { Some, AndY } from './Up/Folder/Better';
            ensure import { Other, Another as X } from './Up/Folder/AnotherBetter';
        end
        """",
        """
        import { Some, AndY } from './Up/Folder/Better';
        import { Other, Another as X } from './Up/Folder/AnotherBetter';
        
        """
    )]
    public void allowing_more_precise_import_path( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer ).ShouldNotBeNull();
        var sourceCode = h.Transform( TestHelper.Monitor, source, function ).ShouldNotBeNull();
        sourceCode.ToString().ShouldBe( result );
    }

}
