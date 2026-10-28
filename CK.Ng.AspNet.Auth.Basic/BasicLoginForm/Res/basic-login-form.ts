import { Component, inject, signal } from '@angular/core';
import { form, FormField, FormRoot, required } from '@angular/forms/signals';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faEye, faEyeSlash, faLock, faUser } from '@fortawesome/free-solid-svg-icons';
import { AuthService, NotificationService } from '@local/ck-gen';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';

@Component( {
  selector: 'ck-basic-login-form',
  imports: [
    CommonModule,
    FormField,
    FormRoot,
    TranslateModule,
    FontAwesomeModule,
    NzButtonModule,
    NzCheckboxModule,
    NzDividerModule,
    NzFormModule,
    NzInputModule,
    NzTooltipModule
  ],
    templateUrl: './basic-login-form.html'
} )
export class BasicLoginForm {
  // <PreDependencyInjection revert />
  readonly #authService = inject( AuthService );
  readonly #notifService = inject( NotificationService );
  readonly #translateService = inject( TranslateService );
  // <PostDependencyInjection />

  // <PreIconsDefinition revert />
  protected eyeIcon = faEye;
  protected eyeSlashIcon = faEyeSlash;
  protected passwordIcon = faLock;
  protected userIcon = faUser;
  // <PostIconsDefinition />

  // <PreLocalVariables revert />
  readonly loginModel = signal( {
    userName: this.#authService.authenticationInfo.unsafeUser.userName,
    password: '',
    rememberMe: this.#authService.rememberMe
  } );
  readonly loginForm = form( this.loginModel, p => {
    required( p.userName );
    required( p.password );
  }, { name: 'login' } );
  showPassword: boolean = false;
  // <PostLocalVariables />

  async submit(): Promise<void> {
    if ( this.loginForm().valid() ) {
      const { userName, password, rememberMe } = this.loginModel();
      await this.#authService.basicLogin( userName.trim(), password, rememberMe );

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
