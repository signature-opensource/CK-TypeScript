import { InjectionToken } from '@angular/core';
import { AxiosInstance } from 'axios';

/**
 * An injection token that can be used in a DI provider that is the default Axios instance.
 */
export const AXIOS = new InjectionToken<AxiosInstance>('AxiosInstance');

