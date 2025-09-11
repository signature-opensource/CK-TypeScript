import { RouterOutlet } from '@angular/router';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';
import { Component, inject, LOCALE_ID, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { locales, LocaleService } from '@local/ck-gen/ts-locales/locales';

@Component( {
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, CKGenAppModule],
  templateUrl: './app.html',
  styleUrl: './app.less'
} )
export class App {
  readonly title = signal( 'CK_Ng_AspNet_Auth_Basic' );

  config = inject( LOCALE_ID );
  lang = inject( LocaleService );

  pi = 3.14159265359;
  now = new Date();
  /**
   *
   */
  constructor() {
    console.log( this.config )
    console.log( this.lang.currentLocale() )
  }

  test(): void {
    const t = Object.keys( locales );
    const loc = t[Math.floor( Math.random() * t.length )];
    this.lang.setLocale( loc );
  }
}
