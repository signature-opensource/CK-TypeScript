// <HasNgPrivatePage />
import { Component, inject, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { CommonModule } from '@angular/common';
import { PrivatePageComponent, NgAuthService } from '@local/ck-gen';
// Private Page is from CK.Ng.AspNet.Auth package.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, PrivatePageComponent, CKGenAppModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {

readonly #authService = inject( NgAuthService );
isAuthenticated = computed( () => this.#authService.authenticationInfo().user.userId !== 0 );

  title = 'From_Scratch';
}
