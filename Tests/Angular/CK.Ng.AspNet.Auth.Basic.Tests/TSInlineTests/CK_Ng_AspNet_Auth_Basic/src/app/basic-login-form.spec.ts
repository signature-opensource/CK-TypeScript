import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthService, NotificationService } from '@local/ck-gen';
import { BasicLoginForm } from '@local/ck-gen/CK/Ng/AspNet/Auth/Basic/basic-login-form/basic-login-form';

describe( 'BasicLoginForm', () => {
  let fixture: ComponentFixture<BasicLoginForm>;
  let basicLogin: jest.Mock;

  beforeEach( async () => {
    basicLogin = jest.fn().mockResolvedValue( undefined );
    const authService = {
      authenticationInfo: { unsafeUser: { userName: '' } },
      rememberMe: true,
      lastResult: { error: null },
      basicLogin
    };
    await TestBed.configureTestingModule( {
      imports: [BasicLoginForm],
      providers: [
        provideTranslateService(),
        { provide: AuthService, useValue: authService },
        { provide: NotificationService, useValue: { displaySimpleMessage: jest.fn() } }
      ]
    } ).compileComponents();
    fixture = TestBed.createComponent( BasicLoginForm );
    fixture.detectChanges();
    await fixture.whenStable();
  } );

  function input( autocomplete: string ): HTMLInputElement {
    return fixture.nativeElement.querySelector( `input[autocomplete="${autocomplete}"]` );
  }

  async function type( autocomplete: string, value: string ): Promise<void> {
    const element = input( autocomplete );
    element.value = value;
    element.dispatchEvent( new Event( 'input' ) );
    element.dispatchEvent( new Event( 'blur' ) );
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function loginButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector( 'button.login-form-button' );
  }

  it( 'disables the login button until the user name and the password are filled', async () => {
    expect( loginButton().disabled ).toBe( true );
    await type( 'username', 'Albert' );
    expect( loginButton().disabled ).toBe( true );
    await type( 'current-password', 'secret' );
    expect( loginButton().disabled ).toBe( false );
  } );

  it( 'logs in with the trimmed user name, the password and remember me', async () => {
    await type( 'username', '  Albert ' );
    await type( 'current-password', ' secret ' );
    // A DOM click would run the ng-zorro wave, which needs the getPropertyValue that the jest-setup.ts getComputedStyle stub lacks.
    fixture.debugElement.query( By.css( 'button.login-form-button' ) ).triggerEventHandler( 'click' );
    await fixture.whenStable();
    expect( basicLogin ).toHaveBeenCalledWith( 'Albert', ' secret ', true );
  } );

  it( 'does not log in on Enter while the password is empty', async () => {
    await type( 'username', 'Albert' );
    input( 'username' ).dispatchEvent( new KeyboardEvent( 'keydown', { code: 'Enter', bubbles: true } ) );
    await fixture.whenStable();
    expect( basicLogin ).not.toHaveBeenCalled();
  } );

  it( 'shows the required tip once the user name is cleared', async () => {
    expect( fixture.nativeElement.textContent ).not.toContain( 'CK.BasicLogin.UserName.Required' );
    await type( 'username', 'Albert' );
    await type( 'username', '' );
    expect( fixture.nativeElement.textContent ).toContain( 'CK.BasicLogin.UserName.Required' );
  } );
} );
