import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule, CKGenInjected } from '@local/ck-gen/CK/Angular/CKGenAppModule';

const ckGenInjected: CKGenInjected = [];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CKGenAppModule, ...ckGenInjected],
  templateUrl: './app.component.html',
  styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
}

