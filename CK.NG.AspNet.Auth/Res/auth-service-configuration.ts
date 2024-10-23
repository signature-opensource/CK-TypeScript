import { IEndPoint, IAuthServiceConfiguration } from '@local/ck-gen';

export class AuthServiceClientConfiguration implements IAuthServiceConfiguration {
    public readonly identityEndPoint: IEndPoint;
    public readonly loginPath: string;
    public readonly useLocalStorage?: boolean;

    /**
     * Creates an instance of AuthServiceClientConfiguration using the provided login route and endpoint.
     * @param identityEndPoint The identity endpoint to use in WebFrontAuth
     * @param [loginPath='/login'] The route path WebFrontAuth should redirect to when authentication is required.
     * @param
     */
    constructor( identityEndPoint: IEndPoint, loginPath: string = '/login', useLocalStorage?: boolean ) {
        this.identityEndPoint = identityEndPoint;
        this.loginPath = loginPath;
        this.useLocalStorage = useLocalStorage;
    }
}

/**
 * Creates an instance of AuthServiceClientConfiguration using the specified login route,
 * and the current host as identity endpoint.
 *
 * @export
 * @param [loginPath='/login'] The route path WebFrontAuth should redirect to when authentication is required.
 */
export function createDefaultConfig( loginPath: string = '/login', useLocalStorage?: boolean ): AuthServiceClientConfiguration {
    const isHttps = window.location.protocol.toLowerCase() === 'https:';
    const identityEndPoint: IEndPoint = {
        hostname: window.location.hostname,
        port: window.location.port ? Number( window.location.port ) : undefined,
        disableSsl: !isHttps
    };

    return new AuthServiceClientConfiguration( identityEndPoint, loginPath, useLocalStorage );
}
