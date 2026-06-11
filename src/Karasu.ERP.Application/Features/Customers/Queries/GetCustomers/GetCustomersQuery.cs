using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomers;

public record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    CustomerType? Type = null,
    CustomerStatus? Status = null) : IRequest<Result<PaginatedList<CustomerListDto>>>;

public record CustomerListDto(
    Guid Id,
    CustomerType Type,
    string FullName,
    string? CompanyName,
    string? Phone,
    string? Email,
    string? City,
    decimal Balance,
    decimal CreditLimit,
    CustomerStatus Status,
    DateTime CreatedAt);
