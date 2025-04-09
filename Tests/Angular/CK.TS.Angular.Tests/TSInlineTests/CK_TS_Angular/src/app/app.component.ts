import { RouterOutlet } from '@angular/router';
import { CKGenAppModule, CKGenInjected } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject } from '@angular/core';
import {LoginComponent, SomeAuthService, PublicTopbarComponent } from '@local/ck-gen';

const ckGenInjected: CKGenInjected = [LoginComponent,PublicTopbarComponent];

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, CKGenAppModule, ...ckGenInjected],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
  authService = inject(SomeAuthService);
  // <Constructor>
}

