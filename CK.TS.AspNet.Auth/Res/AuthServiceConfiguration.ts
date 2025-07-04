import { IAuthServiceConfiguration, IEndPoint } from './authService.model.public';

export class AuthServiceConfiguration {
    readonly #identityServerEndPoint: string;

    /** When defined, the local storage is available. */
    public readonly localStorage?: Storage;

    /** Gets the end point address. */
    public get webFrontAuthEndPoint(): string { return this.#identityServerEndPoint; }

    constructor(config?: IAuthServiceConfiguration) {
        config = config || {};
        if (!config.identityEndPoint) {
            if (typeof (window) === 'undefined') {
                throw new Error("IAuthServiceConfiguration required.");
            }
            this.#identityServerEndPoint = window.location.origin + '/';
        }
        else if (typeof config.identityEndPoint === "string") {
            this.#identityServerEndPoint = config.identityEndPoint;
            if (!this.#identityServerEndPoint.endsWith('/')) {
                this.#identityServerEndPoint += '/';
            }
        }
        else {
            this.#identityServerEndPoint = AuthServiceConfiguration.getUrlFromEndPoint(config.identityEndPoint);
        }
        if (config.useLocalStorage === undefined || config.useLocalStorage) {
            this.localStorage = this.getAvailableStorage('localStorage');
        }
    }

    private static getUrlFromEndPoint(endPoint: IEndPoint): string {
        if (!endPoint.hostname) { return '/'; }
        const isHttps = !endPoint.disableSsl;
        let hostnameAndPort = endPoint.hostname;
        if (endPoint.port !== undefined && endPoint.port !== null && !this.isDefaultPort(isHttps, endPoint.port)) {
            hostnameAndPort += ':' + endPoint.port;
        }
        return `${isHttps ? 'https' : 'http'}://${hostnameAndPort}/`;
    }

    private static isDefaultPort(isHttps: boolean, portNumber: number): boolean {
        return isHttps ? portNumber === 443 : portNumber === 80;
    }

    /**
     * Returns the localStorage or null if it is unavailable.
     * Reference: https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API/Using_the_Web_Storage_API#Feature-detecting_localStorage
     * @param storageType Storage type, either 'localStorage' or 'sessionStorage'
     */
    private getAvailableStorage(storageType: 'localStorage' | 'sessionStorage'): Storage | undefined {
        let storage: Storage | undefined = undefined;
        try {
            if (typeof (window) !== 'undefined') {
                storage = window[storageType];
                const key = '__storage_test__';
                storage.setItem(key, key);
                storage.removeItem(key);
            }
        }
        catch (e) {
            const isAvailable = e instanceof DOMException
                && e.name === 'QuotaExceededError'
                // acknowledge QuotaExceededError only if there's something already stored
                && storage
                && storage!.length !== 0;

            if (!isAvailable) storage = undefined;
        }
        return storage;
    }

}
