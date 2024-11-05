
export type Collector = (s:string) => void;

/**
 * Mutable description of the basic popup window (it has to be a real browser's 'window).
 */
export class PopupDescriptor {

    #popupTitle: string = 'Connection';
    public get popupTitle(): string { return this.#popupTitle; }
    public set popupTitle( popupTitle: string ) { if( popupTitle ) { this.#popupTitle = popupTitle; } }

    #features: string = 'menubar=no, status=no, scrollbars=no, menubar=no, width=700, height=700';
    public get features(): string { return this.#features; }
    public set features( features: string ) { if( features ) { this.#features = features; } }

    #basicFormTitle: string = 'Connection';
    public get basicFormTitle(): string { return this.#basicFormTitle; }
    public set basicFormTitle( basicFormTitile: string ) { if( basicFormTitile ) { this.#basicFormTitle = basicFormTitile; } }

    #basicUserNamePlaceholder: string = 'username';
    public get basicUserNamePlaceholder(): string { return this.#basicUserNamePlaceholder; }
    public set basicUserNamePlaceholder( basicUserNamePlaceholder: string ) {
        if( basicUserNamePlaceholder ) { this.#basicUserNamePlaceholder = basicUserNamePlaceholder; }
    }

    #basicPasswordPlaceholder: string = 'password';
    public get basicPasswordPlaceholder(): string { return this.#basicPasswordPlaceholder; }
    public set basicPasswordPlaceholder( baiscPasswordPlaceholder: string ) {
        if( baiscPasswordPlaceholder ) { this.#basicPasswordPlaceholder = baiscPasswordPlaceholder; }
    }

    #basicSubmitButtonLabel: string = 'Submit';
    public get basicSubmitButtonLabel(): string { return this.#basicSubmitButtonLabel; }
    public set basicSubmitButtonLabel( basicSubmitButtonLabel: string ) {
        if( basicSubmitButtonLabel ) { this.#basicSubmitButtonLabel = basicSubmitButtonLabel; }
    }

    #basicMissingCredentialsError: string = 'Missing credentials.';
    public get basicMissingCredentialsError() : string { return this.#basicMissingCredentialsError; }
    public set basicMissingCredentialsError( basicMissingCredentialsError: string ) { 
        if( basicMissingCredentialsError ) { this.#basicMissingCredentialsError = basicMissingCredentialsError; }
    }
    #basicInvalidCredentialsError: string = 'Invalid credentials.';
    public get basicInvalidCredentialsError(): string { return this.#basicInvalidCredentialsError; }
    public set basicInvalidCredentialsError( basicInvalidCredentialsError: string ) {
        if( basicInvalidCredentialsError ) { this.#basicInvalidCredentialsError = basicInvalidCredentialsError; }
    }
    #basicRememberMeLabel: string = 'Remember me';
    public get basicRememberMeLabel(): string { return this.#basicRememberMeLabel; }
    public set basicRememberMeLabel( basicRememberMeLabel: string ) {
        if( basicRememberMeLabel ) { this.#basicRememberMeLabel = basicRememberMeLabel; }
    }

    public generateBasicHtml(rememberMe: boolean): string {
        const buffer: string[] = [];

        buffer.push( '<!DOCTYPE html> <html>' );
        this.generateHeader( (s: string) => buffer.push( s ) );
        this.generateBody( rememberMe, (s: string) => buffer.push( s ) );
        buffer.push( '</html>' );

        return buffer.join( ' ' );
    }
    
    protected generateHeader( collector: Collector ): void {
        collector('<head> <title>');
        collector( this.popupTitle );
        collector( '</title>' );
        this.generateStyle( collector );
        collector('</head>');
    }

    protected generateStyle( collector: Collector ): void {
        collector( '<style>' );
        collector(
`body{
font-family: Avenir,Helvetica,Arial,sans-serif;
-webkit-font-smoothing: antialiased;
-moz-osx-font-smoothing: grayscale;
text-align: center;
color: #2c3e50;
margin-top: 60px;
}
h1{
font-weight: 400;
}
.error{
margin: auto auto 10px;
width: 40%;
background-color: rgb(239, 181, 181);
border-radius: 5px;
padding: 3px;
font-size: 80%;
color: rgb(226, 28, 28);
display: none;
}` );
        collector( '</style>' );
    }
    
    protected generateBody( rememberMe: boolean, collector: Collector ): void {
        collector( '<body> <h1>' );
        collector( this.basicFormTitle );
        collector( '</h1> <div id="error-div" class="error"> <span id="error"></span> </div> <div class="form"> <input type="text" id="username-input" placeholder="' );
        collector( this.basicUserNamePlaceholder );
        collector( '" class="username-input"/> <input type="password" id="password-input" placeholder="' );
        collector( this.basicPasswordPlaceholder );
        collector( '" class="password-input"/> <input type="checkbox" id="remember-me-input"' );
        if( rememberMe ) collector( 'checked' );
        collector( 'class="remember-me-input"/> <label for="remember-me-input" class="remember-me-label">' );
        collector( this.basicRememberMeLabel );
        collector( '</label> </div> <button id="submit-button">' );
        collector( this.#basicSubmitButtonLabel );
        collector( '</button>' );
        this.generateScript( collector );
        collector( '</body>');
    }

    protected generateScript( collector: Collector ): void {
    }
}
