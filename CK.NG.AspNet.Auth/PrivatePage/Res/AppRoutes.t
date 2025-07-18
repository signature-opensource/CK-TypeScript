create <ts> transformer on "CK/Angular/routes.ts"
begin
    ensure import {AuthService} from "@local/ck-gen";
    ensure import {inject} from "@angular/core";

    insert ", canActivate: [() => inject( AuthService ).authenticationInfo.user.userId > 0]"
        after last "rPrivatePageComponent";
end
