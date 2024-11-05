import { TestBed } from '@angular/core/testing';
import { NgAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/auth.service';
import { AXIOS, provideNgAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/auth-service-provider';
import axios, { AxiosInstance } from 'axios';
import { AuthServiceClientConfiguration, createDefaultConfig } from '@local/ck-gen/CK/Ng/AspNet/Auth/auth-service-configuration';

describe( 'NgAuthService configuration and injection tests', () => {
    let service: NgAuthService;
    let axiosInstance: AxiosInstance;
    const defaultConfig = createDefaultConfig();

    beforeEach( async () => {
        axiosInstance = axios.create();

        await TestBed.configureTestingModule( {
            providers: [provideNgAuthService( axiosInstance, defaultConfig )],
        } ).compileComponents();

        service = TestBed.inject( NgAuthService );
    } );

    it( 'NgAuthService can be injected', () => {
        expect( service ).toBeTruthy();
    } );

    it( 'AXIOS injection token should return the correct axios instance', () => {
        const injectedAxios = TestBed.inject( AXIOS );
        expect( injectedAxios ).toBe( axiosInstance );
    } );

    it( 'AuthConfiguration should return the correct instance', () => {
        const authConfig = TestBed.inject( AuthServiceClientConfiguration );
        expect( authConfig ).toStrictEqual( defaultConfig );
    } );
} );
