import { Component, inject } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faEye, faEyeSlash, faLock, faUser } from '@fortawesome/free-solid-svg-icons';
import { AuthService, CKNotificationService } from '@local/ck-gen';

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
    TranslateModule,
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
  readonly #notifService = inject( CKNotificationService );
  readonly #translateService = inject( TranslateService );

  protected eyeIcon = faEye;
  protected eyeSlashIcon = faEyeSlash;
  protected passwordIcon = faLock;
  protected userIcon = faUser;

  loginForm: FormGroup = this.#formBuilder.group( {
    userName: new FormControl( this.#authService.authenticationInfo.unsafeUser.userName, { nonNullable: true, validators: [Validators.required] } ),
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

      if ( this.#authService.lastResult.error ) {
        this.#notifService.displaySimpleMessage( 'error', this.#translateService.instant( this.#authService.lastResult.error.errorId ) );
      }
    }
  }

  async keyDown( event: KeyboardEvent ): Promise<void> {
    if ( event.code === "Enter" ) {
      await this.submit();
    }
  }
}
