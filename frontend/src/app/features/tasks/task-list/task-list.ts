import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import {
  CreateTaskRequest,
  TaskResponse,
  TaskStatus,
} from '../../../core/api/generated';
import { toUserMessage } from '../../../core/http/http-error';
import { TaskForm } from '../task-form/task-form';
import { TasksDataService } from '../tasks-data.service';
import {
  TASK_STATUS_OPTIONS,
  taskStatusLabel,
  taskStatusModifier,
} from '../task-status.util';

type StatusFilter = TaskStatus | 'All';

/** Editor dialog state: closed, creating, or editing a specific task. */
type EditorState =
  | { mode: 'closed' }
  | { mode: 'create' }
  | { mode: 'edit'; task: TaskResponse };

const DEFAULT_PAGE_SIZE = 10;

/**
 * Task list: paginated, status-filterable list of the signed-in user's tasks
 * with create / edit (shared `TaskForm`) and delete-with-confirm. Handles
 * loading, empty, and error states; refetches after every mutation to stay in
 * sync with server-side paging/filtering.
 */
@Component({
  selector: 'app-task-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TaskForm, DatePipe],
  templateUrl: './task-list.html',
  styleUrl: './task-list.scss',
})
export class TaskList {
  private readonly data = inject(TasksDataService);

  protected readonly statusOptions = TASK_STATUS_OPTIONS;
  protected readonly statusLabel = taskStatusLabel;
  protected readonly statusModifier = taskStatusModifier;

  protected readonly tasks = signal<TaskResponse[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  protected readonly statusFilter = signal<StatusFilter>('All');

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly editor = signal<EditorState>({ mode: 'closed' });
  protected readonly saving = signal(false);
  protected readonly pendingDelete = signal<TaskResponse | null>(null);
  protected readonly deleting = signal(false);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize())),
  );
  protected readonly isEmpty = computed(
    () => !this.loading() && !this.error() && this.tasks().length === 0,
  );
  protected readonly rangeStart = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize(), this.total()),
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    const filter = this.statusFilter();
    const status = filter === 'All' ? undefined : filter;

    this.data.list(this.page(), this.pageSize(), status).subscribe({
      next: (result) => {
        this.tasks.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(toUserMessage(err, 'Could not load tasks.'));
        this.loading.set(false);
      },
    });
  }

  protected onFilterChange(value: string): void {
    this.statusFilter.set(value as StatusFilter);
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.load();
  }

  protected startCreate(): void {
    this.editor.set({ mode: 'create' });
  }

  protected startEdit(task: TaskResponse): void {
    this.editor.set({ mode: 'edit', task });
  }

  protected closeEditor(): void {
    this.editor.set({ mode: 'closed' });
    this.saving.set(false);
  }

  protected get editorTask(): TaskResponse | null {
    const state = this.editor();
    return state.mode === 'edit' ? state.task : null;
  }

  protected saveTask(request: CreateTaskRequest): void {
    const state = this.editor();
    if (state.mode === 'closed') {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const request$ =
      state.mode === 'edit'
        ? this.data.update(state.task.id, request)
        : this.data.create(request);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEditor();
        // After creating, jump to the first page to reveal the new task.
        if (state.mode === 'create') {
          this.page.set(1);
        }
        this.load();
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(toUserMessage(err, 'Could not save the task.'));
      },
    });
  }

  protected requestDelete(task: TaskResponse): void {
    this.pendingDelete.set(task);
  }

  protected cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  protected confirmDelete(): void {
    const task = this.pendingDelete();
    if (!task) {
      return;
    }

    this.deleting.set(true);
    this.error.set(null);

    this.data.delete(task.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDelete.set(null);
        // If we removed the last item on a page, step back a page.
        if (this.tasks().length === 1 && this.page() > 1) {
          this.page.set(this.page() - 1);
        }
        this.load();
      },
      error: (err: unknown) => {
        this.deleting.set(false);
        this.error.set(toUserMessage(err, 'Could not delete the task.'));
      },
    });
  }
}
