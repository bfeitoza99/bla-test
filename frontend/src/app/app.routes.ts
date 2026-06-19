import { Routes } from '@angular/router';

/**
 * Top-level routes.
 *
 * - `/login`, `/register` → public auth feature.
 * - `/tasks` → protected tasks feature (guarded inside `tasks.routes.ts`).
 * - default redirects to `/tasks` (the guard bounces unauthenticated users to
 *   `/login`).
 */
export const routes: Routes = [
  {
    path: '',
    redirectTo: 'tasks',
    pathMatch: 'full',
  },
  {
    path: '',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'tasks',
    loadChildren: () =>
      import('./features/tasks/tasks.routes').then((m) => m.tasksRoutes),
  },
  {
    path: '**',
    redirectTo: 'tasks',
  },
];
