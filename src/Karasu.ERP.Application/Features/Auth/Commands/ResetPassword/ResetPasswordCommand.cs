using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var (success, error) = await _identityService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);

        return success ? Result.Success() : Result.Failure(error ?? "Şifre sıfırlama başarısız.", "RESET_FAILED");
    }
}
