import { Signal, signal, WritableSignal } from '@angular/core';

/**
 * This service requires an explicit parameter: it cannot be Injectable,
 * we need a Provider to instantiate it.
 *
 * The DemoNgModule handles this registration thanks to the [NgProviderImport] and
 * [NgProvider] attributes.
 * 
 */
export class SomeAuthService {

    #someParameter: string;
    #userName: WritableSignal<string | undefined> = signal(undefined);

    /**
     * User name signal (read only).
     */
    userName: Signal<string | undefined> = this.#userName.asReadonly();

    get someParameter() : string {
        return this.#someParameter;
    }

    get isAnonymous(): boolean {
        return this.#userName === undefined;
    }

    login(userName: string) {
        this.#userName.set(userName);
    }

    logout() {
        this.#userName.set(undefined);
    }

    constructor(requiredParameter: string) {
        this.#someParameter = requiredParameter;
    }
}
