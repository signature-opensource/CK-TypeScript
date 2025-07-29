import { UserService } from '@local/ck-gen/CK/Ng/UserProfile/user.service';
import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'ck-user-profile-page',
    imports: [
        NzAvatarModule,
        NzTabsModule,
        TranslateModule
    ],
    templateUrl: './user-profile-page.component.html'
})
export class UserProfilePageComponent {
    // <PreDependencyInjection revert />
    readonly #userService = inject(UserService);
    // <PostDependencyInjection />

    // <PreInputOutput revert />
    avatarSize = input<number>(192);
    // <PostInputOutput />

    // <PreLocalVariables revert />
    userProfile = linkedSignal(() => this.#userService.userProfile());
    actualAvatarSize = computed(() => {
        let result = this.avatarSize();
        // <AvatarSizeComputing />
        return result;
    });
    avatarImgSrc = computed(() => {
        let result = '';
        // <AvatarImgSrcComputing />
        return result;
    });
    avatarFallback = computed(() => {
        const trimAndUpper = (s: string) => s.trim().charAt(0).toUpperCase();

        let result = '';
        if (this.userProfile()) {
            result = this.userProfile()!.userName;

            // <PreAvatarFallbackComputing revert />

            // Split by separators and pick up to 2 initials
            const parts = result.split(/[\s._\-+~]+/).filter(Boolean);
            if (parts.length >= 2) {
                return `${trimAndUpper(parts[0])}${trimAndUpper(parts[1])}`;
            }
            result = result.slice(0, 2).toUpperCase();

            // <PostAvatarFallbackComputing />
        }

        return result;
    });
    // <PostLocalVariables />
}
