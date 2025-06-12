import { Component, effect, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Router } from '@angular/router';

import { IUserSchemeInfo, NgAuthService, ResponsiveDirective } from '@local/ck-gen';

import { NzButtonModule } from 'ng-zorro-antd/button';

@Component( {
  selector: 'ck-login',
  imports: [
    CommonModule,
    TranslateModule,
    ResponsiveDirective,
    NzButtonModule
  ],
  templateUrl: './login.component.html'
} )
export class LoginComponent {
  readonly #authService = inject( NgAuthService );
  readonly #router = inject( Router );

  // TODO: handle light/dark mode toggle
  // logoWhiteSrc = input<string>( 'logos/login-logo-white.png' );

  // assets can be overridden
  displayedLogoSrc: string = 'logos/login-logo.png';

  // <RedirectionPath />
  redirectionPath: string = '';

  sortedProviders: Array<string> = [];

  constructor() {
    this.#authService.authService.addOnChange( async ( auth ) => {
      if ( auth.authenticationInfo.user.userId !== 0 ) {
        this.#router.navigate( [this.redirectionPath] );
      }
    } );

    effect( () => {
      this.sortStringsByLastUsed();
    } );
  }

  async loginWithProvider( p: string ): Promise<void> {
    await this.#authService.authService.startPopupLogin( p );
  }

  getProviderLogoSrc( p: string ): string {
    if ( p.includes( 'Oidc.' ) ) {
      return `login-providers/${p.split( 'Oidc.' )[1].toLocaleLowerCase()}.png`;
    }
    return `login-providers/${p.toLocaleLowerCase()}.png`;
  }

  getProviderName( p: string ): string {
    if ( p.includes( 'Oidc.' ) ) {
      return p.split( 'Oidc.' )[1];
    }

    return p;
  }

  sortStringsByLastUsed(): void {
    const lastUsedMap = new Map<string, Date>();
    this.#authService.authenticationInfo().user.schemes.forEach( ( obj: IUserSchemeInfo ) => {
      lastUsedMap.set( obj.name, obj.lastUsed );
    } );

    const schemes = [...this.#authService.authService.availableSchemes];
    this.sortedProviders = schemes.sort( ( a, b ) => {
      const dateA = lastUsedMap.get( a );
      const dateB = lastUsedMap.get( b );

      if ( !dateA && !dateB ) return 0;
      if ( !dateA ) return 1;
      if ( !dateB ) return -1;

      return dateB.getTime() - dateA.getTime();
    } );
  }
}
