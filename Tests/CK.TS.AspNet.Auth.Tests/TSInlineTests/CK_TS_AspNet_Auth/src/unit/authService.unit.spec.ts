import axios, { AxiosResponse, AxiosError } from 'axios';

import { AuthService, IAuthenticationInfo, AuthLevel, IUserInfo, SchemeUsageStatus, WebFrontAuthError } from '@local/ck-gen';
import { IWebFrontAuthResponse } from '@local/ck-gen/CK/AspNet/Auth/index.private';
import { areSchemesEquals, areUserInfoEquals } from '../helpers/test-helpers';
import ResponseBuilder from '../helpers/response-builder';

if( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout(30 * 60 * 1000 ); // 30 minutes

describe('AuthService', function () {
    const axiosInstance = axios.create({ timeout: 1 });
    let requestInterceptorId: number;
    let responseInterceptorId: number;

    let authService!: AuthService;
    const schemeLastUsed = new Date();
    const exp = new Date();
    exp.setHours(exp.getHours() + 6);
    const cexp = new Date();
    cexp.setHours(cexp.getHours() + 3);

    const emptyResponse: IWebFrontAuthResponse = {};
    let serverResponse: IWebFrontAuthResponse = emptyResponse;

    const anonymous: IUserInfo = {
        userId: 0,
        userName: '',
        schemes: []
    };

    async function doLogin(name:string) {
        serverResponse = new ResponseBuilder()
        .withUser({ id: 2, name: 'Alice', schemes: [{ name: name, lastUsed: schemeLastUsed }] })
        .withExpires(exp)
        .withToken('CfDJ8CS62…pLB10X')
        .withRefreshable(true)
        .build();
        await authService.basicLogin('', '');
    }

    beforeAll(function () {
        authService = new AuthService( axiosInstance, { identityEndPoint: {} } );

        requestInterceptorId = axiosInstance.interceptors.request.use((config) => {
            return config;
        });

        responseInterceptorId = axiosInstance.interceptors.response.use((response: AxiosResponse) => {
            return response; // Never occurs
        }, (error: AxiosError) => {
            return Promise.resolve({
                data: serverResponse,
                status: 200,
                statusText: 'Ok',
                headers: {},
                config: error.config
            });
        });
    });

    beforeEach(async function () {
        serverResponse = emptyResponse;
        await authService.logout();
        serverResponse = new ResponseBuilder().withSchemes( ['Basic'] ).build();
        await authService.refresh( false, true );
    });

    afterAll(function () {
        axiosInstance.interceptors.request.eject(requestInterceptorId);
        axiosInstance.interceptors.response.eject(responseInterceptorId);
        authService.close();
    });

    describe('when using localStorage', function() {

        it('JSON.stringify( StdAuthenticationInfo ) is safe (toJSON is implemented on all Std objects).', async function() {

            const nicoleUser = authService.typeSystem.userInfo.create( 3712, 'Nicole', [{name:'Provider', lastUsed: new Date(), status: SchemeUsageStatus.Active}] );
            const nicoleAuth = authService.typeSystem.authenticationInfo.create(nicoleUser,exp,cexp);
            expect( JSON.stringify( nicoleAuth ) ).toBe( JSON.stringify( nicoleAuth ) );

            const user = { id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] };
            serverResponse = new ResponseBuilder()
                .withUser( user )
                .withExpires( exp )
                .withToken('CfDJ8CS62…pLB10X')
                .build();

            await authService.basicLogin('', '');


            const expectedUser = '{"userId":2,"userName":"Alice","schemes":[{"name":"Basic","lastUsed":"'+ schemeLastUsed.toISOString() +'","status":1}]}';
            // Note that criticalExpires being undefined, it is not exported as JSON!
            // This '","criticalExpires":"'+ cexp.toISOString() doesn't appear in result.
            const expected = '{"user":'+expectedUser+',"unsafeUser":'+expectedUser
                             +',"level":2,"expires":"'+ exp.toISOString() +'","deviceId":"","isImpersonated":false'
                             +',"actualUser":'+expectedUser+'}';
            const result = JSON.stringify( authService.authenticationInfo );
            expect( result ).toBe( expected );
        });

        it('it is possible to store a null AuthenticationInfo (and schemes are saved nevertheless).', function() {
            authService.typeSystem.authenticationInfo.saveToLocalStorage( localStorage,
                                                                          'theEndPoint',
                                                                           null,
                                                                           ['Saved','Schemes','even', 'when','null','AuthInfo'] );

            const [restored,schemes] = authService.typeSystem.authenticationInfo.loadFromLocalStorage(localStorage, 'theEndPoint' );
            expect( restored ).toBeNull();
            expect( schemes ).toStrictEqual( ['Saved','Schemes','even', 'when','null','AuthInfo'] );

            const [_,schemes2] = authService.typeSystem.authenticationInfo.loadFromLocalStorage(localStorage, 'theEndPoint', ['Hop'] );
            expect( schemes2 ).toStrictEqual( ['Hop'] );
        });

        it('AuthenticationInfo is restored as unsafe user.', function() {
            const nicoleUser = authService.typeSystem.userInfo.create( 3712, 'Nicole', [{name:'Provider', lastUsed: new Date(), status: SchemeUsageStatus.Active}] );
            const nicoleAuth = authService.typeSystem.authenticationInfo.create(nicoleUser,exp,cexp);

            expect( nicoleAuth.level ).toBe( AuthLevel.Critical );
            authService.typeSystem.authenticationInfo.saveToLocalStorage( localStorage, 'theEndPoint', nicoleAuth );

            const [restored,schemes] = authService.typeSystem.authenticationInfo.loadFromLocalStorage(localStorage, 'theEndPoint', ['Provider']);
            expect( restored ).not.toBeNull();
            expect( restored ).not.toBe( nicoleAuth );

            expect( restored!.level ).toBe( AuthLevel.Unsafe );
            expect( restored!.user ).toStrictEqual( authService.typeSystem.userInfo.anonymous );
            expect( restored!.unsafeUser.userName ).toBe( 'Nicole' );
            expect( areSchemesEquals( restored!.unsafeUser.schemes, nicoleAuth.user.schemes ) ).toBe( true );
        });

        it('AuthenticationInfo and Schemes are stored by end point.', function() {

            const nicoleUser = authService.typeSystem.userInfo.create( 3712, 'Nicole', [{name:'Provider', lastUsed: new Date(), status: SchemeUsageStatus.Active}] );
            const nicoleAuth = authService.typeSystem.authenticationInfo.create(nicoleUser,exp,cexp);
            const momoUser = authService.typeSystem.userInfo.create( 10578, 'Momo', [{name:'Basic', lastUsed: new Date(), status: SchemeUsageStatus.Active}] );
            const momoAuth = authService.typeSystem.authenticationInfo.create(momoUser,exp);

            expect( nicoleAuth.level ).toBe( AuthLevel.Critical );
            authService.typeSystem.authenticationInfo.saveToLocalStorage( localStorage, 'EndPointForNicole', nicoleAuth );
            expect( momoAuth.level ).toBe( AuthLevel.Normal );
            authService.typeSystem.authenticationInfo.saveToLocalStorage( localStorage, 'EndPointForMomo', momoAuth );

            const [rNicole,schemes] = authService.typeSystem.authenticationInfo.loadFromLocalStorage(localStorage, 'EndPointForNicole', ['Another']);
            expect( schemes ).toStrictEqual( ['Another'] );
            expect( rNicole!.level ).toBe( AuthLevel.Unsafe );
            expect( rNicole!.unsafeUser.userName ).toBe( 'Nicole' );
            expect( rNicole!.unsafeUser.schemes[0].status ).toBe( SchemeUsageStatus.Deprecated );

            const [rMomo,_] = authService.typeSystem.authenticationInfo.loadFromLocalStorage(localStorage, 'EndPointForMomo' );
            expect( rMomo!.level ).toBe( AuthLevel.Unsafe );
            expect( rMomo!.unsafeUser.userName ).toBe( 'Momo' );
            expect( rMomo!.unsafeUser.schemes[0].status ).toBe( SchemeUsageStatus.Active );
        });
    });

    describe('when parsing server response', function () {

        it('should parse basicLogin response.', async function () {

            const expectedLoginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [{ name: 'Basic', lastUsed: schemeLastUsed, status: SchemeUsageStatus.Active }]
            }

            serverResponse = new ResponseBuilder()
                .withLoginFailure({ loginFailureCode: 4, loginFailureReason: 'Invalid credentials.' })
                .build();


            await authService.basicLogin('', '');

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, anonymous)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
            expect(authService.token).toBe('');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toEqual(new WebFrontAuthError({
                loginFailureCode: 4,
                loginFailureReason: 'Invalid credentials.'
            }));

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withToken('CfDJ8CS62…pLB10X')
                .build();
            await authService.basicLogin('', '');

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, expectedLoginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Unsafe);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(true)
                .build();
            await authService.basicLogin('', '');

            expect(areUserInfoEquals(authService.authenticationInfo.user, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, expectedLoginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(true);
            expect(authService.lastResult.error).toBeUndefined();
        });

        it('should parse refresh response.', async function () {
            const loginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [{ name: 'Basic', lastUsed: schemeLastUsed, status: SchemeUsageStatus.Active }]
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(true)
                .build();
            await authService.basicLogin('', '');

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(false)
                .withVersion("the version")
                .build();
            await authService.refresh();

            expect(areUserInfoEquals(authService.authenticationInfo.user, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, loginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();
            expect(authService.endPointVersion).toBe( "the version" );

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(false)
                .build();
            await authService.refresh();

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, loginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Unsafe);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();

            serverResponse = emptyResponse;
            await authService.refresh();

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, anonymous)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
            expect(authService.token).toBe('');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();
        });

        it('should parse logout response.', async function () {
            const loginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [{ name: 'Basic', lastUsed: schemeLastUsed, status:SchemeUsageStatus.Active }]
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(true)
                .build();
            await authService.basicLogin('', '');
     
            expect(areUserInfoEquals(authService.authenticationInfo.user, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, loginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(true);
            expect(authService.lastResult.error).toBeUndefined();

            // We set the response for the refresh which is triggered by the logout
            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(false)
                .build();
            await authService.logout();

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, loginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Unsafe);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();

            serverResponse = emptyResponse;
            await authService.logout();

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, anonymous)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
            expect(authService.token).toBe('');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();
        });

        it('should parse unsafeDirectLogin response.', async function () {

            const loginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [{ name: 'Basic', lastUsed: schemeLastUsed, status:SchemeUsageStatus.Active }]
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ8CS62…pLB10X')
                .withRefreshable(false)
                .build();
            await authService.unsafeDirectLogin('', {});

            expect(areUserInfoEquals(authService.authenticationInfo.user, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, loginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, loginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();

            serverResponse = new ResponseBuilder()
                .withError({ errorId: 'System.ArgumentException', errorText: 'Invalid payload.' })
                .build();
            await authService.unsafeDirectLogin('', {});

            expect(areUserInfoEquals(authService.authenticationInfo.user, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, anonymous)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, anonymous)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
            expect(authService.token).toBe('');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toEqual(new WebFrontAuthError({
                errorId: 'System.ArgumentException',
                errorText: 'Invalid payload.'
            }));
        });

        it('should parse impersonate response.', async function () {
            const impersonatedLoginInfo: IUserInfo = {
                userId: 3,
                userName: 'Bob',
                schemes: [{ name: 'Basic', lastUsed: new Date( 98797179 ), status: SchemeUsageStatus.Active }]
            }

            const impersonatorLoginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [{ name: 'Basic', lastUsed: schemeLastUsed, status: SchemeUsageStatus.Active }]
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 3, name: 'Bob', schemes: [{ name: 'Basic', lastUsed: new Date( 98797179 )}] })
                .withActualUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(exp)
                .withToken('CfDJ…s4POjOs')
                .withRefreshable(false)
                .build();
            await authService.impersonate('');

            expect(areUserInfoEquals(authService.authenticationInfo.user, impersonatedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, impersonatedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, impersonatorLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, impersonatorLoginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ…s4POjOs');
            expect(authService.refreshable).toBe(false);
            expect(authService.lastResult.error).toBeUndefined();
        });

        it('should update schemes status.', async function () {

            serverResponse = new ResponseBuilder()
                .withSchemes( ["Basic", "BrandNewProvider"] )
                .build();
            await authService.refresh( false, true );

            expect( authService.availableSchemes ).toEqual( ["Basic", "BrandNewProvider"] );

            const expectedLoginInfo: IUserInfo = {
                userId: 2,
                userName: 'Alice',
                schemes: [
                    { name: 'Basic', lastUsed: schemeLastUsed, status: SchemeUsageStatus.Active },
                    { name: 'Wanadoo', lastUsed: new Date(1999,12,14), status: SchemeUsageStatus.Deprecated },
                    { name: 'BrandNewProvider', lastUsed: new Date(0), status: SchemeUsageStatus.Unused }
                ]
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes:
                            [
                                { name: 'Basic', lastUsed: schemeLastUsed },
                                { name: 'Wanadoo', lastUsed: new Date(1999,12,14) }
                        ] })
                .withToken('CfDJ8CS62…pLB10X')
                .withExpires(exp)
                .build();
            await authService.basicLogin('', '');

            expect(areUserInfoEquals(authService.authenticationInfo.user, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeUser, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.actualUser, expectedLoginInfo)).toBe(true);
            expect(areUserInfoEquals(authService.authenticationInfo.unsafeActualUser, expectedLoginInfo)).toBe(true);
            expect(authService.authenticationInfo.level).toBe(AuthLevel.Normal);
            expect(authService.token).toBe('CfDJ8CS62…pLB10X');
            expect(authService.lastResult.error).toBeUndefined();
        });

   });

    describe('when authentication info changes', function () {

        it('should call OnChange().', async function () {
            let authenticationInfo: IAuthenticationInfo = authService.authenticationInfo;
            let token: string = '';

            const updateAuthenticationInfo = () => authenticationInfo = authService.authenticationInfo;
            const updateToken = () => token = authService.token;
            authService.addOnChange(updateAuthenticationInfo);
            authService.addOnChange(updateToken);

            await doLogin( 'Alice' );

            expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(false);
            expect(token).not.toEqual('');

            serverResponse = emptyResponse;
            await authService.logout();

            expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(true);
            expect(token).toBe('');

            authService.removeOnChange(updateAuthenticationInfo);

            await doLogin( 'Alice' );

            expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(true);
            expect(token).not.toEqual('');
        });

        it('should contains the source as an Event parameter.', async function () {
            let eventSource: AuthService|null = null;
            const assertEventSource = (source: AuthService) => eventSource = source;
            authService.addOnChange(assertEventSource);

            await doLogin( 'Alice' );

            expect(eventSource).toEqual(authService);
        });

        /**
         * NOTE
         * Do not use async here. Otherwise a "method is overspecified" error will be throw.
         * This error is thrown whenever a function returns a promise and uses the done callback.
         * Since this test relies on events' callback, we call done() after the last expectation.
         */
        it('should start expires and critical expires respective timers.', function (done) {
            const now = new Date();
            const criticalExpires = new Date( now.getTime() + 100 );
            const expires = new Date( criticalExpires.getTime() + 100 );

            const assertCriticalExpiresDemoted = (source: AuthService) => {
                expect(source.authenticationInfo.level === AuthLevel.Normal);
                source.removeOnChange(assertCriticalExpiresDemoted);
                source.addOnChange(assertExpiresDemoted);
            }

            const assertExpiresDemoted = (source: AuthService) => {
                expect(source.authenticationInfo.level === AuthLevel.Unsafe);
                source.removeOnChange(assertExpiresDemoted);
                done();
            }

            serverResponse = new ResponseBuilder()
                .withUser({ id: 2, name: 'Alice', schemes: [{ name: 'Basic', lastUsed: schemeLastUsed }] })
                .withExpires(expires)
                .withCriticalExpires(criticalExpires)
                .withToken('Cf0DEq...Fd10xRD')
                .withRefreshable(false)
                .build();

            authService.basicLogin('', '').then(_ => {
                expect(authService.authenticationInfo.level).toBe(AuthLevel.Critical);
                authService.addOnChange(assertCriticalExpiresDemoted);
            });
        });

        it('should call OnChange() for every subscribed functions.', async function() {
            const booleanArray: boolean[] = [false, false, false];
            const functionArray: (() => void)[] = [];

            for(let i=0; i<booleanArray.length; ++i) functionArray.push(function() { booleanArray[i] = true; });
            functionArray.forEach(func => authService.addOnChange(func));

            await doLogin( 'Alice' );
            booleanArray.forEach(b => expect(b).toBe(true));
            // Clears the array.
            for(let i=0; i<booleanArray.length; ++i) booleanArray[i] = false;

            await authService.logout();
            booleanArray.forEach(b => expect(b).toBe(true));
            // Clears the array.
            for(let i=0; i<booleanArray.length; ++i) booleanArray[i] = false;

            functionArray.forEach(func => authService.removeOnChange(func));
            await doLogin( 'Alice' );
            booleanArray.forEach(b => expect(b).toBe(false));
        });

    });

});
