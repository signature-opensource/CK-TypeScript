import axios from 'axios';

import { AuthService, IAuthenticationInfo, AuthLevel, IUserInfo } from '@local/ck-gen';
import { areUserInfoEquals, areAuthenticationInfoEquals } from '../helpers/test-helpers';

if( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout(30 * 60 * 1000 ); // 30 minutes

/*
 * These tests require a webfrontauth() in order to run them.
 * It needs to have:
 *  - Basic login enabled with one user matching the following pattern:
 *      {
 *          name: 'admin',
 *          password: 'admin'
 *      }
 *  - A not null sliding expiration
 */
describe('AuthService', function() {
    let authService: AuthService;

    const anonymous: IUserInfo = {
        userId: 0,
        userName: '',
        schemes: []
    };

    const logoutModel: IAuthenticationInfo = {
        user: anonymous,
        unsafeUser: anonymous,
        actualUser: anonymous,
        unsafeActualUser: anonymous,
        expires: undefined,
        criticalExpires: undefined,
        deviceId:"",
        isImpersonated: false,
        level: AuthLevel.None
    };

    beforeAll(async function() {
        const axiosInstance = axios.create();
        authService = await AuthService.createAsync( axiosInstance );
    });

    beforeEach(async function() {
        await authService.logout();
    });

    it('should basicLogin and logout (rememberMe: false).', async function() {
        await authService.basicLogin('Albert', 'success', false);
        let currentModel: IAuthenticationInfo = authService.authenticationInfo;
        expect(currentModel.user.userName).toBe('Albert');
        expect(currentModel.unsafeUser.userName).toBe('Albert');
        expect(currentModel.actualUser.userName).toBe('Albert');
        expect(currentModel.unsafeActualUser.userName).toBe('Albert');
        expect(currentModel.isImpersonated).toBe(false);
        expect(currentModel.level).toBe(AuthLevel.Normal);
        expect(authService.token).not.toBe('');
        expect(authService.refreshable).toBe(true);

        await authService.logout();
        currentModel = authService.authenticationInfo;
        expect(areUserInfoEquals(currentModel.user, anonymous)).toBe(true);
        expect(currentModel.unsafeUser.userName).toBe('');
        expect(areUserInfoEquals(currentModel.actualUser, anonymous)).toBe(true);
        expect(currentModel.unsafeActualUser.userName).toBe('');
        expect(currentModel.isImpersonated).toBe(false);
        expect(currentModel.level).toBe(AuthLevel.None); // rememberMe is false, authLevel is None (and not Unsafe)
        expect(authService.token).not.toBe(''); // TODO: Is it normal to still have a token at this point?
        expect(authService.refreshable).toBe(false);

        await authService.logout();
        expect(areAuthenticationInfoEquals(authService.authenticationInfo, logoutModel, true)).toBe(true);
        expect(authService.token).not.toBe('');
        expect(authService.refreshable).toBe(false);
    });

    it('should refresh correctly.', async function() {
        await authService.refresh();
        let currentModel: IAuthenticationInfo = authService.authenticationInfo;

        await authService.basicLogin('Albert', 'success', false);
        currentModel = authService.authenticationInfo;
        expect(currentModel.user.userName).toBe('Albert');
        expect(currentModel.unsafeUser.userName).toBe('Albert');
        expect(currentModel.actualUser.userName).toBe('Albert');
        expect(currentModel.unsafeActualUser.userName).toBe('Albert');
        expect(currentModel.isImpersonated).toBe(false);
        expect(currentModel.level).toBe(AuthLevel.Normal);
        expect(authService.token).not.toBe('');
        expect(authService.refreshable).toBe(true);

        await authService.refresh();
        currentModel = authService.authenticationInfo;
        expect(currentModel.user.userName).toBe('Albert');
        expect(currentModel.unsafeUser.userName).toBe('Albert');
        expect(currentModel.actualUser.userName).toEqual('Albert');
        expect(currentModel.unsafeActualUser.userName).toBe('Albert');
        expect(currentModel.isImpersonated).toBe(false);
        expect(currentModel.level).toBe(AuthLevel.Normal);
        expect(authService.token).not.toBe('');
        expect(authService.refreshable).toBe(true);

        await authService.logout();
        currentModel = authService.authenticationInfo;
        expect(areUserInfoEquals(currentModel.user, anonymous)).toBe(true);
        expect(currentModel.unsafeUser.userName).toBe('');
        expect(areUserInfoEquals(currentModel.actualUser, anonymous)).toBe(true);
        expect(currentModel.unsafeActualUser.userName).toBe('');
        expect(currentModel.isImpersonated).toBe(false);
        expect(currentModel.level).toBe(AuthLevel.None); // rememberMe is false, authLevel is None (and not Unsafe)
        expect(authService.token).not.toBe(''); // TODO: Is it normal to still have a token at this point?
        expect(authService.refreshable).toBe(false);

        await authService.logout();
        expect(areAuthenticationInfoEquals(authService.authenticationInfo, logoutModel, true)).toBe(true);
        expect(authService.token).not.toBe(''); // TODO: Is it normal to still have a token at this point?
        expect(authService.refreshable).toBe(false);
    });

    it('should call OnChange() correctly.', async function() {
        let authenticationInfo: IAuthenticationInfo = authService.authenticationInfo;
        const onChangeFunction = () => authenticationInfo = authService.authenticationInfo;
        authService.addOnChange(onChangeFunction);
        await authService.basicLogin('Albert','success');
        expect(authService.authenticationInfo.user.userName).toBe('Albert');
        expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(false);
        await authService.logout();
        expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(true);

        authService.removeOnChange(onChangeFunction);
        await authService.basicLogin('Albert','success');
        expect(areUserInfoEquals(authenticationInfo.user, anonymous)).toBe(true);
    });
});
