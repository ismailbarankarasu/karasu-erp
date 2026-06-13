using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetBankAccounts;

public record GetBankAccountsQuery() : IRequest<Result<List<BankAccountDto>>>;

public record BankAccountDto(
    Guid Id,
    string BankName,
    string AccountName,
    string? Iban,
    decimal CurrentBalance,
    bool IsActive);
