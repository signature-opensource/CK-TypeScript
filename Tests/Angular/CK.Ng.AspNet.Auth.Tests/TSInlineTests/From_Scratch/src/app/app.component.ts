// <HasNgPrivatePage />
import { Component, inject, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PrivatePageComponent, NgAuthService } from '@local/ck-gen';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
// Private Page is from CK.Ng.AspNet.Auth package.
@Component({
  selector: 'app-root',
  imports: [RouterOutlet CKGenAppModule,, CommonModule, PrivatePageComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {

#authService = inject(NgAuthService);
isAuthenticated = computed(() => this.#authService.authenticationInfo().user.userId !== 0);

  title = 'From_Scratch';
}

