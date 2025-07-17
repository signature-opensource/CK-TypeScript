import { Component, inject, computed } from '@angular/core';
import { NgAuthService } from '@local/ck-gen';

@Component({
    selector: 'ck-user-profile-page',
    imports: [],
    templateUrl: './user-profile-page.component.html'
})
export class UserProfilePageComponent {
    readonly #authService = inject(NgAuthService);
}
