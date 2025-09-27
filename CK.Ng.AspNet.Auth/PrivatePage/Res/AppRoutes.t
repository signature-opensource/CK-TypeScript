create <ts> transformer on "CK/Angular/routes.ts"
begin
    ensure import { AuthService } from "@local/ck-gen";

    insert """

           let authService: AuthService | null = null;
           """
        before single "export default";

    insert """
           ,
           canMatch: [() => (authService ??= inject( AuthService )).authenticationInfo.user.userId > 0]

           """
        after last "component: PrivatePage";

    replace last "targetUrl = router.parseUrl( '/' );" with "targetUrl = router.parseUrl( '/auth' );";
    replace last "targetUrl.queryParams[ 'notFound' ] = currentRoute" with "targetUrl.queryParams[ 'redirectTo' ] = currentRoute";
end
