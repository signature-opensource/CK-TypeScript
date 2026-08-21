import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { WSConnection } from '@local/ck-gen';

// How long the bootstrap waits for the first connection. Long enough for a healthy server on a slow
// link, short enough that a server that is down costs a pause and not a blank page.
const INITIAL_CONNECTION_TIMEOUT_MS = 5000;

/**
 * Opens the one WebSocket connection of the application, and holds the bootstrap until it is
 * established: because provideAppInitializer awaits its promise, a feature initialized afterwards
 * finds a connection that already has its identifier.
 *
 * The wait is bounded. A connection is a network condition, not a precondition of the application: if
 * the server cannot be reached the bootstrap goes on after a few seconds while WSConnection keeps
 * retrying with its backoff. Correctness does not rely on this initializer succeeding - every feature
 * awaits whenConnectedAsync() on its own - it only makes the nominal case orderly.
 *
 * @returns EnvironmentProviders that start the WSConnection.
 */
export function provideWSConnectionSupport(): EnvironmentProviders {
    return makeEnvironmentProviders( [
        provideAppInitializer( startWSConnectionAsync )
    ] );
}

async function startWSConnectionAsync(): Promise<void> {
    const connection = inject( WSConnection );
    connection.start();
    let timer: ReturnType<typeof setTimeout> | undefined;
    const timeout = new Promise<'timeout'>( resolve => {
        timer = setTimeout( () => resolve( 'timeout' ), INITIAL_CONNECTION_TIMEOUT_MS );
    } );
    try {
        if ( await Promise.race( [connection.whenConnectedAsync(), timeout] ) === 'timeout' ) {
            console.warn(
                `WSConnection: no connection after ${INITIAL_CONNECTION_TIMEOUT_MS} ms, starting anyway. `
                + 'Features using the channel will connect as soon as the server answers.'
            );
        }
    } finally {
        // Without this the timer keeps the bootstrap task alive for nothing once we are connected.
        if ( timer !== undefined ) clearTimeout( timer );
    }
}
