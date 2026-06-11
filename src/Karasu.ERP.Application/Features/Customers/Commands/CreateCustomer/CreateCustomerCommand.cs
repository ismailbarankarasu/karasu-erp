using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    CustomerType Type,
    string FullName,
    string? CompanyName,
    string? TaxNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    decimal CreditLimit) : IRequest<Result<Guid>>;
