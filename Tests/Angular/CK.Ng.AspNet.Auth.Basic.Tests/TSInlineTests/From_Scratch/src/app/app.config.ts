import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes), ...CKGenAppModule.Providers,
  ]
};

