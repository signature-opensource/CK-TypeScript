import { InjectionToken } from '@angular/core';

/**
 * An injection token that can be used in a DI provider to override the WebSocket endpoint of the
 * channel. Optional: it defaults to '/ws', which is the server side
 * WebApplicationBuilderExtensions.DefaultPath. Provide it only when the server maps another path.
 */
export const WS_CONNECTION_URL = new InjectionToken<string>('WSConnectionUrl');
