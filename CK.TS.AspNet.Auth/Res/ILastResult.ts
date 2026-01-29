/**
 * Captures the last interaction with the backend.
 */
export interface ILastResult {

    /**
     * Gets the server data that has been sent to the backend and may have been modified.
     */
    serverData?: {[index:string]: string | null};

    /**
     * Gets the error if any.
     */
    error?: IWebFrontAuthError;
}

export interface IWebFrontAuthError {
    readonly type: string;
    readonly errorId: string;
    readonly errorText: string;
    readonly error: IResponseError | ILoginError
}

export interface IResponseError {
    readonly errorId: string;
    readonly errorText: string;
}

export interface ILoginError {
    readonly loginFailureCode: number;
    readonly loginFailureReason: string;
}

export class WebFrontAuthError implements IWebFrontAuthError {
    public readonly type: string;
    public readonly errorId: string;
    public readonly errorText: string;

    constructor(public readonly error: IResponseError | ILoginError) {
        if (this.isResponseError(error)) {
            this.type = "Protocol";
            this.errorId = error.errorId;
            this.errorText = error.errorText;
        } else if (this.isLoginError(error)) {
            this.type = "Login";
            this.errorId = error.loginFailureCode.toString();
            this.errorText = error.loginFailureReason;
        } else {
            throw new Error(`Invalid argument: error ${error}`);
        }
    }

    protected isResponseError(error: IResponseError | ILoginError): error is IResponseError {
        const cast = (error as IResponseError);
        return !!cast.errorId || !!cast.errorText;
    }

    protected isLoginError(error: IResponseError | ILoginError): error is ILoginError {
        const cast = (error as ILoginError);
        return !!cast.loginFailureCode && !!cast.loginFailureReason;
    }
}

