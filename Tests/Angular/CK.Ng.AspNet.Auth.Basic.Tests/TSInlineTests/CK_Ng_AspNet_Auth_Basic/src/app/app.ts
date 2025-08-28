import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component( {
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, CKGenAppModule],
  templateUrl: './app.html',
  styleUrl: './app.less'
} )
export class App {
  readonly title = signal( 'CK_Ng_AspNet_Auth_Basic' );
}
