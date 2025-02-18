import { RouterOutlet } from '@angular/router';
import { CKGenAppModule, CKGenInjected, LoginComponent, SomeAuthService, PublicTopbarComponent } from '@local/ck-gen';
import { Component, inject } from '@angular/core';


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

