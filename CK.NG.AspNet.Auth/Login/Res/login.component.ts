import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { AuthService } from '@local/ck-gen';

@Component( {
  selector: 'ck-login',
  imports: [
    CommonModule,
    TranslateModule
  ],
  templateUrl: './login.component.html'
} )
export class LoginComponent {
  readonly #authService = inject( AuthService );
  readonly #router = inject( Router );

  logoSrc = input<string>( 'logos/signature-one-logo.png' );
  redirectionPath = input<string>( '' );

  constructor() {
    this.#authService.addOnChange( async ( auth ) => {
      if ( auth.authenticationInfo.user.userId !== 0 ) {
        this.#router.navigate( [this.redirectionPath()] );
      }
    } );
  }
}
