// <HasNgPublicPage />
import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { PublicPage } from '@local/ck-gen';
// Public Page is from CK.Ng.PublicPage package.

@Component( {
  selector: 'app-root',
  imports: [RouterOutlet, PublicPage, CKGenAppModule],
  templateUrl: './app.html',
  styleUrl: './app.less'
} )
export class App {
  readonly title = signal( 'CK_TS_Angular' );
}
