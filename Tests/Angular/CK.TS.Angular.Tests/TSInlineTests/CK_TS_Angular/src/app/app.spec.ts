import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { appConfig } from './app.config';

describe( 'AppComponent', () => {
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

  it( `should have the 'CK_TS_Angular' title`, () => {
    const fixture = TestBed.createComponent( App );
    const app = fixture.componentInstance;
    expect( app.title() ).toEqual( 'CK_TS_Angular' );
  } );

  it( 'should render title', () => {
    const fixture = TestBed.createComponent( App );
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect( compiled.querySelector( 'h1' )?.textContent ).toContain( 'Hello, CK_TS_Angular' );
  } );
} );
