import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders, Optional } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService, IAuthenticationInfoTypeSystem, IUserInfo } from '@local/ck-gen';
import { AxiosInstance } from 'axios';
import { AuthServiceClientConfiguration, createDefaultConfig } from './auth-service-configuration';
import { authInterceptor } from './auth-interceptor';

/**
 * An injection token that can be used in a DI provider.
 * Note: this is the one you pass to HttpCrisEndpoint to unify axios instances.
 * This could come from another very basic package, that would answer most of the basic use cases.
 */
export const AXIOS = new InjectionToken<AxiosInstance>( 'AxiosInstance' );

/**
 * Factory method that instanciates an AuthService. A default AuthServiceClientConfiguration is created if none is given.
 * @param axiosInstance The axios instance that will be used. An interceptor is automatically registered that adds the token to each request.
 * @param authConfig The optional configuration (endpoint and whether local storage should be used). A default configuration will be created if none is given ({@link createDefaultConfig}).
 * @param typeSystem Optional specialized type system that manages AuthenticationInfo and UserInfo. Note: this could/should be removed anytime.
 * @returns A new AuthService.
 */
export function authServiceFactory( axiosInstance: AxiosInstance, authConfig?: AuthServiceClientConfiguration, typeSystem?: IAuthenticationInfoTypeSystem<IUserInfo> ): AuthService {
    if ( !authConfig ) {
        authConfig = createDefaultConfig();
    }
    return new AuthService( authConfig, axiosInstance, typeSystem );
}

/**
 * Provides the necessary providers to configure the NgAuthService. (AXIOS injectionToken, AuthService, HttpClient with AuthInterceptor)
 * @param axiosInstance The axios instance that will be used. An interceptor is automatically registered that adds the token to each request.
 * @param authConfig The optional configuration (endpoint and whether local storage should be used). A default configuration will be created if none is given ({@link createDefaultConfig}).
 * @param typeSystem Optional specialized type system that manages AuthenticationInfo and UserInfo. Note: this could/should be removed anytime.
 * @returns An EnvironmentProviders that can be used to configure the NgAuthService in an app's providers configuration.
 */
export function provideNgAuthService( axiosInstance: AxiosInstance, config?: AuthServiceClientConfiguration, typeSystem?: IAuthenticationInfoTypeSystem<IUserInfo> ): EnvironmentProviders {
    return makeEnvironmentProviders([
        { provide: AXIOS, useValue: axiosInstance },
        { provide: AuthService, useFactory: authServiceFactory, deps: [AXIOS, [new Optional(), config], [new Optional(), typeSystem]] },
        provideHttpClient( withInterceptors( [authInterceptor] ) )
    ]);
}
