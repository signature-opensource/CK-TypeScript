using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class EnsureArrayTests
{
    [TestCase( "n°1",
        """
        import { Component } from '@angular/core';
        import { RouterOutlet } from '@angular/router';

        @Component({
            selector: 'ck-logout-confirm',
            standalone: true,
            imports: [
                // This is the router outlet.
                RouterOutlet, AnotherComponent],
            templateUrl: './logout-confirm.component.html',
            styleUrl: './logout-confirm.component.less'
        })
        export class LogoutConfirmComponent { }
        """,
        """"
        create <ts> transformer
        begin
            ensure import { NewComponent } from '@local/ck-gen';
            in after "@Component" 
                in first {^braces}
                    in after "imports:"
                        in first {^[]}
                            // ensure array contains "NewComponent"
                            replace "RouterOutlet" with "RouterOutlet, NewComponent";
        end
        """",
        """
        import { Component } from '@angular/core';
        import { RouterOutlet } from '@angular/router';
        import { NewComponent } from '@local/ck-gen';

        @Component({
            selector: 'ck-logout-confirm',
            standalone: true,
            imports: [
                // This is the router outlet.
                RouterOutlet, NewComponent, AnotherComponent],
            templateUrl: './logout-confirm.component.html',
            styleUrl: './logout-confirm.component.less'
        })
        export class LogoutConfirmComponent { }
        """
    )]
    public void ensure_array_contains( string nTest, string source, string transformer, string result )
    {
        var h = new TransformerHost( new TypeScriptLanguage() );
        h.ApplyAndCheck( source, transformer, result );
    }
}
