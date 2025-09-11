create <ts> transformer
begin
    ensure import { UserInfoBoxComponent } from '@local/ck-gen';
 
    in after "@Component" 
        in first {^braces}
            in after "imports:"
                in first {^[]}
                    replace "RouterOutlet" with "RouterOutlet, UserInfoBoxComponent";
end

create <html> transformer
begin
    insert before * "<ck-user-info-box />";
end
