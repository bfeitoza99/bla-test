import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth';

/**
 * App shell: responsive header with primary nav + a router-outlet for feature
 * pages. The sign-in/sign-out control reflects the AuthService stub's token
 * state.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isAuthenticated = this.authService.isAuthenticated;

  /** Clears the token and returns to the login page. */
  protected logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
