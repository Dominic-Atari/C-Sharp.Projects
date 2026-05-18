export const environment = {
  production: false,
  // Use the dev proxy during local development so browser requests go through the dev server
  // and avoid CORS issues. The proxy forwards `/api` to http://localhost:7071.
  apiBase: '/api',
  apiKey: 'dev-api-key',
  defaultUserId: '',
};
