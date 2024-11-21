import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '@local/ck-gen';

export const authInterceptor: HttpInterceptorFn = ( request, next ) => {
    const authService = inject(AuthService);

    const clonedRequest = authService.shouldSetToken(request.url) && authService.token
                            ? request.clone({ headers: request.headers.set('Authorization', `Bearer ${authService.token}`) })
                            : request;

    return next( clonedRequest );
};
