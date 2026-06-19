import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * Route guard for authenticated areas (the Tasks feature).
 *
 * Allows activation when a token is present; otherwise redirects to `/login`,
 * preserving the attempted URL as a `returnUrl` query param so the user can be
 * sent back after signing in.
 *
 * Note: this is a client-side gate for UX only — the API is authoritative and
 * rejects unauthenticated requests with `401` (see security.md).
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
