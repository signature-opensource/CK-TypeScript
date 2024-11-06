import { AxiosRequestConfig, AxiosError, AxiosInstance, InternalAxiosRequestConfig, AxiosHeaders } from 'axios';

import { IWebFrontAuthResponse, AuthServiceConfiguration } from './index.private';
import { AuthLevel, IAuthenticationInfo, IUserInfo, IAuthServiceConfiguration, IWebFrontAuthError, ILastResult } from './authService.model.public';
import { WebFrontAuthError } from './authService.model.extension';
import { IAuthenticationInfoTypeSystem, IAuthenticationInfoImpl } from './type-system/type-system.model';
import { StdAuthenticationTypeSystem } from './type-system';
import { PopupDescriptor } from './PopupDescriptor';

export class AuthService<T extends IUserInfo = IUserInfo> {

    #authenticationInfo: IAuthenticationInfoImpl<T>;
    #token: string;
    #rememberMe: boolean;
    #refreshable: boolean;
    #availableSchemes: ReadonlyArray<string>;
    #endPointVersion: string;
    #configuration: AuthServiceConfiguration;
    #currentError?: IWebFrontAuthError;
    #lastResult: ILastResult;

    #axiosInstance: AxiosInstance;
    #interceptor: number;
    #typeSystem: IAuthenticationInfoTypeSystem<T>;
    #popupDescriptor: PopupDescriptor | undefined;
    #expTimer: ReturnType<typeof setTimeout> | undefined;
    #cexpTimer: ReturnType<typeof setTimeout> | undefined;
    #isInitialized: Promise<void>;
    #resolveInitialized: () => void;
    #subscribers: Set<(eventSource: AuthService) => void>;
    #onMessage?: (this: Window, ev: MessageEvent) => void;
    #closed: boolean;
    #popupWin: Window | undefined;

