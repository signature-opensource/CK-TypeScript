create <html> transformer
begin
    inject """

           <ck-basic-login-form />

           """ into <BasicLogin>;
end

create <ts> transformer
begin
    ensure import { BasicLoginFormComponent } from '../Basic/basic-login-form/basic-login-form.component';

    in after "@Component" 
        in first {^braces}
            in after "imports:"
                in first {^[]}
                    replace "TranslateModule" with "TranslateModule, BasicLoginFormComponent";
end
