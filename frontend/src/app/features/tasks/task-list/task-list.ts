import { ChangeDetectionStrategy, Component } from '@angular/core';

import { PagePlaceholder } from '../../../shared/ui';

/**
 * Task list page (placeholder).
 *
 * SKELETON ONLY: no data yet. A later wave wires this to the generated Tasks
 * client (`GET /api/tasks?page=&pageSize=&status=`) for the paginated,
 * status-filterable list, plus create/edit/delete. This route is protected by
 * `authGuard`.
 */
@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PagePlaceholder],
  templateUrl: './task-list.html',
  styleUrl: './task-list.scss',
})
export class TaskList {}
