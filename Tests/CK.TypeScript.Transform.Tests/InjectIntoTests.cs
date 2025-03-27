using CK.Core;
using CK.Transform.Core;
using Shouldly;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class InjectIntoTests
{
    [TestCase( "n°1",
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
    [TestCase( "n°2",
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

                   // Modified by transformer.
                   title += ' (modified)';
                   // Modified by transformer.

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
          
          // Modified by transformer.
          title += ' (modified)';
          // Modified by transformer.
          
          //</Constructor>
        }
        """
        )]
    public void first_injection_ever( string title, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        var function = h.TryParseFunction( TestHelper.Monitor, transformer );
        Throw.DebugAssert( function != null );
        var sourceCode = h.Transform( TestHelper.Monitor, source, function );
        Throw.DebugAssert( sourceCode != null );
        sourceCode.ToString().ShouldBe( result );
    }

}
