import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { CKGenAppModule, NgAuthService } from '@local/ck-gen';

describe('AppComponent without backend...', () => {
  let fixture: ComponentFixture<AppComponent>;
  let app: AppComponent;

  // The app is available (but the AuthService is on error).
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: CKGenAppModule.Providers,
    }).compileComponents();
    fixture = TestBed.createComponent(AppComponent);
    app = fixture.componentInstance;
    // Wait for the AuthService initialization (404 not found).
    const ngAuthService = TestBed.inject( NgAuthService );
    await ngAuthService.authService.isInitialized;
    expect( ngAuthService.authService.lastResult.error?.errorId ).toBeTruthy();
  });

  it(`should have the 'Demo' title`, () => {
    expect(app.title).toEqual('Demo');
  });

  it('should render title', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Hello, Demo');
  });

});
