import { Component, inject, computed } from '@angular/core';
import { NgAuthService } from '@local/ck-gen';


@Component({
    selector: 'ck-user-info-box',
    imports: [],
    templateUrl: './user-info-box.component.html'
})
export class UserInfoBoxComponent {
    readonly #authService = inject(NgAuthService);

    userName = computed(() => this.#authService.authenticationInfo().user.userName);

}
