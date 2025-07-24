import { Component, inject, computed, input, linkedSignal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faArrowRightFromBracket } from '@fortawesome/free-solid-svg-icons';
import { faUser } from '@fortawesome/free-regular-svg-icons';
import { TranslateModule } from '@ngx-translate/core';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NgAuthService } from '@local/ck-gen/CK/Ng/AspNet/Auth/NgAuthService';


@Component( {
    selector: 'ck-user-info-box',
    imports: [RouterLink, FontAwesomeModule, TranslateModule, NzAvatarModule, NzDividerModule, NzDropDownModule],
    templateUrl: './user-info-box.component.html'
} )
export class UserInfoBoxComponent {
    readonly #authService = inject( NgAuthService );
    readonly #router = inject( Router );

    avatarSize = input<number>( 48 );

    protected userIcon = faUser;
    protected signOutIcon = faArrowRightFromBracket;

    userName = linkedSignal( () => this.#authService.authenticationInfo().user.userName );
    actualAvatarSize = computed( () => {
        let result = this.avatarSize();
        // <AvatarSizeComputing />
        return result;
    } );
    avatarImgSrc = computed( () => {
        let result = '';
        // <AvatarImgSrcComputing />
        return result;
    } );
    avatarFallback = computed( () => {
        const user = this.#authService.authenticationInfo().user;
        const trimAndUpper = ( s: string ) => s.trim().charAt( 0 ).toUpperCase();

        let result = user.userName;

        // <PreAvatarFallbackComputing revert />

        // Split by separators and pick up to 2 initials
        const parts = result.split( /[\s._\-+~]+/ ).filter( Boolean );
        if ( parts.length >= 2 ) {
            return `${trimAndUpper( parts[0] )}${trimAndUpper( parts[1] )}`;
        }
        result = result.slice( 0, 2 ).toUpperCase();

        // <PostAvatarFallbackComputing />

        return result;
    } );

    logout(): void {
        // <PreLogoutRedirection />
        this.#router.navigate( ['/auth/logout'] );
        // <PostLogoutRedirection />
    }
}
