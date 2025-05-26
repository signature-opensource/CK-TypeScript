create <html> transformer on "../src/app/app.component.html"
begin
    unless <HasNgPublicPage>
    begin
        replace single "<router-outlet />" with "<ck-public-page />";
    end
end

create <ts> transformer on "../src/app/app.component.ts"
begin
    unless <HasNgPublicPage>
    begin
        ensure import { PublicPageComponent } from '@local/ck-gen';

        insert before "@Component" """
                                   // Public Page is from CK.Ng.PublicPage package.

                                   """;

        in after "@Component" 
            in first {^braces}
                in after "imports:"
                    // Should be: ensure array contains "PublicPageComponent";
                    // That is: ensure single array contains "PublicPageComponent";
                    in first {^[]}
                        replace "RouterOutlet" with "RouterOutlet, PublicPageComponent";
    end
end
