# syntax=docker/dockerfile:1

# --- Build ---------------------------------------------------------------------------------
FROM node:24-alpine AS build
WORKDIR /app

# Copy just the manifest files first so `npm ci` is cached across builds until a dependency
# actually changes, instead of being invalidated by every source edit.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ .
RUN npm run build -- --configuration=production

# --- Runtime ---------------------------------------------------------------------------------
FROM nginxinc/nginx-unprivileged:alpine AS final

COPY frontend/docker/nginx.conf /etc/nginx/conf.d/default.conf
# --chown=nginx:nginx: the entrypoint script below runs as the unprivileged `nginx` user and must
# be able to overwrite env.js in place at container start — root-owned files (COPY's default)
# would make that fail with "Permission denied".
COPY --chown=nginx:nginx --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY --chown=nginx:nginx frontend/docker/env.template.js /usr/share/nginx/html/env.template.js
COPY frontend/docker/docker-entrypoint.d/30-generate-env.sh /docker-entrypoint.d/30-generate-env.sh

USER root
RUN chmod +x /docker-entrypoint.d/30-generate-env.sh
USER nginx

EXPOSE 8080
