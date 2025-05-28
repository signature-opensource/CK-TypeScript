create <html> transformer
begin
    inject """

           <ck-basic-login-form />

           """ into BasicLogin;
end

create <ts> transformer
begin
    unless <HasNgAspNetAuthBasic>
    begin
        ensure import { BasicLoginFormComponent } from '@local/ck-gen';

        in after "@Component" 
            in first {^braces}
                in after "imports:"
                    in first {^[]}
                        replace "TranslateModule" with "TranslateModule, BasicLoginFormComponent";
    end
end
