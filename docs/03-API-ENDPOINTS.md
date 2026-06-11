# Karasu ERP — API Endpoint Catalog

> Base: `/api/v1` | Auth: `Bearer JWT` | Tenant: JWT claim veya `X-Tenant-Id`

---

## 1. Authentication & Identity

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/auth/register` | Public | Yeni tenant + owner kayıt |
| POST | `/auth/login` | Public | JWT + Refresh token |
| POST | `/auth/refresh` | Public | Token rotation |
| POST | `/auth/logout` | Authenticated | Refresh token revoke |
| POST | `/auth/forgot-password` | Public | Şifre sıfırlama e-postası |
| POST | `/auth/reset-password` | Public | Şifre sıfırlama |
| GET | `/auth/me` | Authenticated | Mevcut kullanıcı profili |
| PUT | `/auth/me` | Authenticated | Profil güncelle |
| PUT | `/auth/change-password` | Authenticated | Şifre değiştir |

---

## 2. Tenant & Branch Management

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/tenants/current` | Authenticated | Mevcut tenant bilgisi |
| PUT | `/tenants/current` | CompanyOwner | Tenant ayarları güncelle |
| GET | `/tenants/current/settings` | Manager+ | Modül ayarları |
| PUT | `/tenants/current/settings` | CompanyOwner | Modül ayarları güncelle |
| GET | `/branches` | Branch.View | Şube listesi |
| GET | `/branches/{id}` | Branch.View | Şube detay |
| POST | `/branches` | Branch.Create | Yeni şube |
| PUT | `/branches/{id}` | Branch.Update | Şube güncelle |
| DELETE | `/branches/{id}` | Branch.Delete | Şube sil (soft) |

---

## 3. User & Role Management

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/users` | User.View | Kullanıcı listesi |
| GET | `/users/{id}` | User.View | Kullanıcı detay |
| POST | `/users` | User.Create | Kullanıcı oluştur |
| PUT | `/users/{id}` | User.Update | Kullanıcı güncelle |
| DELETE | `/users/{id}` | User.Delete | Kullanıcı deaktif |
| GET | `/roles` | Role.View | Rol listesi |
| POST | `/roles` | Role.Create | Rol oluştur |
| PUT | `/roles/{id}` | Role.Update | Rol güncelle |
| PUT | `/roles/{id}/permissions` | Role.Update | Rol izinleri ata |
| GET | `/permissions` | Role.View | Tüm izin kataloğu |

---

## 4. Dashboard

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/dashboard/summary` | Dashboard.View | KPI özeti |
| GET | `/dashboard/sales-trend` | Dashboard.View | Satış trendi (daily/weekly/monthly) |
| GET | `/dashboard/revenue-expense` | Dashboard.View | Gelir-gider analizi |
| GET | `/dashboard/top-products` | Dashboard.View | En çok satan ürünler |
| GET | `/dashboard/branch-comparison` | Dashboard.View | Şube karşılaştırma |
| GET | `/dashboard/recent-activities` | Dashboard.View | Son işlemler |
| GET | `/dashboard/critical-stock` | Dashboard.View | Kritik stok listesi |
| GET | `/dashboard/pending-orders` | Dashboard.View | Bekleyen siparişler |

---

## 5. Product Management

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/products` | Product.View | Ürün listesi (paginated, filter) |
| GET | `/products/{id}` | Product.View | Ürün detay + varyantlar |
| POST | `/products` | Product.Create | Yeni ürün |
| PUT | `/products/{id}` | Product.Update | Ürün güncelle |
| DELETE | `/products/{id}` | Product.Delete | Ürün sil (soft) |
| POST | `/products/{id}/variants` | Product.Create | Varyant ekle |
| PUT | `/products/{id}/variants/{variantId}` | Product.Update | Varyant güncelle |
| DELETE | `/products/{id}/variants/{variantId}` | Product.Delete | Varyant sil |
| POST | `/products/import` | Product.Import | Excel toplu import |
| GET | `/products/export` | Product.Export | Excel export |
| GET | `/products/barcode/{barcode}` | Product.View | Barkod ile arama |
| POST | `/products/{id}/barcode/generate` | Product.Update | Barkod oluştur |
| POST | `/products/{id}/qrcode/generate` | Product.Update | QR kod oluştur |
| GET | `/categories` | Product.View | Kategori listesi (tree) |
| POST | `/categories` | Product.Create | Kategori oluştur |
| PUT | `/categories/{id}` | Product.Update | Kategori güncelle |
| DELETE | `/categories/{id}` | Product.Delete | Kategori sil |
| GET | `/brands` | Product.View | Marka listesi |
| POST | `/brands` | Product.Create | Marka oluştur |

---

## 6. Customer Management (CRM)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/customers` | Customer.View | Müşteri listesi |
| GET | `/customers/{id}` | Customer.View | Müşteri detay |
| POST | `/customers` | Customer.Create | Yeni müşteri |
| PUT | `/customers/{id}` | Customer.Update | Müşteri güncelle |
| DELETE | `/customers/{id}` | Customer.Delete | Müşteri sil |
| GET | `/customers/{id}/orders` | Customer.View | Sipariş geçmişi |
| GET | `/customers/{id}/payments` | Customer.View | Ödeme geçmişi |
| GET | `/customers/{id}/balance` | Customer.View | Cari bakiye |
| GET | `/customers/{id}/notes` | Customer.View | Notlar |
| POST | `/customers/{id}/notes` | Customer.Update | Not ekle |
| POST | `/customers/{id}/attachments` | Customer.Update | Dosya yükle |
| GET | `/customers/{id}/attachments` | Customer.View | Dosya listesi |

