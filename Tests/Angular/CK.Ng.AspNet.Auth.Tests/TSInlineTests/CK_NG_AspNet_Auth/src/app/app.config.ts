import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { CKGenAppModule } from '@local/ck-gen';

export const appConfig: ApplicationConfig = {
  providers:
    [
      provideZoneChangeDetection({ eventCoalescing: true }),
      provideRouter(routes),
      ...CKGenAppModule.Providers
    ]
};

