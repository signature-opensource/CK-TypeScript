import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { WSConnection } from '@local/ck-gen';

/**
 * Opens the one WebSocket connection of the application during the bootstrap.
 *
 * It opens it and does not wait for it: the initializer returns nothing, so Angular renders straight
 * away. Waiting would buy nothing and cost a blank page - the socket starts opening at this very
 * instant either way, and the data of a feature comes from the Cris command it sends afterwards, which
 * happens after the bootstrap regardless. A connection is a network condition, not a precondition of
 * the application.
 *
 * Each feature waits where it can say so: ObservableDomainClient through whenConnectedAsync() in its
 * detached reconnect loop, the session channel through the onConnected callback of its topic. A
 * feature that needs to show something while the socket is coming up watches its own state - for
 * observable domains, ObservableDomainClient.connectionState$.
 *
 * @returns EnvironmentProviders that start the WSConnection.
 */
export function provideWSConnectionSupport(): EnvironmentProviders {
    return makeEnvironmentProviders( [
        provideAppInitializer( () => { inject( WSConnection ).start(); } )
    ] );
}
