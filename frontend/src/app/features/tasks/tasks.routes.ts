import { Routes } from '@angular/router';

import { authGuard } from '../../core/auth';

/**
 * Tasks feature routes. The whole feature is protected by `authGuard` — only
 * authenticated users reach it. Lazy-loaded from the app routes.
 */
export const tasksRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    title: 'My tasks · BLA',
    loadComponent: () => import('./task-list/task-list').then((m) => m.TaskList),
  },
];
