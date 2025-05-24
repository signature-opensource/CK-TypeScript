import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject } from '@angular/core';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, CKGenAppModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
}

