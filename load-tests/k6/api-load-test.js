import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const apiDuration = new Trend('api_duration', true);

export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 10,
      duration: '30s',
    },
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '2m', target: 100 },
        { duration: '5m', target: 500 },
        { duration: '2m', target: 1000 },
        { duration: '3m', target: 1000 },
        { duration: '2m', target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'],
    errors: ['rate<0.05'],
    api_duration: ['p(95)<300'],
  },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:5000';

export function setup() {
  const slug = `load-${Date.now()}`;
  const registerRes = http.post(
    `${BASE_URL}/api/v1/auth/register`,
    JSON.stringify({
      companyName: 'Load Test Co',
      slug,
      email: `${slug}@loadtest.com`,
      password: 'Password123',
      fullName: 'Load Tester',
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(registerRes, { 'register ok': (r) => r.status === 200 });
  const token = registerRes.json('data.accessToken');
  return { token };
}

export default function (data) {
  const headers = {
    Authorization: `Bearer ${data.token}`,
    'Content-Type': 'application/json',
  };

  const healthRes = http.get(`${BASE_URL}/health/live`);
  check(healthRes, { 'health ok': (r) => r.status === 200 });
  errorRate.add(healthRes.status !== 200);
  apiDuration.add(healthRes.timings.duration);

  const productsRes = http.get(`${BASE_URL}/api/v1/products?page=1&pageSize=20`, { headers });
  check(productsRes, { 'products ok': (r) => r.status === 200 });
  errorRate.add(productsRes.status !== 200);
  apiDuration.add(productsRes.timings.duration);

  const dashboardRes = http.get(`${BASE_URL}/api/v1/dashboard/summary`, { headers });
  check(dashboardRes, { 'dashboard ok': (r) => r.status === 200 });
  errorRate.add(dashboardRes.status !== 200);
  apiDuration.add(dashboardRes.timings.duration);

  sleep(0.5);
}
