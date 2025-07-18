// <HasNgPublicPage />
// <HasNgPrivatePage />
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrivatePageComponent, NgAuthService, PublicPageComponent } from '@local/ck-gen';
// Public Page is from CK.Ng.PublicPage package.
// Private Page is from CK.Ng.AspNet.Auth package.
@Component( {
  selector: 'app-root',
  imports: [RouterOutlet, PublicPageComponent, CommonModule, PrivatePageComponent, CKGenAppModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
} )
export class AppComponent {

  #authService = inject( NgAuthService );
  isAuthenticated = computed( () => this.#authService.authenticationInfo().user.userId !== 0 );

  title = 'CK_Ng_AspNet_Auth_Basic';
}
