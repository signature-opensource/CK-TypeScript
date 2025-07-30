import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { NgAuthService, AuthLevel } from '@local/ck-gen';
import { ComponentFixtureAutoDetect, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { provideRouter, Router } from '@angular/router';
import { routes } from './app.routes';

if ( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout( 30 * 60 * 1000 ); // 30 minutes

describe( 'NgAuthService integration tests', () => {
  let ngAuthService: NgAuthService;

  beforeEach( async () => {
    await TestBed.configureTestingModule(
      {
        imports: [AppComponent],
        providers: [provideRouter( routes ), ...CKGenAppModule.Providers, { provide: ComponentFixtureAutoDetect, useValue: true }]
      } ).compileComponents();

    ngAuthService = TestBed.inject( NgAuthService );
    await ngAuthService.authService.isInitialized;
  } );

  afterEach( async () => {
    await ngAuthService.authService.logout();
  } );

  it( 'ngAuthService authInfos should be correctly updated', async () => {
    const authService = ngAuthService.authService;

    expect( authService.authenticationInfo.level ).toBe( AuthLevel.None );
    expect( authService.availableSchemes.length ).toBeGreaterThan( 0 );

    expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );
    await authService.basicLogin( 'Albert', 'success' );
    expect( ngAuthService.authenticationInfo().level ).toBe( AuthLevel.Normal );
    expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );

    let current = ngAuthService.authenticationInfo();
    expect( current.user.userName ).toBe( 'Albert' );
    expect( current.level ).toBe( AuthLevel.Normal );

    await authService.logout();
    expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );

    current = ngAuthService.authenticationInfo();
    expect( current.user.userId ).toBe( 0 );
    expect( current.user.userName ).toBe( '' );
  } );

  it( 'should render userName when logged in', async () => {
    const fixture = TestBed.createComponent( AppComponent );
    const router = TestBed.inject( Router );
    const authService = ngAuthService.authService;

    await authService.basicLogin( 'Albert', 'success' );

    router.navigate( [''] );
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect( compiled.querySelector( 'h3' )?.textContent ).toContain( 'MyUserInfoBox: Albert' );
  } );
} );
