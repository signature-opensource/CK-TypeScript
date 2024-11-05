import { APP_INITIALIZER, EnvironmentProviders, inject, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { IAuthServiceConfiguration, AuthService, IAuthenticationInfoTypeSystem, IUserInfo } from '@local/ck-gen';
import { AxiosInstance } from 'axios';
import { authInterceptor } from './auth-interceptor';

/**
 * An injection token that can be used in a DI provider.
 * Note: this is the one you pass to HttpCrisEndpoint to unify axios instances.
 * This could come from another very basic package, that would answer most of the basic use cases.
 */
export const AXIOS = new InjectionToken<AxiosInstance>('AxiosInstance');

/**
 * Provides the necessary providers to configure the NgAuthService. (AXIOS injectionToken, AuthService, HttpClient with AuthInterceptor)
 * @param axiosInstance The axios instance that will be used. An interceptor is automatically registered that adds the token to each request.
 * @param authConfig The optional configuration (endpoint and whether local storage should be used). A default configuration will be created if none is given ({@link createDefaultConfig}).
 * @param typeSystem Optional specialized type system that manages AuthenticationInfo and UserInfo. Note: this could/should be removed anytime.
 * @returns An EnvironmentProviders that can be used to configure the NgAuthService in an app's providers configuration.
 */
export function provideNgAuthService(axiosInstance: AxiosInstance,
    authConfig?: IAuthServiceConfiguration,
    typeSystem?: IAuthenticationInfoTypeSystem<IUserInfo>): EnvironmentProviders {

    return makeEnvironmentProviders([
        { provide: AXIOS, useValue: axiosInstance },
        { provide: AuthService, useValue: new AuthService(axiosInstance, authConfig, typeSystem) },
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
