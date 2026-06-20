// Generates the typed Angular client from the live API's OpenAPI document using the
// openapi-generator Docker image — so no local Java/JDK is required, only Docker.
//
// Usage: `npm run generate:api` (the API must be running and serving /openapi/v1.json).
// Override the source with API_OPENAPI_URL if the API isn't on http://localhost:8080.
import { execFileSync } from 'node:child_process';
import { writeFileSync } from 'node:fs';

const SPEC_URL = process.env.API_OPENAPI_URL ?? 'http://localhost:8080/openapi/v1.json';
const IMAGE = 'openapitools/openapi-generator-cli:v7.18.0';
const OUT = 'src/app/core/api/generated';

const res = await fetch(SPEC_URL).catch((err) => {
  console.error(`Could not reach ${SPEC_URL}: ${err.message}. Is the API running?`);
  process.exit(1);
});
if (!res.ok) {
  console.error(`Failed to fetch OpenAPI doc from ${SPEC_URL} (HTTP ${res.status}).`);
  process.exit(1);
}
writeFileSync('openapi.json', await res.text());
console.log(`Fetched OpenAPI doc from ${SPEC_URL}`);

// Docker accepts forward-slash Windows paths (C:/Users/...), which avoids drive-letter
// colon parsing issues in the `-v host:container` argument.
const mount = process.cwd().replace(/\\/g, '/');
execFileSync(
  'docker',
  [
    'run', '--rm',
    '-v', `${mount}:/local`,
    IMAGE,
    'generate',
    '-i', '/local/openapi.json',
    '-g', 'typescript-angular',
    '-o', `/local/${OUT}`,
    '--additional-properties=ngVersion=22.0.0,providedInRoot=true,withInterfaces=true',
  ],
  { stdio: 'inherit' },
);
console.log(`Generated Angular client -> ${OUT}`);
