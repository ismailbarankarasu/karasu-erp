using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<AuthUserResponse>>;
