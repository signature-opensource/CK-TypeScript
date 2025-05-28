import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faEnvelope } from '@fortawesome/free-regular-svg-icons';
import { faArrowLeft } from '@fortawesome/free-solid-svg-icons';
import { TranslateModule } from '@ngx-translate/core';
import {
    //HttpCrisEndpoint,
    //SendForgotPasswordEmailCommand,
    CKNotificationService
} from '@local/ck-gen';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';

@Component( {
  selector: 'ck-password-lost',
  templateUrl: './password-lost.component.html',
  imports: [
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    FontAwesomeModule,
    NzButtonModule,
    NzFormModule
  ]
} )
export class PasswordLostComponent {
  readonly #router = inject( Router );
  readonly #formBuilder = inject( FormBuilder );
  //readonly #crisEndpoint = inject( HttpCrisEndpoint );
  readonly #notifService = inject( CKNotificationService );

  public envelopeIcon = faEnvelope;
  public returnIcon = faArrowLeft;
  public formGroup: FormGroup<{ email: FormControl<string> }> = this.#formBuilder.group( {
    email: new FormControl( '', { nonNullable: true, validators: [Validators.email, Validators.required] } )
  } );

  goToLogin(): void {
    this.#router.navigate( ['login'] );
  }

  async sendEmail(): Promise<void> {
    if ( this.formGroup.valid ) {
      //const cmd = new SendForgotPasswordEmailCommand( this.formGroup.get( 'email' )!.value/*, SupportedLanguage.FRENCH*/ );
      //const res = await this.#crisEndpoint.sendOrThrowAsync( cmd );
      //if ( res ) {
      //  this.#notifService.handleNotification( res.level, res.message );
      //}
      this.formGroup.reset();
    }
  }

  async keyDown( event: KeyboardEvent ): Promise<void> {
    if ( event.code === "Enter" ) {
      await this.sendEmail();
    }
  }
}
