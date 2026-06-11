using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;
