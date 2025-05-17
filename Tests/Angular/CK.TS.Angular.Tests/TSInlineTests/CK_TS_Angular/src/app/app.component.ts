import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject } from '@angular/core';
import {LoginComponent, SomeAuthService, PublicTopbarComponent } from '@local/ck-gen';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, CKGenAppModule, LoginComponent, PublicTopbarComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
  authService = inject(SomeAuthService);
  // <Constructor>
}