---

## 7. Order Management

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/orders` | Order.View | Sipariş listesi |
| GET | `/orders/{id}` | Order.View | Sipariş detay |
| POST | `/orders` | Order.Create | Yeni sipariş |
| PUT | `/orders/{id}` | Order.Update | Sipariş güncelle |
| DELETE | `/orders/{id}` | Order.Delete | Sipariş sil (Draft only) |
| PATCH | `/orders/{id}/status` | Order.Update | Durum değiştir |
| POST | `/orders/{id}/confirm` | Order.Confirm | Onayla |
| POST | `/orders/{id}/cancel` | Order.Cancel | İptal et |
| GET | `/orders/{id}/history` | Order.View | Durum geçmişi |
| GET | `/quotes` | Quote.View | Teklif listesi |
| POST | `/quotes` | Quote.Create | Teklif oluştur |
| PUT | `/quotes/{id}` | Quote.Update | Teklif güncelle |
| POST | `/quotes/{id}/convert` | Quote.Convert | Siparişe dönüştür |
| GET | `/invoices` | Invoice.View | Fatura listesi |
| GET | `/invoices/{id}` | Invoice.View | Fatura detay |
| POST | `/invoices` | Invoice.Create | Fatura oluştur |
| POST | `/orders/{id}/invoice` | Invoice.Create | Siparişten fatura |

---

## 8. POS (Point of Sale)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/pos/sessions/open` | Pos.Open | Kasa oturumu aç |
| POST | `/pos/sessions/{id}/close` | Pos.Close | Kasa oturumu kapat |
| GET | `/pos/sessions/current` | Pos.View | Aktif oturum |
| POST | `/pos/sales` | Pos.Sell | Hızlı satış |
| POST | `/pos/sales/{id}/payments` | Pos.Sell | Çoklu ödeme ekle |
| POST | `/pos/returns` | Pos.Return | İade işlemi |
| GET | `/pos/products/search` | Pos.View | Barkod/isim arama |
| POST | `/pos/receipt/{orderId}/print` | Pos.View | Fiş yazdır (PDF) |

---

## 9. Inventory Management

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/warehouses` | Warehouse.View | Depo listesi |
| POST | `/warehouses` | Warehouse.Create | Depo oluştur |
| PUT | `/warehouses/{id}` | Warehouse.Update | Depo güncelle |
| GET | `/stock` | Stock.View | Stok listesi (filter by warehouse) |
| GET | `/stock/{productVariantId}` | Stock.View | Ürün stok durumu |
| GET | `/stock/movements` | Stock.View | Stok hareketleri |
| POST | `/stock/adjustments` | Stock.Adjust | Stok düzeltme |
| GET | `/stock/transfers` | Stock.View | Transfer listesi |
| POST | `/stock/transfers` | Stock.Transfer | Transfer oluştur |
| PATCH | `/stock/transfers/{id}/complete` | Stock.Transfer | Transfer tamamla |
| GET | `/stock/counts` | Stock.View | Sayım listesi |
| POST | `/stock/counts` | Stock.Count | Sayım başlat |
| PUT | `/stock/counts/{id}/lines` | Stock.Count | Sayım satırları güncelle |
| POST | `/stock/counts/{id}/complete` | Stock.Count | Sayım tamamla |
| GET | `/stock/alerts` | Stock.View | Kritik stok uyarıları |

---

## 10. Finance

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/finance/cash-registers` | Finance.View | Kasa listesi |
| POST | `/finance/cash-registers` | Finance.Create | Kasa oluştur |
| GET | `/finance/cash-registers/{id}/transactions` | Finance.View | Kasa hareketleri |
| POST | `/finance/cash-transactions` | Finance.Create | Kasa işlemi |
| GET | `/finance/bank-accounts` | Finance.View | Banka hesapları |
| POST | `/finance/bank-accounts` | Finance.Create | Banka hesabı ekle |
| GET | `/finance/bank-accounts/{id}/transactions` | Finance.View | Banka hareketleri |
| POST | `/finance/bank-transactions` | Finance.Create | Banka işlemi |
| GET | `/finance/expenses` | Finance.View | Gider listesi |
| POST | `/finance/expenses` | Finance.Create | Gider kaydet |
| GET | `/finance/incomes` | Finance.View | Gelir listesi |
| POST | `/finance/incomes` | Finance.Create | Gelir kaydet |
| GET | `/finance/receivables` | Finance.View | Alacaklar |
| GET | `/finance/payables` | Finance.View | Borçlar |
| POST | `/finance/payments` | Finance.Create | Tahsilat/ödeme |
| GET | `/finance/summary` | Finance.View | Finansal özet |

