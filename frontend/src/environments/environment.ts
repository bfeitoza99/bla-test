/**
 * Production environment.
 *
 * `apiBaseUrl` is the root of the BLA .NET API. The generated OpenAPI client
 * (see `core/api/generated/`) and the JWT interceptor are configured from this
 * value, so the HTTP layer never hard-codes a host.
 */
export const environment = {
  production: true,
  apiBaseUrl: 'http://localhost:8080',
};
