import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  CreateTaskRequest,
  TaskResponse,
  TaskStatus,
  TasksService,
  UpdateTaskRequest,
} from '../../core/api/generated';

/** A normalized page of tasks with numeric paging metadata. */
export interface TaskPage {
  items: TaskResponse[];
  page: number;
  pageSize: number;
  total: number;
}

/**
 * Thin facade over the generated `TasksService`.
 *
 * Keeps feature components free of the generated method names and normalizes
 * `PagedResultOfTaskResponse` (whose paging fields are loosely typed as
 * `any | null` in the contract) into a clean numeric `TaskPage`.
 */
@Injectable({ providedIn: 'root' })
export class TasksDataService {
  private readonly api = inject(TasksService);

  list(page: number, pageSize: number, status?: TaskStatus): Observable<TaskPage> {
    return this.api.apiTasksGet(page, pageSize, status).pipe(
      map((result) => ({
        items: result.items ?? [],
        page: toNumber(result.page, page),
        pageSize: toNumber(result.pageSize, pageSize),
        total: toNumber(result.total, result.items?.length ?? 0),
      })),
    );
  }

  get(id: string): Observable<TaskResponse> {
    return this.api.apiTasksIdGet(id);
  }

  create(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.api.apiTasksPost(request);
  }

  update(id: string, request: UpdateTaskRequest): Observable<TaskResponse> {
    return this.api.apiTasksIdPut(id, request);
  }

  delete(id: string): Observable<unknown> {
    return this.api.apiTasksIdDelete(id);
  }
}

function toNumber(value: unknown, fallback: number): number {
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}