---

## 11. Human Resources

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/hr/employees` | Hr.View | Personel listesi |
| POST | `/hr/employees` | Hr.Create | Personel ekle |
| PUT | `/hr/employees/{id}` | Hr.Update | Personel güncelle |
| GET | `/hr/leave-requests` | Hr.View | İzin talepleri |
| POST | `/hr/leave-requests` | Hr.Create | İzin talebi |
| PATCH | `/hr/leave-requests/{id}/approve` | Hr.Approve | İzin onayla |
| GET | `/hr/shifts` | Hr.View | Vardiya listesi |
| POST | `/hr/shifts` | Hr.Create | Vardiya oluştur |
| GET | `/hr/payrolls` | Hr.View | Maaş bordroları |
| POST | `/hr/payrolls/generate` | Hr.Create | Bordro oluştur |

---

## 12. Supplier & Procurement

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/suppliers` | Supplier.View | Tedarikçi listesi |
| POST | `/suppliers` | Supplier.Create | Tedarikçi ekle |
| PUT | `/suppliers/{id}` | Supplier.Update | Tedarikçi güncelle |
| GET | `/suppliers/{id}/performance` | Supplier.View | Performans analizi |
| GET | `/purchase-orders` | PurchaseOrder.View | Satın alma siparişleri |
| POST | `/purchase-orders` | PurchaseOrder.Create | PO oluştur |
| PATCH | `/purchase-orders/{id}/receive` | PurchaseOrder.Receive | Mal kabul |

---

## 13. E-Invoice (Integration Ready)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/einvoice/profile` | EInvoice.View | Entegratör profili |
| PUT | `/einvoice/profile` | EInvoice.Configure | Profil yapılandır |
| POST | `/einvoice/submit/{invoiceId}` | EInvoice.Submit | E-Fatura gönder |
| POST | `/einvoice/archive/{invoiceId}` | EInvoice.Submit | E-Arşiv gönder |
| POST | `/einvoice/dispatch/{orderId}` | EInvoice.Submit | E-İrsaliye gönder |
| GET | `/einvoice/submissions` | EInvoice.View | Gönderim geçmişi |
| GET | `/einvoice/submissions/{id}/status` | EInvoice.View | GIB durum sorgula |

---

## 14. Notifications (REST + SignalR)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/notifications` | Authenticated | Bildirim listesi |
| PATCH | `/notifications/{id}/read` | Authenticated | Okundu işaretle |
| PATCH | `/notifications/read-all` | Authenticated | Tümünü okundu |
| WS | `/hubs/notifications` | Authenticated | SignalR hub |

**SignalR Events:**
- `NewOrder` — Yeni sipariş
- `CriticalStock` — Kritik stok
- `PaymentReceived` — Tahsilat
- `SystemError` — Hata bildirimi
- `SystemAnnouncement` — Duyuru

---

## 15. Reporting

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/reports/sales` | Report.Sales | Satış raporu |
| GET | `/reports/profit-loss` | Report.Finance | Kar-zarar |
| GET | `/reports/income-expense` | Report.Finance | Gelir-gider |
| GET | `/reports/customers` | Report.Customer | Müşteri raporu |
| GET | `/reports/products` | Report.Product | Ürün raporu |
| GET | `/reports/stock` | Report.Stock | Stok raporu |
| GET | `/reports/{type}/export` | Report.Export | PDF/Excel/CSV export |

Query params: `?format=pdf|excel|csv&from=2026-01-01&to=2026-06-11&branchId=...`

---

## 16. Audit & Health

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/audit-logs` | Audit.View | Denetim kayıtları |
| GET | `/health` | Public | Health check |
| GET | `/health/ready` | Public | Readiness probe |
| GET | `/health/live` | Public | Liveness probe |

---

## 17. SuperAdmin (Platform Level)

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/admin/tenants` | SuperAdmin | Tüm tenant'lar |
| POST | `/admin/tenants` | SuperAdmin | Tenant oluştur |
| PATCH | `/admin/tenants/{id}/suspend` | SuperAdmin | Tenant askıya al |
| GET | `/admin/metrics` | SuperAdmin | Platform metrikleri |
