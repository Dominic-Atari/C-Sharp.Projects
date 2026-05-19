export const environment = {
  production: false,
  // Use the dev proxy so /api/auth and /api/V1 reach the Clients host (7071)
  // while everything else (schools, topics, courses, etc) reaches the Functions host (7072).
  apiBase: '/api',
  apiKey: 'dev-api-key',
  defaultUserId: '',
};
