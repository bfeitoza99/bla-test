import { Routes } from '@angular/router';

/**
 * Public auth routes (login / register). Lazy-loaded from the app routes.
 */
export const authRoutes: Routes = [
  {
    path: 'login',
    title: 'Sign in · BLA',
    loadComponent: () => import('./login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account · BLA',
    loadComponent: () => import('./register/register').then((m) => m.Register),
  },
];
