import axios from 'axios';
import { AuthLevel, AuthService } from '@local/ck-gen';
import { NgAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/auth.service';
import { TestBed } from '@angular/core/testing';
import { provideNgAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/auth-service-provider';

if ( process.env["VSCODE_INSPECTOR_OPTIONS"] ) jest.setTimeout( 30 * 60 * 1000 ); // 30 minutes

const serverAddress = CKTypeScriptEnv['SERVER_ADDRESS'] ?? "";
const describeWithServer = serverAddress ? describe : describe.skip;

describeWithServer( 'NgAuthService integration tests', () => {
    beforeAll( async () => {
        const axiosInstance = axios.create();
        await TestBed.configureTestingModule( { providers: [provideNgAuthService( axiosInstance )] } ).compileComponents();
    } );

    beforeEach( async () => {
        const authService = TestBed.inject( AuthService );
        await authService.logout();
    } );

    it( 'ngAuthService authInfos should be correctly updated', async () => {
        const ngAuthService = TestBed.inject( NgAuthService );
        const authService = TestBed.inject( AuthService );

        expect( ngAuthService.authenficationInfo() ).toStrictEqual( authService.authenticationInfo );
        await authService.basicLogin( 'Albert', 'success' );
        expect( ngAuthService.authenficationInfo() ).toStrictEqual( authService.authenticationInfo );

        let current = ngAuthService.authenficationInfo();
        expect( current.user.userName ).toBe( 'Albert' );
        expect( current.level ).toBe( AuthLevel.Normal );

        await authService.logout();
        expect( ngAuthService.authenficationInfo() ).toStrictEqual( authService.authenticationInfo );

        current = ngAuthService.authenficationInfo();
        expect( current.user.userId ).toBe( 0 );
        expect( current.user.userName ).toBe( '' );
    } );
} );