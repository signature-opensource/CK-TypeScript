import { inject, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { AuthService, IAuthenticationInfo, IUserInfo } from '@local/ck-gen';

@Injectable({ providedIn: 'root' })
export class NgAuthService {
    #authService = inject(AuthService);
    #authenticationInfo: WritableSignal<IAuthenticationInfo> = signal(this.#authService.authenticationInfo);

    authenficationInfo: Signal<IAuthenticationInfo<IUserInfo>> = this.#authenticationInfo.asReadonly();

    constructor() {
        this.#authService.addOnChange((eventSource: AuthService) => this.#authenticationInfo.set(eventSource.authenticationInfo));
    }
}
