#!/usr/bin/env node
// Generates public/env.js from docker/env.template.js, substituting API_BASE_URL/HUB_BASE_URL from
// the environment. Runs as an npm `prebuild` hook, automatically before every `npm run build` (see
// package.json) — no separate step to remember.
//
// Why this exists, and how it differs from the Docker path (frontend/docker/*):
//  - Cloudflare Pages is static hosting only — there's no container entrypoint to run a
//    substitution script at *request* time, so the URLs have to be baked in at *build* time
//    instead. Set API_BASE_URL/HUB_BASE_URL as Pages project environment variables
//    (Settings > Environment variables) to `https://api.somospix.com`, and this script picks them
//    up during the Pages build.
//  - Local `npm run build` / the Docker image *build* run with neither var set, so this reproduces
//    the checked-in empty-string defaults in `public/env.js` — no working-tree diff, `ng serve`
//    still falls back to environment.ts as before.
//  - The Docker image additionally regenerates env.js at *container start*
//    (frontend/docker/docker-entrypoint.d/30-generate-env.sh) using envsubst on the same template —
//    that mechanism (runtime, not build-time) is what actually applies for docker-compose/self-host,
//    and overwrites whatever this script baked into the image.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const templatePath = join(__dirname, '..', 'docker', 'env.template.js');
const outputPath = join(__dirname, '..', 'public', 'env.js');

const apiBaseUrl = process.env.API_BASE_URL ?? '';
const hubBaseUrl = process.env.HUB_BASE_URL ?? '';

const template = readFileSync(templatePath, 'utf8');
const output = template
  .replaceAll('${API_BASE_URL}', apiBaseUrl)
  .replaceAll('${HUB_BASE_URL}', hubBaseUrl);

writeFileSync(outputPath, output);
console.log(
  `generate-env.mjs: wrote public/env.js (apiBaseUrl=${apiBaseUrl || '<empty>'}, hubBaseUrl=${hubBaseUrl || '<empty>'})`,
);
