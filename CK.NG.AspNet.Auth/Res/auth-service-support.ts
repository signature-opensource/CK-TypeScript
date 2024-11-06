import { APP_INITIALIZER, EnvironmentProviders, inject, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService, } from '@local/ck-gen';
import { authInterceptor } from './auth-interceptor';

/**
 * Provides support providers for NgAuthService: an APP_INITIALIZER that will call the initial async refresh and configures the HttpClient 
 * with an interceptor that handles the Bearer token. 
 * @returns  EnvironmentProviders that support the NgAuthService.
 */
export function provideNgAuthSupport(): EnvironmentProviders {

    return makeEnvironmentProviders([
        {
            provide: APP_INITIALIZER,
            useFactory: refreshAuthService,
            multi: true
        },
        {
            provide: APP_INITIALIZER,
            useFactory: refreshAuthService2,
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

function refreshAuthService2() {
    const authService = inject(AuthService);
    if (authService.availableSchemes.length == 0) {
        throw new Error("Raté");
    }

}
