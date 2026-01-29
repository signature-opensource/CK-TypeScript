import { InjectionToken } from '@angular/core';
import {IAuthServiceConfiguration} from '@local/ck-gen';

/**
 * An injection token that can be used in a DI provider that is the auth service configuration.
 * It is an optional parameter of the AuthService constructor.
 */
export const AUTH_SERVICE_CONFIGURATION = new InjectionToken<IAuthServiceConfiguration>('IAuthServiceConfiguration');

