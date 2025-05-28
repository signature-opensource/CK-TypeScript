import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Router, RouterLink } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faEnvelope, faEye, faEyeSlash, faLock, faUser } from '@fortawesome/free-solid-svg-icons';
import { ResponsiveDirective, AuthLevel, AuthService, CKNotificationService } from '@local/ck-gen';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';

@Component( {
  selector: 'ck-basic-login-form',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    ResponsiveDirective,
    FontAwesomeModule,
    NzButtonModule,
    NzCheckboxModule,
    NzDividerModule,
    NzFormModule,
    NzInputModule,
    NzToolTipModule
  ],
    templateUrl: './basic-login-form.component.html'
} )
export class BasicLoginFormComponent {
  readonly #authService = inject( AuthService );
  readonly #formBuilder = inject( FormBuilder );
  readonly #router = inject( Router );
  readonly #notifService = inject( CKNotificationService );

  protected eyeIcon = faEye;
  protected eyeSlashIcon = faEyeSlash;
  protected emailIcon = faEnvelope;
  protected passwordIcon = faLock;
  protected guestIcon = faUser;

  loginForm: FormGroup = this.#formBuilder.group( {
    userName: new FormControl( this.#authService.authenticationInfo.user.userName, { nonNullable: true, validators: [Validators.required, Validators.email] } ),
    password: new FormControl( '', { nonNullable: true, validators: [Validators.required] } ),
    rememberMe: new FormControl( this.#authService.rememberMe, { nonNullable: true } )
  } );
  showPassword: boolean = false;

  async submit(): Promise<void> {
    if ( this.loginForm && this.loginForm.valid ) {
      await this.#authService.basicLogin(
        this.loginForm.get( 'userName' )!.value.trim(),
        this.loginForm.get( 'password' )!.value,
        this.loginForm.get( 'rememberMe' )!.value
      );

      if ( !this.#authService.lastResult.error ) {
        this.#router.navigate( [''] );
      } else {
        this.#notifService.displaySimpleMessage( 'error', this.#authService.lastResult.error.errorId );
      }
    }
  }

  async keyDown( event: KeyboardEvent ): Promise<void> {
    if ( event.code === "Enter" ) {
      await this.submit();
    }
  }
}