    /** Gets the current authentication information. */
    public get authenticationInfo(): IAuthenticationInfo<T> { return this.#authenticationInfo; }
    /** Gets the current authentication token. This is the empty string when there is currently no authentication. */
    public get token(): string { return this.#token; }
    /** Gets whether this service will automatically refreshes the authentication. */
    public get refreshable(): boolean { return this.#refreshable; }
    /** Gets whether the current authentication should be memorized or considered a transient one. */
    public get rememberMe(): boolean { return this.#rememberMe; }
    /** Gets the available authentication schemes names. */
    public get availableSchemes(): ReadonlyArray<string> { return this.#availableSchemes; }
    /** Gets the Authentication server version.*/
    public get endPointVersion(): string { return this.#endPointVersion; }
    /** Gets the last server result with the last server data and error if any. */
    public get lastResult(): ILastResult { return this.#lastResult; }
    /** Gets the TypeSystem that manages AuthenticationInfo and UserInfo.*/
    public get typeSystem(): IAuthenticationInfoTypeSystem<T> { return this.#typeSystem; }
    /** Gets whether this AuthService is closed: no method should be called anymore. */
    public get isClosed(): boolean { return this.#closed; }

    /** 
     * A promise resolved when this AuthService has been initialized: refresh has been called at least once. 
     * This is awaitable and should be used when initialization is handled by out-of-reach code. 
     * This doesn't capture the potential error: the error is for the refresh caller and this {@link authenticationInfo}
     * is anonymous (and {@link lastResult} can be used if needed).
     */
    public get isInitialized(): Promise<void> { return this.#isInitialized; }

    public get popupDescriptor(): PopupDescriptor {
        if (!this.#popupDescriptor) { this.#popupDescriptor = new PopupDescriptor(); }
        return this.#popupDescriptor;
    }
    public set popupDescriptor(popupDescriptor: PopupDescriptor) {
        if (popupDescriptor) { this.#popupDescriptor = popupDescriptor; }
    };

    //#region constructor

    /**
     * Instantiates a new AuthService. Note that the 'refresh' method must be called.
     * It is simpler and safer to use the factory method 'createAsync' that fully initializes
     * the AuthService before returning it.
     * @param axiosInstance The axios instance that will be used. An interceptor is automatically registered that adds the token to each request (under control of {@link shouldSetToken}).
     * @param configuration Optional configuration (server url and whether local storage should be used). Local storage is used by default.
     * @param typeSystem Optional specialized type system that manages AuthenticationInfo and UserInfo.
     */
    constructor(axiosInstance: AxiosInstance,
        configuration?: IAuthServiceConfiguration,
        typeSystem?: IAuthenticationInfoTypeSystem<T>) {
        if (!axiosInstance) { throw new Error('AxiosInstance must be defined.'); }
        this.#axiosInstance = axiosInstance;
        this.#configuration = new AuthServiceConfiguration(configuration);

        this.#interceptor = this.#axiosInstance.interceptors.request.use(this.onIntercept());

        // Intermediate r and guard is to satisfy TypeScript definite assignment rule.
        var r;
        this.#isInitialized = new Promise(resolve => r = resolve);
        if (typeof r === "undefined") throw new Error();
        this.#resolveInitialized = r;

        this.#closed = false;
        this.#typeSystem = typeSystem ? typeSystem : new StdAuthenticationTypeSystem() as any;
        this.#endPointVersion = '';
        this.#availableSchemes = [];
        this.#subscribers = new Set<() => void>();
        this.#expTimer = undefined;
        this.#cexpTimer = undefined;
        this.#authenticationInfo = this.#typeSystem.authenticationInfo.none;
        this.#refreshable = false;
        this.#rememberMe = false;
        this.#token = '';
        this.#lastResult = { serverData: undefined, error: undefined };
        this.#popupDescriptor = undefined;

        if (!(typeof window === 'undefined')) {
            this.#onMessage = this.onMessage();
            window.addEventListener('message', this.#onMessage, false);
        }

        this.localDisconnect();
    }

    /**
     * Factory method that instantiates a new AuthService and calls the asynchronous refresh before
     * returning the service.
     * @param axiosInstance The axios instance that will be used. An interceptor is automatically registered that adds the token to each request (under control of {@link shouldSetToken}).
     * @param configuration Optional configuration (endpoint and whether local storage should be used). Local storage is used by default.
     * @param typeSystem Optional specialized type system that manages AuthenticationInfo and UserInfo.
     * @param throwOnError True to throw if any error occurred (server cannot be reached, protocol error, etc.)
     * @returns A new AuthService that may have a currentError if parameter throwOnError is false (the default).
     */
    public static async createAsync<T extends IUserInfo = IUserInfo>(axiosInstance: AxiosInstance,
        configuration?: IAuthServiceConfiguration,
        typeSystem?: IAuthenticationInfoTypeSystem<T>,
        throwOnError: boolean = false): Promise<AuthService> {
        const authService = new AuthService<T>(axiosInstance, configuration, typeSystem);
        await authService.refresh(true);
        if (authService.lastResult.error) {
            console.error(
                'Error while initalizing new AuthService.',
                authService.lastResult.error.errorId,
                authService.lastResult.error.errorText
            );

            if (throwOnError) {
                authService.close();
                throw new Error('Unable to initalize a new AuthService. See logs.');
            }
        }
        return authService;
    }

    /**
     * Closes this AuthService, ejecting the axios interceptor, the window message hook
     * and clearing onChange subscriptions.
     * This can be called multiple times.
     */
    public close(): void {
        if (!this.#closed) {
            this.#closed = true;
            this.clearTimeouts();
            this.#subscribers.clear();
            this.#axiosInstance.interceptors.request.eject(this.#interceptor);
            if (this.#onMessage) window.removeEventListener('message', this.#onMessage, false);
        }
    }

    private checkClosed() { if (this.#closed) throw new Error("This AuthService has been closed."); }

    //#endregion

    //#region events

    private readonly maxTimeout: number = 2147483647;

    private static getSafeTimeDifference(timeDifference: number): number {
        return timeDifference * 2 / 3;
    }

    private setExpirationTimeout(): void {
        const timeDifference = this.#authenticationInfo.expires!.getTime() - Date.now();

        if (timeDifference > this.maxTimeout) {
            this.#expTimer = setTimeout(this.setExpirationTimeout, this.maxTimeout);
        } else {
            this.#expTimer = setTimeout(() => {
                if (this.#refreshable) {
                    this.refresh();
                } else {
                    this.#authenticationInfo = this.#authenticationInfo.setExpires(undefined);
                    this.onChange();
                }
            }, AuthService.getSafeTimeDifference(timeDifference));
        }
    }

    private setCriticalExpirationTimeout(): void {
        const timeDifference = this.#authenticationInfo.criticalExpires!.getTime() - Date.now();

        if (timeDifference > this.maxTimeout) {
            this.#cexpTimer = setTimeout(this.setCriticalExpirationTimeout, this.maxTimeout);
        } else {
            this.#cexpTimer = setTimeout(() => {
                if (this.#refreshable) {
                    this.refresh();
                } else {
                    this.#authenticationInfo = this.#authenticationInfo.setCriticalExpires();
                    this.onChange();
                }
            }, AuthService.getSafeTimeDifference(timeDifference));
        }
    }

    private clearTimeouts(): void {
        if (this.#expTimer) {
            clearTimeout(this.#expTimer);
            this.#expTimer = undefined;
        }
        if (this.#cexpTimer) {
            clearTimeout(this.#cexpTimer);
            this.#cexpTimer = undefined;
        }
    }

    private onIntercept(): (value: InternalAxiosRequestConfig<undefined>) => InternalAxiosRequestConfig<undefined> {
        return (config: InternalAxiosRequestConfig<undefined>) => {
            if (this.#token
                && this.shouldSetToken(config.url!)) {
                config.headers = config.headers ?? new AxiosHeaders();
                Object.assign(config.headers, { Authorization: `Bearer ${this.#token}` });
            }
            return config;
        };
    }

    private onMessage(): (this: Window, ev: MessageEvent) => void {
        return (messageEvent) => {
            if (messageEvent.data.WFA === 'WFA') {
                // We are the only one (in terms of domain origin) that can receive this message since we call the server with this
                // document.location.origin and this has been used by the postMessage targetOrigin parameter.
                // Still, anybody may send us such a message so here we check the other way around before accepting the message: the
                // sender must origin from our endPoint.
                const origin = messageEvent.origin + '/';
                if (origin !== this.#configuration.webFrontAuthEndPoint) {
                    throw new Error(`Incorrect origin in postMessage. Expected '${this.#configuration.webFrontAuthEndPoint}', but was '${origin}'`);
                }
                this.handleServerResponse(messageEvent.data.data);
            }
        };
    }

    //#endregion

    //#region request handling

    /** helper that assigns a new LastResult instance. Must be called before onChange(). */
    private setLastResult(data?: IWebFrontAuthResponse): void {
        this.#lastResult = { error: this.#currentError, serverData: data ? data.userData : undefined };
    }

    /**
     * When the server cannot be reached, there is no point to call localDisconnect(): we'd better wait its availability.
     * Actually, when we are refreshing and the current authentication is None and the LocalStorage
     * is enabled, we restore the available schemes and the Authentication info from the local storage.
     *
     * When the server returns an HTTP error, whether we should lose the local authentication
     * (i.e call localDisconnect) is questionable...
     * 4XX (Client errors) means that someting's wrong in the payload we sent (an invalid user data for instance): such a client
     * error should not lose the current authentication.
     * Same for 5XX (server errors), it may be a bad handling of a Client error, or the server is temporary unavailable.
     * However, a 1XX (information), a 3XX (redirection) are NOT in the protocol (and browsers silently follow them anyway).
     * This is weird. Just like if we receive an out-of-band status (600).
     * But how does this impact our current local authentication information?
     * Disconnecting the user here doesn't seem to provide any benefits...
     *
     * The only thing we can do on error is checking the current authentication expiration to update it
     * regardless of the timeouts (since they have been disabled) and reenable them.
     *
     * When the server returned a 200, then the repsonse is applied, no matter what: it may result in a
     * localDisconnect and this is fine.
     *
     * Error handling is never easy...
     *
     * @param entryPoint
     * @param requestOptions
     * @param skipResponseHandling
     */
    private async sendRequest(entryPoint: 'basicLogin' | 'unsafeDirectLogin' | 'refresh' | 'impersonate' | 'logout' | 'startLogin',
        requestOptions: { body?: object, queries?: Array<string | { key: string, value: string }> },
        skipResponseHandling: boolean = false): Promise<void> {
        try {
            this.clearTimeouts(); // We clear timeouts beforehand to avoid concurent requests

            const query = this.buildQueryString(requestOptions.queries);
            const response = await this.#axiosInstance.post<IWebFrontAuthResponse>(
                `${this.#configuration.webFrontAuthEndPoint}.webfront/c/${entryPoint}${query}`,
                !!requestOptions.body ? JSON.stringify(requestOptions.body) : {},
                { withCredentials: true });

            const status = response.status;
            if (status === 200) {
                if (!skipResponseHandling) {
                    this.handleServerResponse(response.data);
                }
                // Done (handleServerResponse did its job or there is nothing to do).
                return;
            }
            // Sets the current error (weird success status).
            this.#currentError = new WebFrontAuthError({
                errorId: `HTTP.Status.${status}`,
                errorText: 'Unhandled success status'
            });
            this.setLastResult(response.data);
        } catch (error) {

            // This should not happen too often nor contain dangerous secrets...
            console.log('Exception while sending ' + entryPoint + ' request.', error);

            const axiosError = error as AxiosError;
            if (!(axiosError && axiosError.response)) {
                // Connection issue (no axios response): 408 is Request Timeout.
                this.#currentError = new WebFrontAuthError({
                    errorId: 'HTTP.Status.408',
                    errorText: 'No connection could be made'
                });
                // If we are refreshing and there is no connection to the server, and the current level is None,
                /// we challenge the local storage and try to restore the available schemes and (unsafe) authentication.
                if (this.#authenticationInfo.level == AuthLevel.None
                    && entryPoint === 'refresh'
                    && this.#configuration.localStorage) {

                    const [auth, schemes] = this.#typeSystem.authenticationInfo.loadFromLocalStorage(this.#configuration.localStorage,
                        this.#configuration.webFrontAuthEndPoint,
                        this.#availableSchemes);
                    this.#availableSchemes = schemes;
                    if (auth) {
                        // This sets the (unsafe, no expiration) auth and trigger the onChange: we are done.
                        this.localDisconnect(auth);
                        return;
                    }
                }
            }
            else {
                // Sets the current error.
                const errorResponse = axiosError.response;
                const data = errorResponse.data as IWebFrontAuthResponse || {};

                this.#currentError = new WebFrontAuthError({
                    errorId: data.errorId || `HTTP.Status.${errorResponse.status}`,
                    errorText: data.errorText || 'Server response error'
                });
            }
        }
        // There has been an error.
        this.setLastResult();
        const a = this.#authenticationInfo.checkExpiration();
        if (a != this.#authenticationInfo) {
            this.#authenticationInfo = a;
            this.onNewAuthenticationInfo();
        }
        else {
            this.onChange();
        }
    }

    private handleServerResponse(r: IWebFrontAuthResponse): void {
        if (!r) {
            this.localDisconnect();
            return;
        }

        this.#currentError = undefined;

        // Checking the version first.
        if (r.version) {
            // Only refresh returns the version.
            this.#endPointVersion = r.version;
        }
        if (r.schemes) { this.#availableSchemes = r.schemes; }

        if (r.loginFailureCode && r.loginFailureReason) {
            this.#currentError = new WebFrontAuthError({
                loginFailureCode: r.loginFailureCode,
                loginFailureReason: r.loginFailureReason
            });
        }

        if (r.errorId && r.errorText) {
            this.#currentError = new WebFrontAuthError({
                errorId: r.errorId,
                errorText: r.errorText
            });
        }

        if (this.#currentError) {
            this.localDisconnect(undefined, r);
            return;
        }


        if (!r.info) {
            this.localDisconnect(undefined, r);
            return;
        }

        this.#token = r.token ? r.token : '';
        this.#refreshable = r.refreshable ? r.refreshable : false;
        this.#rememberMe = r.rememberMe ? r.rememberMe : false;

        const info = this.#typeSystem.authenticationInfo.fromServerResponse(r.info, this.#availableSchemes);
        this.#authenticationInfo = info !== null ? info : this.#typeSystem.authenticationInfo.none.setDeviceId(this.#authenticationInfo.deviceId);

        this.setLastResult(r);
        this.onNewAuthenticationInfo();
    }

    private onNewAuthenticationInfo() {
        if (this.#authenticationInfo.expires) {
            this.setExpirationTimeout();
            if (this.#authenticationInfo.criticalExpires) { this.setCriticalExpirationTimeout(); }
        }
        if (this.#configuration.localStorage) {
            this.#typeSystem.authenticationInfo.saveToLocalStorage(
                this.#configuration.localStorage,
                this.#configuration.webFrontAuthEndPoint,
                this.#authenticationInfo,
                this.#availableSchemes);
        }
        this.onChange();
    }

    private localDisconnect(fromLocalStorage?: IAuthenticationInfoImpl<T>, data?: IWebFrontAuthResponse): void {
        // Keep (and applies) the current rememberMe configuration: this is the "local" disconnect.
        this.#token = '';
        this.#refreshable = false;
        if (fromLocalStorage) this.#authenticationInfo = fromLocalStorage;
        else if (this.#rememberMe) {
            this.#authenticationInfo = this.#authenticationInfo.setExpires();
        }
        else {
            this.#authenticationInfo = this.#typeSystem.authenticationInfo.none.setDeviceId(this.#authenticationInfo.deviceId);
        }
        this.clearTimeouts();
        this.setLastResult(data);
        this.onChange();
    }

    //#endregion

    //#region webfrontauth protocol

    /**
     * Triggers a basic login with a user name and password.
     * @param userName The user name.
     * @param password The password to use.
     * @param rememberMe False to avoid any memorization (a session cookie is used). When undefined, the current rememberMe value is used.
     * @param impersonateActualUser True to impersonate the current actual user if any. Defaults to false.
     * @param serverData Optional Server data is sent to the backend: the backend can use it to drive its behavior
     * and can modify this dictionary of nullable strings.
     */
    public async basicLogin(userName: string,
        password: string,
        rememberMe?: boolean,
        impersonateActualUser?: boolean,
        serverData?: { [index: string]: string | null }): Promise<void> {
        this.checkClosed();
        if (rememberMe === undefined) rememberMe = this.#rememberMe;
        await this.sendRequest('basicLogin', { body: { userName, password, userData: serverData, rememberMe, impersonateActualUser } });
    }

    /**
     * Triggers a direct, unsafe login (this has to be explicitly allowed by the server).
     * @param provider The authentication scheme to use.
     * @param rememberMe False to avoid any memorization (a session cookie is used). When undefined, the current rememberMe value is used.
     * @param payload The object payload that contain any information required to authenticate with the scheme.
     * @param impersonateActualUser True to impersonate the current actual user if any. Defaults to false.
     * @param serverData Optional Server data is sent to the backend: the backend can use it to drive its behavior
     * and can modify this dictionary of nullable strings.
     */
    public async unsafeDirectLogin(provider: string, payload: object, rememberMe?: boolean, impersonateActualUser?: boolean, serverData?: { [index: string]: string | null }): Promise<void> {
        this.checkClosed();
        if (rememberMe === undefined) rememberMe = this.#rememberMe;
        await this.sendRequest('unsafeDirectLogin', { body: { provider, payload, userData: serverData, rememberMe, impersonateActualUser } });
    }

    /**
     * Refreshes the current authentication.
     * @param callBackend True to trigger a refresh of the authentication on the backend: the backend IWebFrontAuthLoginService.RefreshAuthenticationInfoAsync
     * is called so that user information (the user name) can be updated.
     *  When false (the default), the server trusts the current authentication and may renew its expiration (according to its configuration).
     * @param requestSchemes True to force a refresh of the availableSchemes (this is automatically
     * true when current availableSchemes is empty).
     * @param requestVersion True to force a refresh of the version (this is automatically
     * true when current endPointVersion is the empty string).
     */
    public async refresh(callBackend: boolean = false, requestSchemes: boolean = false, requestVersion: boolean = false): Promise<void> {
        this.checkClosed();
        // No need to encodeURIComponent these parameters.
        const queries: string[] = [];
        if (callBackend) { queries.push('callBackend'); }
        if (requestSchemes || this.#availableSchemes.length === 0) { queries.push('schemes'); }
        if (requestVersion || this.#endPointVersion === '') { queries.push('version'); }
        await this.sendRequest('refresh', { queries });

        this.#resolveInitialized();
    }

    /**
     * Request an impersonation to a user. This may be honored or not by the server.
     * This is always successful when the user is the actual user: the impersonation is cleared.
     * @param user The user name or identifier into whom the currently authenticated user wants to be impersonated.
     */
    public async impersonate(user: string | number): Promise<void> {
        this.checkClosed();
        const requestOptions = { body: (typeof user === 'string') ? { userName: user } : { userId: user } };
        await this.sendRequest('impersonate', requestOptions);
    }

    /**
     * Revokes the current authentication.
     * This removes any memory of the current authentication.
     */
    public async logout(): Promise<void> {
        this.checkClosed();
        this.#token = '';
        await this.sendRequest('logout', {}, /* skipResponseHandling */ true);
        if (this.#configuration.localStorage) {
            this.#typeSystem.authenticationInfo.saveToLocalStorage(
                this.#configuration.localStorage,
                this.#configuration.webFrontAuthEndPoint,
                null,
                [])
        }
        await this.refresh();
    }

    /**
    * Starts an inline login with the provided scheme. Local context is lost since the process will go through one or more pages
    * before redirecting to the provided return url.
    * @param provider The authentication scheme to use.
    * @param returnUrl The final return url. Must starts with one of the configured AllowedReturnUrls.
    * @param rememberMe False to avoid any memorization (a session cookie is used). When undefined, the current rememberMe value is used.
    * @param impersonateActualUser True to impersonate the current actual user if any. Defaults to false.
    * @param serverData Optional Server data is sent to the backend: the backend can use it to drive its behavior
    * and can modify this dictionary of nullable strings.
    */
    public async startInlineLogin(scheme: string, returnUrl: string, rememberMe?: boolean, impersonateActualUser?: boolean, serverData?: { [index: string]: string | null }): Promise<void> {
        this.checkClosed();
        if (!returnUrl) { throw new Error('returnUrl must be defined.'); }
        if (!(returnUrl.startsWith('http://') || returnUrl.startsWith('https://'))) {
            if (returnUrl.charAt(0) !== '/') { returnUrl = '/' + returnUrl; }
            returnUrl = document.location.origin + returnUrl;
        }
        const params = [
            { key: 'returnUrl', value: encodeURIComponent(returnUrl) },
            rememberMe ? 'rememberMe' : '',
            impersonateActualUser ? 'impersonateActualUser' : ''
        ];
        document.location.href = this.buildStartLoginUrl(scheme, params, serverData);
    }

    /**
    * Starts a login process in a popup window: this is the preferred way to do.
    * @param provider The authentication scheme to use.
    * @param rememberMe False to avoid any memorization (a session cookie is used). When undefined, the current rememberMe value is used.
    * @param impersonateActualUser True to impersonate the current actual user if any. Defaults to false.
    * @param serverData Optional Server data is sent to the backend: the backend can use it to drive its behavior
    * and can modify this dictionary of nullable strings.
    */
    public async startPopupLogin(scheme: string,
        rememberMe?: boolean,
        impersonateActualUser?: boolean,
        serverData?: { [index: string]: string | null }): Promise<void> {
        this.checkClosed();
        if (rememberMe === undefined) rememberMe = this.#rememberMe;
        if (scheme === 'Basic') {
            const popup = this.ensurePopup('about:blank');
            popup.document.write(this.popupDescriptor.generateBasicHtml(rememberMe));
            const onClick = async () => {

                const usernameInput = popup!.document.getElementById('username-input') as HTMLInputElement;
                const passwordInput = popup!.document.getElementById('password-input') as HTMLInputElement;
                const rememberMeInput = popup!.document.getElementById('remember-me-input') as HTMLInputElement;
                const errorDiv = popup!.document.getElementById('error-div') as HTMLInputElement;
                const loginData = { username: usernameInput.value, password: passwordInput.value, rememberMe: rememberMeInput.checked };

                if (!(loginData.username && loginData.password)) {
                    errorDiv.innerHTML = this.popupDescriptor.basicMissingCredentialsError;
                    errorDiv.style.display = 'block';
                } else {
                    await this.basicLogin(loginData.username, loginData.password, loginData.rememberMe, false, serverData);

                    if (this.authenticationInfo.level >= AuthLevel.Normal) {
                        popup!.close();
                    } else {
                        errorDiv.innerHTML = this.popupDescriptor.basicInvalidCredentialsError;
                        errorDiv.style.display = 'block';
                    }
                }
            }
            const eOnClick = popup.document.getElementById('submit-button');
            if (eOnClick == null) throw new Error("Unable to find required 'submit-button' element.");
            eOnClick.onclick = (async () => await onClick());
        }
        else {
            const data = [
                { key: 'callerOrigin', value: encodeURIComponent(document.location.origin) },
                rememberMe ? 'rememberMe' : '',
                impersonateActualUser ? 'impersonateActualUser' : ''
            ];
            this.ensurePopup(this.buildStartLoginUrl(scheme, data, serverData));
        }

    }
    private ensurePopup(url: string): Window {
        if (!this.#popupWin || this.#popupWin.closed) {
            this.#popupWin = window.open(url, this.popupDescriptor.popupTitle, this.popupDescriptor.features)!;
            if (this.#popupWin === null) throw new Error("Unable to open popup window.");
        }
        else {
            this.#popupWin.location.href = url;
            this.#popupWin.focus();
        }
        return this.#popupWin;
    }

    /**
     * Checks whether calling the provided url requires the header bearer token to be added or not.
     * Currently, only urls from the authentication endpoint should add the authentication token.
     * The url must exactly starts with the authentication backend address.
     * In the future, this may be extended to support other secure endpoints if needed.
     * @param url The url to be checked.
     */
    public shouldSetToken(url: string): boolean {
        this.checkClosed();
        if (!url) throw new Error("Url should not be null or undefined")
        if (this.#token === "") return false;
        return url.startsWith(this.#configuration.webFrontAuthEndPoint);
    }

    //#endregion

    //#region onChange

    private onChange(): void {
        this.#subscribers.forEach(func => func(this));
    }

    /**
     * Registers a callback function that will be called on any change.
     * @param func A callback function that will be called whenever something changes.
     */
    public addOnChange(func: (eventSource: AuthService) => void): void {
        this.checkClosed();
        if (func !== undefined && func !== null) { this.#subscribers.add(func); }
    }

    /**
     * Unregister a previously registered callback.
     * @param func The callback function to remove.
     * @returns True if the callback has been found and removed, false otherwise.
     */
    public removeOnChange(func: (eventSource: AuthService) => void): boolean {
        this.checkClosed();
        return this.#subscribers.delete(func);
    }
    //#endregion

    private buildQueryString(params?: Array<string | { key: string, value: string | null }>, scheme?: string): string {
        let query = params && params.length
            ? `?${params.map(q => typeof (q) === "string"
                ? q
                : q.value === null
                    ? q.key
                    : `${q.key}=${q.value}`)
                .join('&')}`
            : '';
        let schemeParam = scheme ? `scheme=${encodeURIComponent(scheme)}` : '';
        if (query && schemeParam) {
            query += `&${schemeParam}`;
        }
        else if (schemeParam) {
            query += `?${schemeParam}`;
        }
        return query;
    }

    private buildStartLoginUrl(scheme: string,
        params: Array<string | { key: string, value: string | null }>,
        serverData?: { [index: string]: string | null }
    ): string {
        if (serverData) {
            Object.keys(serverData).forEach(i => params.push({ key: encodeURIComponent(i), value: this.encData(serverData[i]) }));
        }
        return `${this.#configuration.webFrontAuthEndPoint}.webfront/c/startLogin${this.buildQueryString(params, scheme)}`;
    }
    private encData(v: string | null): string | null {
        return v === null ? null : encodeURIComponent(v);
    }

}
