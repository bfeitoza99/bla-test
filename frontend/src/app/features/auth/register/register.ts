import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth';
import { toUserMessage } from '../../../core/http/http-error';
import { passwordPolicyValidator } from '../password.validator';

/**
 * Register page: a reactive sign-up form wired to the real `AuthService`
 * (`POST /api/auth/register`). Validation mirrors the API password policy
 * (min 8 chars with upper/lower/digit). On success the user is signed in and
 * routed to the tasks page.
 */
@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: '../auth.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, passwordPolicyValidator()]],
  });

  /** Human-readable list of unmet password requirements, for inline feedback. */
  protected passwordRequirements(): string[] {
    const errors = this.form.controls.password.errors;
    const policy = errors?.['passwordPolicy'] as string[] | undefined;
    return policy ?? [];
  }

  protected submit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        void this.router.navigateByUrl('/tasks');
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.serverError.set(toUserMessage(error));
      },
    });
  }
}
