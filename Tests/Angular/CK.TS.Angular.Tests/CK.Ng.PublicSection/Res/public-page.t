create <less> transformer
begin
  inject """
         background-image: url("/images/background.jpg");
         background-size: cover;
         background-position: 70% 50%;
         display: inline-block;
         """ into <CKPublicPage>;
end

create <html> transformer
begin
    insert """
           <div>
           <button nz-button nzType="primary" routerLink="/auth">
                          {{ 'Button.GoToLogin' }}
           </button>
           </div>
 
           """ before "<router-outlet />";
end

 
create <ts> transformer
begin
    ensure import { NzButtonModule } from 'ng-zorro-antd/button';
    ensure import { RouterLink } from '@angular/router';
 
    in after "@Component" 
        in first {^braces}
            in after "imports:"
                in first {^[]}
                    replace "RouterOutlet" with "RouterOutlet, NzButtonModule, RouterLink";
end
