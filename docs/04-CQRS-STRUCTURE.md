# Karasu ERP — CQRS & MediatR Structure

---

## 1. Application Layer Folder Structure

```
Karasu.ERP.Application/
├── Common/
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs          # FluentValidation pipeline
│   │   ├── LoggingBehavior.cs             # Request/response logging
│   │   ├── PerformanceBehavior.cs         # Slow query detection (>300ms)
│   │   ├── TenantBehavior.cs              # Tenant context injection
│   │   └── TransactionBehavior.cs           # UoW transaction wrapper
│   ├── Interfaces/
│   │   ├── IApplicationDbContext.cs
│   │   ├── ICurrentUserService.cs
│   │   ├── ITenantContext.cs
│   │   ├── ICacheService.cs
│   │   ├── IDateTimeService.cs
│   │   └── IOutboxService.cs
│   ├── Mappings/
│   │   └── MappingProfile.cs                # AutoMapper profiles
│   ├── Models/
│   │   ├── Result.cs
│   │   ├── PaginatedList.cs
│   │   └── ApiResponse.cs
│   └── Exceptions/
│       ├── NotFoundException.cs
│       ├── ValidationException.cs
│       └── ForbiddenException.cs
│
├── Features/
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── Login/
│   │   │   │   ├── LoginCommand.cs
│   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   └── LoginCommandValidator.cs
│   │   │   ├── Register/
│   │   │   └── RefreshToken/
│   │   └── Queries/
│   │       └── GetCurrentUser/
│   │
│   ├── Products/
│   │   ├── Commands/
│   │   │   ├── CreateProduct/
│   │   │   ├── UpdateProduct/
│   │   │   ├── DeleteProduct/
│   │   │   ├── ImportProducts/
│   │   │   └── CreateProductVariant/
│   │   └── Queries/
│   │       ├── GetProducts/
│   │       ├── GetProductById/
│   │       ├── GetProductByBarcode/
│   │       └── ExportProducts/
│   │
│   ├── Orders/
│   │   ├── Commands/
│   │   │   ├── CreateOrder/
│   │   │   ├── UpdateOrder/
│   │   │   ├── ConfirmOrder/
│   │   │   ├── CancelOrder/
│   │   │   └── ChangeOrderStatus/
│   │   ├── Queries/
│   │   │   ├── GetOrders/
│   │   │   ├── GetOrderById/
│   │   │   └── GetOrderHistory/
│   │   └── EventHandlers/
│   │       └── OrderConfirmedEventHandler.cs
│   │
│   ├── Customers/
│   ├── Inventory/
│   ├── Finance/
│   ├── Pos/
│   ├── Hr/
│   ├── Suppliers/
│   ├── Dashboard/
│   ├── Reports/
│   ├── Notifications/
│   └── EInvoice/
│
└── DependencyInjection.cs
```

---

## 2. Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `{Action}{Entity}Command` | `CreateOrderCommand` |
| Command Handler | `{Action}{Entity}CommandHandler` | `CreateOrderCommandHandler` |
| Query | `Get{Entity}Query` / `Get{Entities}Query` | `GetOrdersQuery` |
| Query Handler | `Get{Entity}QueryHandler` | `GetOrdersQueryHandler` |
| Validator | `{Command}Validator` | `CreateOrderCommandValidator` |
| DTO | `{Entity}Dto` | `OrderDto` |
| Response | `{Entity}Response` | `OrderDetailResponse` |

---

## 3. Command/Query Separation Rules

### Commands (Write Side)
- Mutate state via Aggregate roots
- Return `Result<T>` or `Result`
- Raise Domain Events
- Wrapped in transaction (UnitOfWork)
- Invalidate related cache keys

### Queries (Read Side)
- Read-only, no side effects
- Direct projection (no domain logic)
- Cache-friendly
- Pagination mandatory for lists
- No transaction needed

---

## 4. MediatR Pipeline Flow

```
HTTP Request
    ↓
Controller → IMediator.Send(command/query)
    ↓
┌─────────────────────────────────┐
│ 1. LoggingBehavior              │
│ 2. PerformanceBehavior          │
│ 3. TenantBehavior               │
│ 4. ValidationBehavior           │
│ 5. TransactionBehavior (cmd)    │
└─────────────────────────────────┘
    ↓
Handler → Repository/Aggregate
    ↓
Domain Events → EventHandlers
    ↓
Response → ApiResponse<T>
```

---

## 5. Feature Slice Example: Orders Module

```
Features/Orders/
├── Commands/
│   ├── CreateOrder/
│   │   ├── CreateOrderCommand.cs
│   │   ├── CreateOrderCommandHandler.cs
│   │   └── CreateOrderCommandValidator.cs
│   ├── ConfirmOrder/
│   └── CancelOrder/
├── Queries/
│   ├── GetOrders/
│   │   ├── GetOrdersQuery.cs
│   │   ├── GetOrdersQueryHandler.cs
│   │   └── OrderListDto.cs
│   └── GetOrderById/
├── EventHandlers/
│   ├── OrderConfirmedEventHandler.cs      # Stock reservation
│   └── OrderCreatedNotificationHandler.cs # SignalR push
└── Dtos/
    ├── OrderDto.cs
    ├── OrderLineDto.cs
    └── OrderDetailDto.cs
```

---

## 6. Domain Events Flow

```csharp
// Domain Layer
public class Order : TenantEntity, IAggregateRoot
{
    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, TenantId, Lines));
    }
}

// Application Layer — Event Handler
public class OrderConfirmedEventHandler 
    : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent e, CancellationToken ct)
    {
        // 1. Reserve stock
        // 2. Write to outbox for reporting
        // 3. Send SignalR notification
    }
}
```

---

## 7. Repository + Unit of Work

```csharp
// Domain
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}

// Persistence
public class OrderRepository : IOrderRepository { ... }
public class UnitOfWork : IUnitOfWork { ... }
```

---

## 8. Validation Strategy

- **FluentValidation** per command/query
- **Domain validation** in aggregate methods (invariants)
- **ValidationBehavior** runs all validators before handler

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("En az bir satır gerekli");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.ProductVariantId).NotEmpty();
        });
    }
}
```
