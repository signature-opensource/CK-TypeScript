import axios from 'axios';
import { AuthLevel, AuthService } from '@local/ck-gen';
import { NgAuthService, CKGenAppModule } from '@local/ck-gen';
import { TestBed } from '@angular/core/testing';
import { CookieJar } from 'tough-cookie';
import { wrapper as addCookieJar } from 'axios-cookiejar-support';

if ( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout( 30 * 60 * 1000 ); // 30 minutes

const serverAddress = ""//CKTypeScriptEnv['SERVER_ADDRESS'] ?? "";
const describeWithServer = serverAddress ? describe : describe.skip;

describeWithServer( 'NgAuthService integration tests', () => {
    beforeAll( async () => {
        const axiosInstance = axios.create();
        const cookieJar = new CookieJar();
        addCookieJar(axiosInstance);
        axiosInstance.defaults.jar = cookieJar;
        await TestBed.configureTestingModule( { providers: CKGenAppModule.Providers } ).compileComponents();
    } );

    beforeEach( async () => {
        const authService = TestBed.inject( AuthService );
        await authService.logout();
    } );
        
    it( 'ngAuthService authInfos should be correctly updated', async () => {
        
        const ngAuthService = TestBed.inject( NgAuthService );
        const authService = ngAuthService.authService;

      expect(authService.authenticationInfo.level).toBe(AuthLevel.None);
      expect(authService.availableSchemes.length).toBeGreaterThan(0);

        expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );
        await authService.basicLogin( 'Albert', 'success' );
        expect( ngAuthService.authenticationInfo().level ).toBe( AuthLevel.Normal );
        expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );

        let current = ngAuthService.authenticationInfo();
        expect( current.user.userName ).toBe( 'Albert' );
        expect( current.level ).toBe( AuthLevel.Normal );

        await authService.logout();
        expect( ngAuthService.authenticationInfo() ).toStrictEqual( authService.authenticationInfo );

        current = ngAuthService.authenticationInfo();
        expect( current.user.userId ).toBe( 0 );
        expect( current.user.userName ).toBe( '' );
    } );
} );
