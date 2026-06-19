import { Injectable, computed, signal } from '@angular/core';

/**
 * Stub credentials shapes. These mirror the intended `/api/auth` request bodies
 * (see ai-context/ARCHITECTURE.md). Once the OpenAPI client is generated, the
 * real request/response models live in `core/api/generated/` and these local
 * shapes should be replaced by the generated DTOs.
 */
export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterCredentials {
  email: string;
  password: string;
}

const TOKEN_STORAGE_KEY = 'bla.auth.token';

/**
 * Application auth service.
 *
 * SKELETON ONLY: this is a stub. It owns the single source of truth for the JWT
 * (kept in `localStorage`, per ai-context/rules/security.md) and exposes
 * `login`/`register`/`logout`/`token`. The HTTP calls are intentionally NOT
 * implemented yet — the backend is not running and the auth endpoints are
 * consumed through the generated OpenAPI client in a later wave.
 *
 * Known tradeoff (documented in security.md): a token in `localStorage` is
 * readable by JS and exposed to XSS, and a single ~1h token cannot be revoked
 * before expiry. The production upgrade path is httpOnly + rotating refresh.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Reactive token state so guards/components can react to sign-in changes. */
  private readonly tokenSignal = signal<string | null>(this.readToken());

  /** True when a token is present. Does not validate the token's signature/expiry. */
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  /** The current JWT, or `null` when signed out. */
  token(): string | null {
    return this.tokenSignal();
  }

  /**
   * TODO(api-wave): wire to the generated AuthService
   * (`POST /api/auth/login`). Should call the generated client, then
   * `setToken(response.token)`. Returns a Promise/Observable in the real impl.
   */
  login(_credentials: LoginCredentials): void {
    throw new Error(
      'AuthService.login is a stub — wire it to the generated OpenAPI client (POST /api/auth/login).',
    );
  }

  /**
   * TODO(api-wave): wire to the generated AuthService
   * (`POST /api/auth/register`). On success, log the user in (or redirect to
   * login) per the chosen UX.
   */
  register(_credentials: RegisterCredentials): void {
    throw new Error(
      'AuthService.register is a stub — wire it to the generated OpenAPI client (POST /api/auth/register).',
    );
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
