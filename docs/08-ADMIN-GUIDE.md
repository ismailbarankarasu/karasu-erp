# Karasu ERP — Admin Kullanım Kılavuzu

Bu kılavuz, Karasu ERP backend API'sini kullanan yönetici ve operasyon ekipleri içindir.

---

## 1. Başlangıç

### 1.1 İlk Kurulum

1. Şirket kaydı: `POST /api/v1/auth/register`
2. Giriş: `POST /api/v1/auth/login`
3. JWT token tüm isteklerde `Authorization: Bearer {token}` olarak gönderilir

### 1.2 Roller

| Rol | Yetki |
|-----|-------|
| **CompanyOwner** | Tüm modüllere tam erişim |
| **Admin** | Yapılandırılmış permission seti |
| **Cashier** | POS ve satış |
| **Warehouse** | Stok ve depo |

---

## 2. Modül Rehberi

### 2.1 Ürün & Stok

- Ürün oluşturma: `POST /api/v1/products`
- Stok görüntüleme: `GET /api/v1/stock`
- Stok düzeltme: `POST /api/v1/stock/adjust`
- Depo transferi: `POST /api/v1/stock/transfers`

### 2.2 Satış & Sipariş

- Sipariş oluşturma: `POST /api/v1/orders`
- Sipariş onaylama: `POST /api/v1/orders/{id}/confirm`
- POS satış: `POST /api/v1/pos/sales`
- Teklif → Sipariş: `POST /api/v1/quotes/{id}/convert`

### 2.3 Finans

- Kasa/Banka: `GET/POST /api/v1/finance/*`
- Tahsilat: `POST /api/v1/finance/payments`
- Alacak/Borç: `GET /api/v1/finance/receivables`, `/payables`

### 2.4 Raporlama

- Dashboard KPI: `GET /api/v1/dashboard/summary`
- Satış raporu: `GET /api/v1/reports/sales`
- Excel/PDF export: `GET /api/v1/reports/{type}/export?format=csv`

### 2.5 HR & Tedarikçi

- Personel: `GET/POST /api/v1/hr/employees`
- İzin onayı: `PATCH /api/v1/hr/leave-requests/{id}/approve`
- Satın alma: `POST /api/v1/purchase-orders`
- Mal kabul: `PATCH /api/v1/purchase-orders/{id}/receive`

### 2.6 E-Fatura

1. Profil yapılandır: `PUT /api/v1/einvoice/profile`
2. Fatura kes: `POST /api/v1/orders/{id}/invoice`
3. E-Fatura gönder: `POST /api/v1/einvoice/submit/{invoiceId}`
4. E-İrsaliye: `POST /api/v1/einvoice/dispatch/{orderId}`

### 2.7 Bildirimler

- REST: `GET /api/v1/notifications`
- Real-time: SignalR hub `/hubs/notifications`
- Eventler: `NewOrder`, `CriticalStock`, `PaymentReceived`

---

## 3. Güvenlik

- JWT token süresi: 15 dakika (yenileme: `POST /api/v1/auth/refresh`)
- Rate limit: 100 istek/dk (API), 10 istek/dk (auth)
- Tüm yanıtlarda güvenlik header'ları aktif
- Production'da HTTPS zorunlu

---

## 4. Ortam Yapılandırması

| Ortam | URL | Veritabanı |
|-------|-----|------------|
| Local | localhost:5000 | Docker SQL Server |
| Staging | staging-api.karasuerp.com | Staging DB |
| Production | api.karasuerp.com | SQL Server / PostgreSQL |

### PostgreSQL Kullanımı

```bash
dotnet run --project src/Karasu.ERP.Api --launch-profile PostgreSQL
# veya
ASPNETCORE_ENVIRONMENT=PostgreSQL dotnet run --project src/Karasu.ERP.Api
```

`appsettings.PostgreSQL.json` dosyasında provider ayarı bulunur.

---

## 5. Sağlık Kontrolleri

| Endpoint | Amaç |
|----------|------|
| `GET /health` | Genel sağlık |
| `GET /health/live` | Liveness probe (K8s) |
| `GET /health/ready` | Readiness probe (K8s) |

---

## 6. API Dokümantasyonu

Development/Staging ortamında Swagger UI:

```
http://localhost:5280/swagger
```

OpenAPI JSON:

```
http://localhost:5000/openapi/v1/openapi.json
```

---

## 7. Destek

- GitHub: https://github.com/ismailbarankarasu/karasu-erp
- Teknik dokümantasyon: `docs/` klasörü
