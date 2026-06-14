# Karasu ERP — k6 Load Tests

1000 eşzamanlı kullanıcı ve p95 < 300ms hedefi için yük testi senaryoları.

## Gereksinimler

- [k6](https://k6.io/docs/get-started/installation/) kurulu olmalı

## Çalıştırma

```bash
# Smoke test (10 VU, 30s)
k6 run --env API_URL=http://localhost:5000 load-tests/k6/api-load-test.js --scenario smoke

# Tam yük testi (1000 VU ramp)
k6 run --env API_URL=http://localhost:5000 load-tests/k6/api-load-test.js --scenario load
```

## Eşikler

| Metrik | Hedef |
|--------|-------|
| `http_req_duration` p95 | < 300ms |
| `errors` rate | < 5% |

## Notlar

- Test öncesi API ve Redis çalışır durumda olmalı
- Production ortamında dikkatli kullanın; rate limiting devreye girebilir
