# syntax=docker/dockerfile:1
# Web app image for the local Docker Compose stack: built with the local sign-in settings (.env.development,
# `npm run build:local`) and served by nginx, which also forwards /api to the API container.

FROM node:26-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
# --ignore-scripts skips postinstall, which only installs the API client generator; the build does not need it.
RUN npm ci --ignore-scripts --no-audit --no-fund
COPY . .
RUN npm run build:local

FROM nginx:1.28-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
