/*
create <ts> transformer on "CK/Angular/routes.ts"
begin                     
    if "CK.Ng.AspNet.Auth"
    begin
        replace single "canActivate" with "canMatch";

        // Note: the following line is not supported... yet.
        in single <LoginRediction> replace * with "";
    end
end
*/
