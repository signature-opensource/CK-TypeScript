create <ts> transformer on "CK/Angular/routes.ts"
begin
    ensure import { AuthService } from "@local/ck-gen";
    ensure import { inject } from "@angular/core";
    ensure import { RedirectCommand, Router } from '@angular/router';

    insert """

           var authService: AuthService | null = null;
           """
        before single "export default";

    insert """
           ,
           canActivate: [() => {
             const isAuthenticated = ( authService ??= inject( AuthService ) ).authenticationInfo.user.userId > 0;
             // <LoginRediction>
             if ( !isAuthenticated ) {
                 const loginPath = inject( Router ).parseUrl( "/auth" );
                 return new RedirectCommand( loginPath );
             }
             // </LoginRediction>
             
             return isAuthenticated;
           }]

           """
        after last "component: PrivatePageComponent";
end
