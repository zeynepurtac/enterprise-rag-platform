// Production configuration. The Angular bundle is served by nginx, which
// reverse-proxies "/api" to the backend container, so a relative URL is used.
export const environment = {
  production: true,
  apiUrl: '/api'
};
