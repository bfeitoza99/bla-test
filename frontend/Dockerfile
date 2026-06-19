# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build stage — Node LTS. Install with `npm ci` (lockfile-exact) then build
# the Angular app for production.
# ---------------------------------------------------------------------------
FROM node:22 AS build
WORKDIR /app

# Install deps first so this layer is cached unless the lockfile changes.
COPY package.json package-lock.json ./
RUN npm ci

# Build the app. The @angular/build:application builder emits the browser
# bundle to dist/frontend/browser (project name "frontend", no custom
# outputPath in angular.json).
COPY . .
RUN npm run build

# ---------------------------------------------------------------------------
# Runtime stage — nginx serving the static browser bundle with SPA fallback.
# Container listens on 80; compose maps it to host 4200.
# ---------------------------------------------------------------------------
FROM nginx:1.27-alpine AS final

# Replace the default site config with our SPA-aware one.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static assets produced by the build stage.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
