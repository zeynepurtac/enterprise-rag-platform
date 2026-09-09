# ---- Build stage --------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm install

COPY . .
RUN npm run build

# ---- Runtime stage ------------------------------------------------------
FROM nginx:1.27-alpine AS final

# SPA + reverse-proxy configuration.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Angular's application builder emits the browser bundle under /browser.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
