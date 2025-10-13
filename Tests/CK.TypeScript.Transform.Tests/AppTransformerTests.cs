using CK.Transform.Core;
using NUnit.Framework;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class AppTransformerTests
{
    [Test]
    public void cleanup_app_spec()
    {
        const string source = """
            import { TestBed } from '@angular/core/testing';
            import { App } from './app';

            describe( 'App', () => {
              beforeEach( async () => {
                await TestBed.configureTestingModule( {
                  imports: [App],
                } ).compileComponents();
              } );

              it( 'should create the app', () => {
                const fixture = TestBed.createComponent( App );
                const app = fixture.componentInstance;
                expect( app ).toBeTruthy();
              } );

              it( 'should render title', () => {
                const fixture = TestBed.createComponent( App );
                fixture.detectChanges();
                const compiled = fixture.nativeElement as HTMLElement;
                expect( compiled.querySelector( 'h1' )?.textContent ).toContain( 'Hello, From_Scratch' );
              } );
            } );
            """;
        const string transformer = """"
            create <ts> transformer
            begin
                ensure import { appConfig } from './app.config';
                in after "await TestBed.configureTestingModule"
                    in first {^{}}
                        insert """
                                     // Added by CK.TS.AngularEngine: DI is fully configured and available in tests.
                                     providers: appConfig.providers,

                               """
                            before "imports:";
                in after last "it"
                   replace * with "nomore";
                reparse;
                replace "itnomore" with "} );"
            end
            """";

        const string result = """
            import { TestBed } from '@angular/core/testing';
            import { App } from './app';
            import { appConfig } from './app.config';

            describe( 'App', () => {
              beforeEach( async () => {
                await TestBed.configureTestingModule( {
                  // Added by CK.TS.AngularEngine: DI is fully configured and available in tests.
                  providers: appConfig.providers,
                  imports: [App],
                } ).compileComponents();
              } );

              it( 'should create the app', () => {
                const fixture = TestBed.createComponent( App );
                const app = fixture.componentInstance;
                expect( app ).toBeTruthy();
              } );

              } );
            """;

        var h = new TransformerHost( new TypeScriptLanguage() );
        h.ApplyAndCheck( source, transformer, result );
    }

    [Test]
    public void cleanup_app()
    {
        const string source = """
            import { Component, signal } from '@angular/core';
            import { RouterOutlet } from '@angular/router';

            @Component({
              selector: 'app-root',
              imports: [RouterOutlet],
              templateUrl: './app.html',
              styleUrl: './app.less'
            })
            export class App {
              protected readonly title = signal('From_Scratch');
            }

            
            """;
        const string transformer = """"
            create <ts> transformer
            begin
                ensure import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
            
                in after "@Component" 
                    in first {^braces}
                        in after "imports:"
                            in first {^[]}
                                replace "RouterOutlet" with "RouterOutlet, CKGenAppModule";
                in single {class}
                    in first {braces}
                    replace * with "{ }";
            end
            """";

        const string result = """
            import { Component, signal } from '@angular/core';
            import { RouterOutlet } from '@angular/router';
            import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';

            @Component({
              selector: 'app-root',
              imports: [RouterOutlet, CKGenAppModule],
              templateUrl: './app.html',
              styleUrl: './app.less'
            })
            export class App { }

            """;

        var h = new TransformerHost( new TypeScriptLanguage() );
        h.ApplyAndCheck( source, transformer, result );        
    }
}
