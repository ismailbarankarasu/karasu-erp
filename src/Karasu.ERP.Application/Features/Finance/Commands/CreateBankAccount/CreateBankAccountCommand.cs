using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankAccount;

public record CreateBankAccountCommand(
    string BankName,
    string AccountName,
    string? Iban = null,
    decimal? OpeningBalance = null) : IRequest<Result<Guid>>;
