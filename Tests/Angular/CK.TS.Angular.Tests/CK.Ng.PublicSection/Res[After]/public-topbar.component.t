create <less> transformer
begin
    inject """
           // Transformed by Res[After]/PublicSection/public-topbar.t

           """ into <PreContent>;
end

create <ts> transformer
begin
    inject """
           // Transformed by Res[After]/PublicSection/public-topbar.t

           """ into <TestPoint>;
end

create <html> transformer
begin
    // Tokens must match... Exactly!
    // This is barely usable: here "I'm the top bar." will fail on "I'm the top bar. ".
    //
    // We should introduce a "inside token matcher". The problem is to transfer the
    // result of a "sub match" to above statements (like the "replace").
    // Sub matches (that may also applies to matches in trivias) can be costly and
    // should not interfere with the TokenSpan [x,y[ token-level model.
    //
    replace "I'm the top bar. "
        with "I'm the top bar (altered by Res[After]/PublicSection/public-topbar.t).<hr>";
end
