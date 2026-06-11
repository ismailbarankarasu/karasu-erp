# Karasu ERP — Enterprise Architecture Overview

> **Versiyon:** 1.0 | **Hedef:** Logo Tiger / Mikro / Nebim / SAP B1 / Paraşüt seviyesinde SaaS ERP

---

## 1. Executive Summary

Karasu ERP, çok kiracılı (multi-tenant), modüler, event-driven bir enterprise SaaS platformudur. Perakende, restoran, toptan, distribütör ve e-ticaret segmentlerini tek platformda destekler.

### Mimari Prensipler

| Prensip | Uygulama |
|---------|----------|
| **Separation of Concerns** | Clean Architecture katmanları |
| **Domain Centricity** | DDD Aggregates, Value Objects, Domain Events |
| **CQRS** | Command/Query ayrımı, read model optimizasyonu |
| **Event Driven** | Outbox pattern, async işlemler |
| **Tenant Isolation** | Shared DB + TenantId + Global Query Filters |
| **API First** | REST + SignalR, OpenAPI 3.1 |

---

## 2. Solution Structure (Clean Architecture)

```
Karasu.ERP/
├── src/
│   ├── Karasu.ERP.Api/                 # Presentation — Controllers, SignalR Hubs, Middleware
│   ├── Karasu.ERP.Application/         # Application — CQRS, MediatR, Validators, DTOs
│   ├── Karasu.ERP.Domain/              # Domain — Entities, Aggregates, Events, Interfaces
│   ├── Karasu.ERP.Infrastructure/      # Infrastructure — Cache, Email, File Storage, External APIs
│   ├── Karasu.ERP.Persistence/         # Persistence — EF Core, Repositories, Migrations
│   ├── Karasu.ERP.Identity/            # Identity — JWT, Refresh Token, Permissions
│   └── Karasu.ERP.Shared/              # Shared — Constants, Enums, Extensions, Result<T>
├── tests/
│   ├── Karasu.ERP.UnitTests/
│   ├── Karasu.ERP.IntegrationTests/
│   └── Karasu.ERP.ArchitectureTests/   # NetArchTest kuralları
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.worker
│   └── docker-compose.yml
├── k8s/                                # Kubernetes manifests
├── pipelines/                          # GitHub Actions + Azure DevOps
└── docs/
```

### Katman Bağımlılık Kuralları

```
Api → Application → Domain ← Persistence
         ↓              ↑
    Infrastructure ─────┘
    Identity ─────────────┘
```

- **Domain** hiçbir katmana bağımlı değildir
- **Application** sadece Domain'e bağımlıdır
- **Persistence/Infrastructure** Domain interface'lerini implement eder
- **Api** tüm katmanları DI ile compose eder

---

## 3. Bounded Contexts (DDD)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Karasu ERP Platform                          │
├─────────────┬─────────────┬─────────────┬───────────────────────┤
│  Identity   │  Catalog    │  Sales      │  Inventory            │
│  & Access   │  (Product)  │  (Order/POS)│  (Stock/Warehouse)    │
├─────────────┼─────────────┼─────────────┼───────────────────────┤
│  CRM        │  Finance    │  HR         │  Procurement          │
│  (Customer) │  (Cash/Bank)│  (Employee) │  (Supplier/PO)        │
├─────────────┼─────────────┼─────────────┼───────────────────────┤
│  Reporting  │  E-Invoice  │  Notification│  Tenant Management   │
│  & Analytics│  (GIB)      │  (SignalR)  │  (Multi-Tenant)       │
└─────────────┴─────────────┴─────────────┴───────────────────────┘
```

Her bounded context kendi Aggregate root'larına sahiptir. Cross-context iletişim **Domain Events** ve **Integration Events** ile yapılır.

---

## 4. Multi-Tenant Architecture

### Strateji: Shared Database, Shared Schema, TenantId Discriminator

```
Request → TenantResolver Middleware
              ↓
         JWT Claim: tenant_id
         OR Subdomain: acme.karasuerp.com
         OR Header: X-Tenant-Id
              ↓
         ITenantContext (Scoped)
              ↓
         EF Global Query Filter: .Where(e => e.TenantId == _tenantId)
              ↓
         SaveChanges: Auto-set TenantId on insert
```

### Tenant Entity

| Alan | Açıklama |
|------|----------|
| Id | GUID |
| Name | Firma adı |
| Slug | Subdomain (acme) |
| BusinessType | Retail, Restaurant, Wholesale, ECommerce |
| SubscriptionPlan | Free, Starter, Pro, Enterprise |
| IsActive | Aktif/pasif |
| Settings | JSON — para birimi, KDV, fiş ayarları |

### İzolasyon Katmanları

1. **Application Layer** — `ITenantContext` zorunlu
2. **Persistence Layer** — Global query filter
3. **Cache Layer** — Key prefix: `tenant:{tenantId}:...`
4. **File Storage** — Path: `/tenants/{tenantId}/...`
5. **SignalR** — Group: `tenant-{tenantId}`

---

## 5. Authorization Model

### Rol Hiyerarşisi

```
SuperAdmin (Platform)
    └── CompanyOwner (Tenant Admin)
            ├── Manager
            ├── Accountant
            ├── SalesPerson
            └── Cashier
