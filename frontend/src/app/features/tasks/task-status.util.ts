import { TaskStatus } from '../../core/api/generated';

/** Selectable status options with human-readable labels (form + filter). */
export const TASK_STATUS_OPTIONS: readonly { value: TaskStatus; label: string }[] = [
  { value: TaskStatus.Todo, label: 'To do' },
  { value: TaskStatus.InProgress, label: 'In progress' },
  { value: TaskStatus.Done, label: 'Done' },
];

const LABELS: Record<TaskStatus, string> = {
  [TaskStatus.Todo]: 'To do',
  [TaskStatus.InProgress]: 'In progress',
  [TaskStatus.Done]: 'Done',
};

/** Readable label for a status badge. */
export function taskStatusLabel(status: TaskStatus): string {
  return LABELS[status] ?? status;
}

/** Stable CSS modifier suffix for a status (kebab-case), e.g. `in-progress`. */
export function taskStatusModifier(status: TaskStatus): string {
  switch (status) {
    case TaskStatus.Todo:
      return 'todo';
    case TaskStatus.InProgress:
      return 'in-progress';
    case TaskStatus.Done:
      return 'done';
    default:
      return 'todo';
  }
}
