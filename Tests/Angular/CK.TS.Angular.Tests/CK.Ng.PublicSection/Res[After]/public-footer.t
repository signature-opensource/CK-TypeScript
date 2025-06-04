
create <ts> transformer
begin
    ensure import { SomeAuthService } from '@local/ck-gen';
    inject """
           // Transformed by Res[After]/PublicSection/public-footer.t

           """ into <TestPoint>;
end
