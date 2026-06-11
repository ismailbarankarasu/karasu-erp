# Karasu ERP — Sprint-Based Development Plan

> **Sprint süresi:** 2 hafta | **Takım:** 4-6 developer + 1 QA + 1 DevOps

---

## Phase 0: Foundation (Sprint 1-2)

### Sprint 1 — Solution Scaffold & Core Infrastructure
| Story | Points | Deliverable |
|-------|--------|-------------|
| Solution structure (Clean Architecture) | 5 | 7 proje, DI setup |
| EF Core + SQL Server + Migrations base | 8 | BaseEntity, TenantEntity |
| Multi-tenant middleware + global filter | 8 | ITenantContext |
| ASP.NET Identity + JWT + Refresh Token | 13 | Auth endpoints |
| Serilog + Elasticsearch integration | 5 | Structured logging |
| Health checks | 3 | /health endpoints |
| Docker Compose (local dev) | 5 | API + SQL + Redis + ES |
| **Total** | **47** | |

### Sprint 2 — Authorization & Audit
| Story | Points | Deliverable |
|-------|--------|-------------|
| Role & Permission system | 8 | RBAC + policy-based auth |
| Permission seed data (all modules) | 5 | 50+ permissions |
| Audit log interceptor | 8 | Auto audit on SaveChanges |
| FluentValidation pipeline | 3 | ValidationBehavior |
| MediatR pipeline behaviors | 5 | Logging, Performance, Tenant |
| Unit test infrastructure | 5 | xUnit + Moq + FluentAssertions |
| Architecture tests (NetArchTest) | 3 | Layer dependency rules |
| CI pipeline (GitHub Actions) | 5 | Build + test on PR |
| **Total** | **42** | |

**Milestone:** Auth çalışır, tenant izolasyonu test edilmiş, CI green.

---

## Phase 1: Core Business (Sprint 3-6)

### Sprint 3 — Product Management
| Story | Points | Deliverable |
|-------|--------|-------------|
| Product CRUD (CQRS) | 8 | Create/Update/Delete/View |
| Category & Brand management | 5 | Hierarchical categories |
| Product variants (size, color) | 8 | Variant CRUD |
| Barcode & QR generation | 5 | ZXing.Net integration |
| Redis cache for products | 5 | Cache-aside pattern |
| Excel import/export | 8 | ClosedXML |
| Product API tests | 5 | Integration tests |
| **Total** | **44** | |

### Sprint 4 — Customer Management (CRM)
| Story | Points | Deliverable |
|-------|--------|-------------|
| Customer CRUD | 5 | Full CRM card |
| Customer notes & attachments | 5 | File upload to blob |
| Balance tracking | 8 | Cari hesap |
| Order history per customer | 3 | Query projection |
| Payment history per customer | 3 | Query projection |
| Customer search (filter, pagination) | 5 | Full-text search |
| **Total** | **29** | |

### Sprint 5 — Order Management
| Story | Points | Deliverable |
|-------|--------|-------------|
| Order CRUD + status workflow | 13 | State machine |
| Quote → Order conversion | 8 | Teklif akışı |
| Order lines & calculations | 8 | KDV, indirim, toplam |
| Invoice generation from order | 8 | Fatura oluşturma |
| Order domain events | 5 | OrderConfirmed, etc. |
| Order status history | 3 | Audit trail |
| SignalR: NewOrder notification | 5 | Real-time push |
| **Total** | **50** | |

### Sprint 6 — Inventory Management
| Story | Points | Deliverable |
|-------|--------|-------------|
| Warehouse CRUD | 5 | Multi-warehouse |
| Stock items & movements | 8 | In/Out/Adjust |
| Stock transfer between warehouses | 8 | Transfer workflow |
| Stock count (inventory) | 8 | Sayım işlemi |
| Critical stock alerts | 5 | SignalR + Redis |
| Stock reservation on order confirm | 8 | Distributed lock |
| **Total** | **42** | |

**Milestone:** Ürün → Müşteri → Sipariş → Stok akışı uçtan uca çalışır.

---

## Phase 2: Sales & Finance (Sprint 7-9)

### Sprint 7 — POS Module
| Story | Points | Deliverable |
|-------|--------|-------------|
| POS session (open/close) | 8 | Kasa oturumu |
| Quick sale flow | 13 | Barkod + touch UI API |
| Multi-payment support | 8 | Nakit/Kart/Havale/Veresiye |
| Return/refund processing | 8 | İade akışı |
| Receipt printing (PDF) | 5 | QuestPDF |
| POS ↔ Order ↔ Stock integration | 8 | End-to-end |
| **Total** | **50** | |

