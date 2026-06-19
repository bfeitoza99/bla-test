import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/**
 * JWT bearer interceptor (functional interceptor, Angular standalone style).
 *
 * - Reads the token from the AuthService (backed by `localStorage`) and, when
 *   present, attaches `Authorization: Bearer <token>` to outgoing requests.
 * - On a `401 Unauthorized`, clears the token and redirects to `/login` — the
 *   token is missing/expired/invalid, so the session is over.
 *
 * See ai-context/rules/security.md (Frontend section).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.token();
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        authService.clearToken();
        void router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
