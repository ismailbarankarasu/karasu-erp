# Karasu ERP — Redis Cache Strategy

---

## 1. Architecture

```
API Instance 1 ──┐
API Instance 2 ──┼──→ Redis Cluster (Primary + Replica)
API Instance 3 ──┘         ↓
                    SignalR Backplane
                    Distributed Lock
                    Rate Limit Counter
                    Session/Token Blacklist
```

**Connection:** `StackExchange.Redis` with `IConnectionMultiplexer` singleton + `IDistributedCache` abstraction.

---

## 2. Key Naming Convention

```
{env}:{tenantId}:{module}:{entity}:{identifier}

Examples:
  prod:abc123:product:catalog:list:page1
  prod:abc123:dashboard:summary:2026-06-11
  prod:abc123:auth:permissions:user456
  prod:abc123:stock:alert:critical
  prod:global:rate-limit:ip:192.168.1.1
```

---

## 3. Cache Scenarios

### 3.1 Product Catalog (High Read)

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `product:list:{hash}` | 5 min | Product Create/Update/Delete |
| `product:{id}` | 15 min | Product Update/Delete |
| `product:barcode:{barcode}` | 15 min | Product Update |
| `category:tree` | 30 min | Category CRUD |
| `brand:list` | 30 min | Brand CRUD |

**POS barkod araması:** `product:barcode:{barcode}` — target < 10ms

```csharp
var cacheKey = $"product:barcode:{barcode}";
var product = await _cache.GetOrSetAsync(cacheKey, 
    () => _repo.GetByBarcodeAsync(barcode), 
    TimeSpan.FromMinutes(15));
```

### 3.2 Dashboard KPIs (Aggregated)

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `dashboard:summary:{date}` | 2 min | Order/Payment events |
| `dashboard:sales-trend:{period}` | 5 min | OrderConfirmed event |
| `dashboard:top-products:{period}` | 10 min | Nightly refresh + event |
| `dashboard:branch-comparison` | 5 min | OrderConfirmed event |

**Strategy:** Write-through on event + short TTL fallback.

### 3.3 Authorization & Permissions

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `auth:permissions:{userId}` | 30 min | Role/Permission change |
| `auth:roles:{userId}` | 30 min | Role assignment change |
| `auth:token-blacklist:{jti}` | Token expiry | Logout/Revoke |

JWT validation sonrası permission check cache'den — DB'ye gitmez.

### 3.4 Stock & Inventory

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `stock:item:{warehouseId}:{variantId}` | 1 min | Stock movement |
| `stock:alerts:critical` | 2 min | Stock below min |
| `stock:reservation:{orderId}` | 30 min | Order confirm/cancel |

**Distributed Lock:** Stok rezervasyonu için RedLock pattern:
```
LOCK stock:reserve:{variantId}:{warehouseId} TTL 5s
```

### 3.5 Customer Balance

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `customer:balance:{customerId}` | 5 min | Payment/Invoice events |

### 3.6 Rate Limiting

| Key | TTL | Purpose |
|-----|-----|---------|
| `rate:ip:{ip}:minute` | 60s | 100 req/min per IP |
| `rate:user:{userId}:minute` | 60s | 200 req/min per user |
| `rate:login:{ip}` | 15 min | Brute force protection (5 attempts) |

### 3.7 SignalR Backplane

Redis backplane ile multi-instance SignalR:
```csharp
services.AddSignalR().AddStackExchangeRedis(connectionString);
```

Groups: `tenant-{tenantId}`, `branch-{branchId}`, `user-{userId}`

### 3.8 Report Cache (Expensive Queries)

| Key | TTL | Invalidation |
|-----|-----|--------------|
| `report:sales:{hash}` | 15 min | Manual refresh |
| `report:profit-loss:{period}` | 30 min | End of day rebuild |

`{hash}` = MD5 of query parameters (date range, branch, filters)

### 3.9 E-Invoice Status Polling

| Key | TTL | Purpose |
|-----|-----|---------|
| `einvoice:status:{submissionId}` | 5 min | GIB response cache |

---

## 4. Cache Invalidation Patterns

### Event-Driven Invalidation

```
ProductUpdatedEvent → Invalidate:
  - product:{id}
  - product:barcode:{barcode}
  - product:list:*  (pattern delete)
  - category:tree (if category changed)

OrderConfirmedEvent → Invalidate:
  - dashboard:summary:*
  - dashboard:sales-trend:*
  - stock:item:{warehouse}:{variant}
  - customer:balance:{customerId}
```

### Pattern Delete (SCAN)

```csharp
await foreach (var key in server.KeysAsync(pattern: $"prod:{tenantId}:product:list:*"))
    await _cache.RemoveAsync(key);
```

---

## 5. Cache-Aside vs Write-Through

| Scenario | Pattern | Reason |
|----------|---------|--------|
| Product catalog | Cache-Aside | Read-heavy, stale OK |
| Dashboard KPI | Write-Through | Event-triggered refresh |
| Permissions | Cache-Aside + long TTL | Rarely changes |
| Stock quantity | Cache-Aside + short TTL | Must be fresh |
| Rate limiting | Write-Through (INCR) | Atomic counter |

---

## 6. Redis Data Structures Usage

| Structure | Use Case |
|-----------|----------|
| **String** | Serialized DTOs, counters |
| **Hash** | Product variant attributes |
| **Set** | User's active sessions |
| **Sorted Set** | Top selling products ranking |
| **Pub/Sub** | Cache invalidation broadcast |
| **Stream** | Event replay (optional) |

---

## 7. Fallback & Resilience

```csharp
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
{
    try
    {
        var cached = await _cache.GetAsync<T>(key);
        if (cached != null) return cached;
        
        var data = await factory();
        await _cache.SetAsync(key, data, ttl);
        return data;
    }
    catch (RedisException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable, falling back to DB");
        return await factory();  // Graceful degradation
    }
}
```

---

## 8. Memory & Eviction Policy

```
maxmemory: 2gb
maxmemory-policy: allkeys-lru
```

Tenant başına memory quota (opsiyonel): `INFO memory` monitoring ile Enterprise plan tenant'ları için reserved memory.

---

## 9. Monitoring Metrics

| Metric | Alert Threshold |
|--------|-----------------|
| Cache hit ratio | < 80% |
| Average get latency | > 5ms |
| Connected clients | > 1000 |
| Memory usage | > 80% |
| Evicted keys/min | > 100 |

OpenTelemetry → Redis exporter → Grafana/App Insights
