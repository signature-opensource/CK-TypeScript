import { TestBed } from '@angular/core/testing';
import { NgAuthService, AXIOS, CKGenAppModule } from '@local/ck-gen';
import axios, { AxiosInstance } from 'axios';

const serverAddress = "";//CKTypeScriptEnv['SERVER_ADDRESS'] ?? "";
const describeWithServer = serverAddress ? describe : describe.skip;

describeWithServer( 'NgAuthService configuration and injection tests', () => {
    let service: NgAuthService;
    let axiosInstance: AxiosInstance;

    beforeEach( async () => {
        axiosInstance = axios.create();

        await TestBed.configureTestingModule( { providers: CKGenAppModule.Providers } ).compileComponents();

        service = TestBed.inject( NgAuthService );
    } );

    it( 'NgAuthService can be injected', () => {
        expect( service ).toBeTruthy();
    } );

    it( 'AXIOS injection token should return the correct axios instance', () => {
        const injectedAxios = TestBed.inject( AXIOS );
        expect( injectedAxios ).toBe( axiosInstance );
    } );
} );
