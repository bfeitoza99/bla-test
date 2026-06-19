import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PagePlaceholder } from '../../../shared/ui';

/**
 * Register page (placeholder).
 *
 * SKELETON ONLY: no reactive form or HTTP yet. A later wave adds a reactive
 * form (validation mirroring the API password policy) that calls
 * `AuthService.register(...)`, which uses the generated OpenAPI client.
 */
@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PagePlaceholder, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {}
