using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string CompanyName,
    string Slug,
    string Email,
    string Password,
    string FullName) : IRequest<Result<AuthResponse>>;
