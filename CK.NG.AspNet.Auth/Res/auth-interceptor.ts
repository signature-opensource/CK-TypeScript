import { Injectable, inject, signal } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { AuthService } from '@signature/webfrontauth';
import { Observable } from 'rxjs';

@Injectable( { providedIn: 'root' } )
export class AuthInterceptor implements HttpInterceptor {
    readonly #authService = inject( AuthService );

    public intercept( request: HttpRequest<any>, next: HttpHandler ): Observable<HttpEvent<any>> {
        const clonedRequest = this.#authService.shouldSetToken( request.url ) &&
            this.#authService.token ?
            request.clone( { headers: request.headers.set('Authorization', `Bearer ${this.#authService.token}`) } )
            : request;

        return next.handle( clonedRequest );
    }
}
