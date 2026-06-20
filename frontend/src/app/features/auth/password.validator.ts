import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Mirrors the API password policy: at least 8 characters with an uppercase
 * letter, a lowercase letter, and a digit. The server remains authoritative —
 * this only gives fast, inline feedback.
 */
export function passwordPolicyValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    if (!value) {
      return null; // `required` handles emptiness.
    }

    const failures: string[] = [];
    if (value.length < 8) {
      failures.push('at least 8 characters');
    }
    if (!/[A-Z]/.test(value)) {
      failures.push('an uppercase letter');
    }
    if (!/[a-z]/.test(value)) {
      failures.push('a lowercase letter');
    }
    if (!/\d/.test(value)) {
      failures.push('a number');
    }

    return failures.length > 0 ? { passwordPolicy: failures } : null;
  };
}
