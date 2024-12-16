using CK.Transform.TransformLanguage;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

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
        var r = h.Transform( TestHelper.Monitor, source, transformer );
        r.Should().Be( result );
    }

}
