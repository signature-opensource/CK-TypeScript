import { inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { first } from 'rxjs';
import { UserMessageLevel } from '../../Core/UserMessageLevel';
import { SimpleUserMessage } from '../../Core/SimpleUserMessage';

@Injectable( {
  providedIn: 'root'
} )
export class NotificationService {
  #message = inject( NzMessageService );
  #notif = inject( NzNotificationService );
  #translateService = inject( TranslateService );

  displayUserMessage( m: SimpleUserMessage, duration: number = 2500 ): void {
    switch ( m.level ) {
      case UserMessageLevel.Error:
        this.#message.error( m.message, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case UserMessageLevel.Warn:
        this.#message.warning( m.message, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case UserMessageLevel.Info:
        this.#message.success( m.message, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case UserMessageLevel.None:
      default:
        this.#message.info( m.message, { nzDuration: duration, nzPauseOnHover: true } );
        break;
    }
  }

  displaySimpleMessage( type: 'error' | 'warning' | 'success' | 'info', m: string, duration: number = 2500 ): void {
    switch ( type ) {
      case 'error':
        this.#message.error( m, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case 'warning':
        this.#message.warning( m, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case 'success':
        this.#message.success( m, { nzDuration: duration, nzPauseOnHover: true } );
        break;
      case 'info':
      default:
        this.#message.info( m, { nzDuration: duration, nzPauseOnHover: true } );
        break;
    }
  }

  notifyUserMessage( m: SimpleUserMessage, title: string = '', duration: number = 2500 ): void {
    switch ( m.level ) {
      case UserMessageLevel.Error:
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Error' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.error( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.error( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case UserMessageLevel.Warn:
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Warning' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.warning( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.warning( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case UserMessageLevel.Info:
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Success' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.success( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.success( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case UserMessageLevel.None:
      default:
        this.#notif.info( title, m.message, { nzDuration: duration, nzPauseOnHover: true } );
        break;
    }
  }

  notifySimpleMessage( type: 'error' | 'warning' | 'success' | 'info', m: string, title: string = '', duration: number = 2500 ): void {
    switch ( type ) {
      case 'error':
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Error' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.error( title, m, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.error( title, m, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case 'warning':
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Warning' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.warning( title, m, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.warning( title, m, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case 'success':
        if ( title.length === 0 ) {
          this.#translateService.get( 'Notification.GenericTitle.Success' ).pipe( first() ).subscribe( t => {
            title = t;
            this.#notif.success( title, m, { nzDuration: duration, nzPauseOnHover: true } );
          } );
        } else {
          this.#notif.success( title, m, { nzDuration: duration, nzPauseOnHover: true } );
        }
        break;
      case 'info':
      default:
        this.#notif.info( title, m, { nzDuration: duration, nzPauseOnHover: true } );
        break;
    }
  }

  notifyGenericCommunicationError(): void {
    this.#translateService.get( 'Notification.Generic.CommunicationError' ).pipe( first() ).subscribe( t => {
      this.notifySimpleMessage( 'error', t );
    } );
  }
}
