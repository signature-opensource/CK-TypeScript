import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService, } from '@local/ck-gen';
import { authInterceptor } from './auth-interceptor';

/**
 * Provides support providers for AuthService:
 * - provideAppInitializer() will call the initial async refresh of the CK.TS.AspNet.Auth AuthService.
 * - configures the HttpClient with an interceptor that handles the Bearer token from the AuthService. 
 * @returns  EnvironmentProviders that support the AuthService (the NgAuthService injects the AuthService).
 */
export function provideNgAuthSupport(): EnvironmentProviders {

    return makeEnvironmentProviders([
        provideAppInitializer( refreshAuthService ),
        provideHttpClient(withInterceptors([authInterceptor]))
    ]);
}

async function refreshAuthService() {
    const authService = inject(AuthService);
    await authService.refresh(true);
    if (authService.lastResult.error) {
        console.error(
            'Error while initalizing new AuthService.',
            authService.lastResult.error.errorId,
            authService.lastResult.error.errorText
        );
    }
}