### Sprint 8 — Finance Module
| Story | Points | Deliverable |
|-------|--------|-------------|
| Cash register management | 5 | Kasa yönetimi |
| Bank account management | 5 | Banka hesapları |
| Income & expense tracking | 8 | Gelir/gider |
| Payment collection (tahsilat) | 8 | Cari tahsilat |
| Receivables & payables | 8 | Alacak/borç takibi |
| Financial summary queries | 5 | Dashboard data |
| **Total** | **39** | |

### Sprint 9 — Dashboard & Reporting
| Story | Points | Deliverable |
|-------|--------|-------------|
| Dashboard KPI endpoints | 8 | Summary, trends |
| Sales report (filter, export) | 8 | PDF/Excel/CSV |
| Profit-loss report | 8 | Kar-zarar |
| Income-expense report | 5 | Gelir-gider |
| Product & customer reports | 5 | Detay raporlar |
| Stock report | 5 | Stok raporu |
| Read model projections (CQRS) | 8 | Event-driven aggregates |
| Redis cache for dashboard | 3 | KPI caching |
| **Total** | **50** | |

**Milestone:** POS satış yapılır, finans takip edilir, raporlar üretilir.

---

## Phase 3: Extended Modules (Sprint 10-12)

### Sprint 10 — HR & Supplier
| Story | Points | Deliverable |
|-------|--------|-------------|
| Employee management | 5 | Personel kartı |
| Leave management | 5 | İzin talep/onay |
| Shift management | 5 | Vardiya planlama |
| Payroll tracking | 8 | Maaş bordrosu |
| Supplier CRUD | 5 | Tedarikçi kartı |
| Purchase orders | 8 | Satın alma siparişi |
| Supplier performance | 5 | Analiz |
| **Total** | **41** | |

### Sprint 11 — E-Invoice & Notifications
| Story | Points | Deliverable |
|-------|--------|-------------|
| E-Invoice profile & config | 5 | Entegratör ayarları |
| E-Invoice submission (adapter pattern) | 13 | Provider abstraction |
| E-Archive & E-Dispatch | 8 | E-Arşiv, E-İrsaliye |
| Notification system (REST) | 5 | Bildirim CRUD |
| SignalR notification hub | 8 | Real-time events |
| Outbox pattern implementation | 8 | Reliable messaging |
| **Total** | **47** | |

### Sprint 12 — Hardening & Launch Prep
| Story | Points | Deliverable |
|-------|--------|-------------|
| Rate limiting | 3 | AspNetCoreRateLimit |
| CSRF & XSS hardening | 5 | Security headers |
| PostgreSQL provider support | 8 | Dual DB provider |
| Performance optimization | 8 | < 300ms target |
| Load testing (k6) | 5 | 1000 concurrent users |
| Kubernetes manifests | 8 | K8s deployment |
| Azure/AWS deployment | 8 | Production deploy |
| API documentation (Swagger) | 3 | OpenAPI 3.1 |
| User documentation | 5 | Admin guide |
| **Total** | **53** | |

**Milestone:** Production-ready MVP launch.

---

## Phase 4: Post-MVP (Sprint 13+)

| Sprint | Focus |
|--------|-------|
| 13-14 | Frontend (React/Blazor) — Admin panel |
| 15-16 | Frontend — POS touch screen UI |
| 17-18 | Mobile app (React Native / .NET MAUI) |
| 19-20 | E-Ticaret entegrasyonu (Trendyol, Hepsiburada) |
| 21-22 | Restaurant module (table management, kitchen display) |
| 23-24 | Multi-currency & multi-language |
| 25-26 | AI-powered demand forecasting |
| 27-28 | Marketplace & subscription billing (Stripe/Iyzico) |

---

## Timeline Summary

```
Sprint  1-2:  Foundation          ████████
Sprint  3-6:  Core Business     ████████████████
Sprint  7-9:  Sales & Finance   ████████████
Sprint 10-12: Extended Modules  ████████████
Sprint 13+:   Frontend & Growth ████████████████████████

Total MVP: ~24 weeks (6 months) with 4-6 developers
```

---

## Team Allocation

| Role | Sprint 1-6 | Sprint 7-12 |
|------|-----------|-------------|
| Backend Dev x2 | Core modules | POS, Finance, Reports |
| Backend Dev x1 | Infrastructure | E-Invoice, Notifications |
| Frontend Dev x1 | API contract | Dashboard UI (Sprint 9+) |
| QA x1 | Test plans | E2E automation |
| DevOps x0.5 | Docker, CI | K8s, cloud deploy |
| Architect x0.5 | Review, ADRs | Performance, security |

---

## Definition of Done

- [ ] Code reviewed & merged to develop
- [ ] Unit tests (>80% coverage on handlers)
- [ ] Integration tests for API endpoints
- [ ] FluentValidation rules complete
- [ ] Permission checks applied
- [ ] Audit log verified
- [ ] Redis cache invalidation tested
- [ ] Swagger documentation updated
- [ ] No critical/high SonarQube issues
- [ ] QA sign-off
