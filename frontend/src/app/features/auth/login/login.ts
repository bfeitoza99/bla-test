import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PagePlaceholder } from '../../../shared/ui';

/**
 * Login page (placeholder).
 *
 * SKELETON ONLY: no reactive form or HTTP yet. A later wave adds a reactive
 * form (validation mirroring the API rules) that calls
 * `AuthService.login(...)`, which in turn uses the generated OpenAPI client.
 */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PagePlaceholder, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {}
