import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { ResponsiveDirective } from '@local/ck-gen';
import { faArrowLeft } from '@fortawesome/free-solid-svg-icons';

import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
    selector: 'ck-authentication-page',
    imports: [
        CommonModule,
        RouterOutlet,
        RouterLink,
        TranslateModule,
        ResponsiveDirective,
        FontAwesomeModule,
        NzButtonModule,
    ],
    templateUrl: './authentication-page.component.html'
})
export class AuthenticationPageComponent {
    protected leftIcon = faArrowLeft;

    logoSrc = input<string>( 'logos/login-logo.png' );
    logoWhiteSrc = input<string>( 'logos/login-logo-white.png' );

    // TODO: handle light/dark mode toggle
    // displayedLogoSrc: string;
}
