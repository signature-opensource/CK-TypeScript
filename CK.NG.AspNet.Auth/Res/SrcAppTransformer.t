create <html> transformer on "../src/app/app.component.html"
begin
    unless <CK.Ng.AspNet.Auth>
    begin
        insert before * """
                        @if( isAuthenticated() ) {
                        <ck-private-page />
                        } @else {
        
                        """;
        insert "}" after *;
    end
end

create <ts> transformer on "../src/app/app.component.ts"
begin
    unless <CK.Ng.AspNet.Auth>
    begin
        ensure import { inject, computed } from '@angular/core';
        ensure import { CommonModule } from '@angular/common';
        ensure import { PrivatePageComponent, NgAuthService } from '@local/ck-gen';

        in after "@Component" 
            in first {^braces}
                in after "imports:"
                    in first {^[]}
                        // ensure array contains "CommonModule, PrivatePageComponent"
                        replace "RouterOutlet" with "RouterOutlet, CommonModule, PrivatePageComponent";
        in {class} where "AppComponent"
            insert after first "{"
                    """

                    #authService = inject(NgAuthService);
                    isAuthenticated = computed(() => this.#authService.authenticationInfo().user.userId !== 0);


                    """;
    end
end
