// <HasNgPublicPage />
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject } from '@angular/core';
import { PublicPageComponent } from '@local/ck-gen';
// Public Page is from CK.Ng.PublicPage package.

@Component({
    selector: 'app-root',
    imports: [RouterOutlet, PublicPageComponent, CKGenAppModule],
    templateUrl: './app.component.html',
    styleUrl: './app.component.less'
})
export class AppComponent {
  title = 'Demo';
}
