import { HttpErrorResponse } from '@angular/common/http';

import { ProblemDetails } from '../api/generated';

/**
 * Maps an HTTP failure to a friendly, user-facing message.
 *
 * Handles the API's `ProblemDetails` shape (validation `400`s and others),
 * `401` (invalid credentials / expired session), and network/offline errors,
 * falling back to a generic message so the UI never shows a raw stack.
 */
export function toUserMessage(
  error: unknown,
  fallback = 'Something went wrong. Please try again.',
): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  // Network / CORS / server unreachable: status 0, no body.
  if (error.status === 0) {
    return 'Could not reach the server. Check your connection and try again.';
  }

  if (error.status === 401) {
    return 'Invalid email or password.';
  }

  const problem = extractProblem(error);
  if (problem) {
    // Prefer the validation detail, then the title.
    return problem.detail?.trim() || problem.title?.trim() || fallback;
  }

  if (typeof error.error === 'string' && error.error.trim()) {
    return error.error.trim();
  }

  return fallback;
}

function extractProblem(error: HttpErrorResponse): ProblemDetails | null {
  const body: unknown = error.error;
  if (body && typeof body === 'object' && ('title' in body || 'detail' in body)) {
    return body as ProblemDetails;
  }
  return null;
}
