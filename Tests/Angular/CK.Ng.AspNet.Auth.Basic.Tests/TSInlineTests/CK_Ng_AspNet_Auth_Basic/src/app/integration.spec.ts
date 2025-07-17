import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { NgAuthService, AuthLevel, PrivatePageComponent } from '@local/ck-gen';
import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

if (process.env["VSCODE_INSPECTOR_OPTIONS"]) jest.setTimeout(30 * 60 * 1000); // 30 minutes

describe('NgAuthService integration tests', () => {
  beforeAll(async () => {
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      {
        imports: [AppComponent],
        providers: CKGenAppModule.Providers
      }).compileComponents();

    const ngAuthService = TestBed.inject(NgAuthService);
    await ngAuthService.authService.isInitialized;
  });

  it('ngAuthService authInfos should be correctly updated', async () => {
    const ngAuthService = TestBed.inject(NgAuthService);
    const authService = ngAuthService.authService;

    expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
    expect(authService.availableSchemes.length).toBeGreaterThan(0);

    expect(ngAuthService.authenticationInfo()).toStrictEqual(authService.authenticationInfo);
    await authService.basicLogin('Albert', 'success');
    expect(ngAuthService.authenticationInfo().level).toBe(AuthLevel.Normal);
    expect(ngAuthService.authenticationInfo()).toStrictEqual(authService.authenticationInfo);

    let current = ngAuthService.authenticationInfo();
    expect(current.user.userName).toBe('Albert');
    expect(current.level).toBe(AuthLevel.Normal);

    await authService.logout();
    expect(ngAuthService.authenticationInfo()).toStrictEqual(authService.authenticationInfo);

    current = ngAuthService.authenticationInfo();
    expect(current.user.userId).toBe(0);
    expect(current.user.userName).toBe('');
  });

 it('should render userName when logged in', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    const ngAuthService = TestBed.inject(NgAuthService);
    const authService = ngAuthService.authService;

    await authService.basicLogin('Albert', 'success');

    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h3')?.textContent).toContain('MyUserInfoBox: Albert');
    await authService.logout();
});

});
