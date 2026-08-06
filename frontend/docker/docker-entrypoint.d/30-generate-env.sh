#!/bin/sh
# Regenerates env.js from env.template.js using the container's API_BASE_URL/HUB_BASE_URL env
# vars, overwriting the checked-in `ng serve` dev fallback baked into the image at build time.
# Runs automatically: this directory is sourced by the official nginx image's entrypoint before
# nginx starts (see /docker-entrypoint.sh in nginxinc/nginx-unprivileged).
set -eu

envsubst '${API_BASE_URL} ${HUB_BASE_URL}' \
  < /usr/share/nginx/html/env.template.js \
  > /usr/share/nginx/html/env.js

echo "30-generate-env.sh: wrote env.js (apiBaseUrl=${API_BASE_URL}, hubBaseUrl=${HUB_BASE_URL})"
