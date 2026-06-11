using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDetailDto>>;

public record CustomerDetailDto(
    Guid Id,
    CustomerType Type,
    string FullName,
    string? CompanyName,
    string? TaxNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    decimal Balance,
    decimal CreditLimit,
    CustomerStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
