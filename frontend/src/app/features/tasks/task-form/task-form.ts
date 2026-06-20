import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CreateTaskRequest, TaskResponse, TaskStatus } from '../../../core/api/generated';
import { TASK_STATUS_OPTIONS } from '../task-status.util';

/**
 * Reusable create/edit task form. Presentational: it owns no HTTP. The parent
 * passes an optional `task` to edit (absent → create mode) and listens to
 * `save` for the request payload and `cancel` to dismiss.
 *
 * `CreateTaskRequest` and `UpdateTaskRequest` are structurally identical, so the
 * emitted payload works for both create and update calls.
 */
@Component({
  selector: 'app-task-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './task-form.html',
  styleUrl: './task-form.scss',
})
export class TaskForm {
  private readonly fb = inject(FormBuilder);

  /** When set, the form is in edit mode and is pre-filled from this task. */
  readonly task = input<TaskResponse | null>(null);

  /** True while the parent is persisting — disables the submit button. */
  readonly saving = input(false);

  readonly saveTask = output<CreateTaskRequest>();
  readonly cancelEdit = output<void>();

  protected readonly statusOptions = TASK_STATUS_OPTIONS;
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    status: [TaskStatus.Todo as TaskStatus, [Validators.required]],
    dueDate: ['', [Validators.required]],
  });

  constructor() {
    effect(() => {
      const task = this.task();
      if (task) {
        this.form.setValue({
          title: task.title,
          description: task.description ?? '',
          status: task.status,
          dueDate: toDateInputValue(task.dueDate),
        });
      } else {
        this.form.reset({
          title: '',
          description: '',
          status: TaskStatus.Todo,
          dueDate: '',
        });
      }
    });
  }

  protected get isEdit(): boolean {
    return this.task() !== null;
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const description = value.description.trim();
    this.saveTask.emit({
      title: value.title.trim(),
      description: description.length > 0 ? description : null,
      status: value.status,
      // `date` input gives `YYYY-MM-DD`; send it as an ISO date-time (UTC midnight).
      dueDate: new Date(`${value.dueDate}T00:00:00Z`).toISOString(),
    });
  }
}

/** Converts an API ISO date-time into the `YYYY-MM-DD` a date input expects. */
function toDateInputValue(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toISOString().slice(0, 10);
}
