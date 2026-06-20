import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import {
  AuthResponse,
  AuthService as GeneratedAuthService,
  LoginRequest,
  RegisterRequest,
  UserResponse,
} from '../api/generated';

const TOKEN_STORAGE_KEY = 'bla.auth.token';

/**
 * Application auth service.
 *
 * Owns the single source of truth for the JWT (kept in `localStorage`, per
 * ai-context/rules/security.md) and wraps the generated OpenAPI `AuthService`
 * for `login` / `register` / `me`.
 *
 * Known tradeoff (documented in security.md): a token in `localStorage` is
 * readable by JS and exposed to XSS, and a single ~1h token cannot be revoked
 * before expiry. The production upgrade path is httpOnly + rotating refresh.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(GeneratedAuthService);

  /** Reactive token state so guards/components can react to sign-in changes. */
  private readonly tokenSignal = signal<string | null>(this.readToken());

  /** True when a token is present. Does not validate the token's signature/expiry. */
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  /** The current JWT, or `null` when signed out. */
  token(): string | null {
    return this.tokenSignal();
  }

  /**
   * Authenticates against `POST /api/auth/login`. On success the returned JWT is
   * persisted and reactive state updates. Errors propagate for the caller to
   * surface (e.g. 401 invalid credentials).
   */
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.api
      .apiAuthLoginPost(credentials)
      .pipe(tap((response) => this.setToken(response.token)));
  }

  /**
   * Creates an account via `POST /api/auth/register`. The API returns a token on
   * success, so we sign the user straight in.
   */
  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.api
      .apiAuthRegisterPost(credentials)
      .pipe(tap((response) => this.setToken(response.token)));
  }

  /** Fetches the current user via `GET /api/auth/me` (authorized). */
  me(): Observable<UserResponse> {
    return this.api.apiAuthMeGet();
  }

  /** Clears the token. Navigation back to /login is handled by the caller. */
  logout(): void {
    this.clearToken();
  }

  /** Persists the JWT to `localStorage` and updates reactive state. */
  setToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
    this.tokenSignal.set(token);
  }

  /** Removes the JWT from `localStorage` and updates reactive state. */
  clearToken(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.tokenSignal.set(null);
  }

  private readToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }
}
