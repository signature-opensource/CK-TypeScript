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
        import { RouterOutlet } from '@angular/router';
        import { Component } from '@angular/core';
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
        import { Component } from '@angular/core';
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
            ensure import { A as B } from './someOtherFile';
        end
        """",
    """
        import DefaultImport from './someFile';
        import { A as B } from './someOtherFile';
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

[TestFixture]
public class InjectIntoTests
{
    [TestCase(
        """
        import { RouterOutlet } from '@angular/router';
        import { Component } from '@angular/core';
        
        @Component({
          selector: 'app-root',
          standalone: true,
          imports: [RouterOutlet],
          templateUrl: './app.component.html',
          styleUrl: './app.component.less'
        })
        export class AppComponent {
          title = 'Demo';
          //<Constructor/>
        }
        """,
        """"
        create typescript transformer
        begin
            inject """
                   title += ' (modified)';

                   """ into <Constructor>;
        end
        """",
        """
        import { RouterOutlet } from '@angular/router';
        import { Component } from '@angular/core';
        
        @Component({
          selector: 'app-root',
          standalone: true,
          imports: [RouterOutlet],
          templateUrl: './app.component.html',
          styleUrl: './app.component.less'
        })
        export class AppComponent {
          title = 'Demo';
          //<Constructor>
        title += ' (modified)';
        //</Constructor>
        }
        """
        )]
    [TestCase(
        """
        import { RouterOutlet } from '@angular/router';
        import { Component } from '@angular/core';

        const trap1 = '//<Constructor>' + "<Constructor>";
        const trap2 = ` interpolated ${ trap1 } 
            //<Constructor/>
            But this is handled.`;

        @Component({
          selector: 'app-root',
          standalone: true,
          imports: [RouterOutlet],
          templateUrl: './app.component.html',
          styleUrl: './app.component.less'
        })
        export class AppComponent {
          title = 'Demo';
          //<Constructor/>
        }
        """,
        """"
        create typescript transformer
        begin
            inject """
                   title += ' (modified)';

                   """ into <Constructor>;
        end
        """",
        """
        import { RouterOutlet } from '@angular/router';
        import { Component } from '@angular/core';
        
        const trap1 = '//<Constructor>' + "<Constructor>";
        const trap2 = ` interpolated ${ trap1 } 
            //<Constructor/>
            But this is handled.`;

        @Component({
          selector: 'app-root',
          standalone: true,
          imports: [RouterOutlet],
          templateUrl: './app.component.html',
          styleUrl: './app.component.less'
        })
        export class AppComponent {
          title = 'Demo';
          //<Constructor>
        title += ' (modified)';
        //</Constructor>
        }
        """
        )]
    public void first_injection_ever( string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().Should().Be( result );
    }

}
