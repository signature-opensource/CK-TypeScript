import { APP_INITIALIZER, EnvironmentProviders, inject, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService, } from '@local/ck-gen';
import { authInterceptor } from './auth-interceptor';

/**
 * Provides support providers for AuthService:
 * - an APP_INITIALIZER that will call the initial async refresh of the CK.TS.AspNet.Auth AuthService.
 * - configures the HttpClient with an interceptor that handles the Bearer token from the AuthService. 
 * @returns  EnvironmentProviders that support the AuthService (the NgAuthService injects the AuthService).
 */
export function provideNgAuthSupport(): EnvironmentProviders {

    return makeEnvironmentProviders([
        {
            provide: APP_INITIALIZER,
            useFactory: refreshAuthService,
            multi: true
        },
        provideHttpClient(withInterceptors([authInterceptor]))
    ]);
}

function refreshAuthService() {
    const authService = inject(AuthService);
    return async () => {
        await authService.refresh(true);
        if (authService.lastResult.error) {
            console.error(
                'Error while initalizing new AuthService.',
                authService.lastResult.error.errorId,
                authService.lastResult.error.errorText
            );
        }
    }
}