```

### Permission Format

`{Module}.{Entity}.{Action}`

Örnekler:
- `Order.Order.Create`
- `Product.Product.View`
- `Finance.CashRegister.Close`
- `Report.Sales.Export`

### Implementation

- ASP.NET Identity + Custom `Permission` entity
- Policy-based authorization: `[Authorize(Policy = "Order.Create")]`
- Permission claims JWT'ye embed edilir (refresh'te güncellenir)
- SuperAdmin tüm tenant'lara erişir (bypass filter)

---

## 6. Event-Driven Architecture

### Domain Events (In-Process)

```
OrderConfirmedEvent → StockReservationHandler
                   → NotificationHandler
                   → AuditHandler
```

### Integration Events (Outbox Pattern)

```
OrderConfirmed → Outbox Table → Message Broker (RabbitMQ/Azure Service Bus)
                                      ↓
                              Reporting Service (async aggregate)
                              E-Invoice Service (async submission)
```

### SignalR Real-Time

| Event | Hub Group | Client Action |
|-------|-----------|---------------|
| NewOrder | tenant-{id}, branch-{id} | Dashboard refresh |
| CriticalStock | tenant-{id} | Alert toast |
| PaymentReceived | tenant-{id} | Finance update |

---

## 7. Cross-Cutting Concerns

### Audit Trail

Her değişiklik `AuditLog` tablosuna:
- UserId, TenantId, EntityType, EntityId
- Action (Create/Update/Delete)
- OldValues (JSON), NewValues (JSON)
- IpAddress, UserAgent, Timestamp

EF Core `SaveChangesInterceptor` ile otomatik.

### Performance (< 300ms)

| Teknik | Kullanım |
|--------|----------|
| Redis Distributed Cache | Product catalog, permissions, dashboard KPIs |
| Read Replicas | Reporting queries (PostgreSQL) |
| Pagination | Tüm list endpoint'leri (cursor + offset) |
| Projection Queries | CQRS read side — sadece gerekli kolonlar |
| Connection Pooling | EF Core + Redis multiplexer |
| Response Compression | Brotli/Gzip |
| CDN | Static assets, product images |

### Security

| Tehdit | Önlem |
|--------|-------|
| XSS | Input sanitization, CSP headers |
| CSRF | Anti-forgery tokens (cookie-based flows) |
| SQL Injection | Parameterized queries (EF Core) |
| Rate Limiting | AspNetCoreRateLimit — 100 req/min per IP |
| Token Theft | Refresh token rotation + family invalidation |
| Tenant Leakage | Global filter + integration tests |

---

## 8. Technology Stack Mapping

| Concern | Technology |
|---------|------------|
| API | ASP.NET Core 8 Minimal APIs + Controllers |
| ORM | EF Core 8 (SQL Server primary, PostgreSQL secondary) |
| CQRS | MediatR 12+ |
| Validation | FluentValidation |
| Mapping | AutoMapper / Mapster |
| Cache | StackExchange.Redis |
| Real-time | SignalR + Redis backplane |
| Logging | Serilog → Elasticsearch |
| Monitoring | OpenTelemetry → App Insights |
| Health | AspNetCore.HealthChecks.* |
| Background Jobs | Hangfire / Quartz.NET |
| File Export | ClosedXML (Excel), QuestPDF (PDF) |

---

## 9. API Design Conventions

```
Base URL: https://api.karasuerp.com/v1
Auth: Bearer JWT
Tenant: X-Tenant-Id header (optional if in JWT)

Response Envelope:
{
  "success": true,
  "data": { ... },
  "meta": { "page": 1, "pageSize": 20, "totalCount": 150 },
  "errors": null
}

Error Envelope:
{
  "success": false,
  "data": null,
  "errors": [{ "code": "ORDER_NOT_FOUND", "message": "..." }]
}
```

HTTP Status: 200 OK, 201 Created, 204 No Content, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict, 429 Too Many Requests

---

## 10. Module Activation (Feature Flags)

Her tenant farklı modül seti kullanabilir:

```json
{
  "modules": {
    "pos": true,
    "hr": false,
    "eInvoice": true,
    "restaurant": true
  }
}
```

Restoran tenant'ı: Masa yönetimi, mutfak ekranı aktif.
Toptancı tenant'ı: Toptan fiyat listesi, minimum sipariş miktarı aktif.
