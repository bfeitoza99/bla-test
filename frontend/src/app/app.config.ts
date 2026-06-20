import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth';
import { provideApi } from './core/api/generated';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    // HttpClient + the JWT bearer/401 interceptor. The generated API services
    // inject this same HttpClient, so the interceptor applies to every generated
    // call automatically.
    provideHttpClient(withInterceptors([authInterceptor])),
    // Register the generated OpenAPI client's BASE_PATH from the environment so
    // the generated services target the real API and never hard-code a host.
    provideApi(environment.apiBaseUrl),
  ],
};
