// <CK.Ng.AspNetAuth />
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PrivatePageComponent, NgAuthService, AuthLevel } from '@local/ck-gen';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CKGenAppModule, CommonModule, PrivatePageComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
  #authService = inject(NgAuthService);
  isAuthenticated = computed(() => this.#authService.authenticationInfo().level >= AuthLevel.Normal);
}
