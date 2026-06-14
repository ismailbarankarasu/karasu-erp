using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;

public record ForgotPasswordResponse(string Message, string? ResetToken);

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly IIdentityService _identityService;

    public ForgotPasswordCommandHandler(IIdentityService identityService) => _identityService = identityService;

    public async Task<Result<ForgotPasswordResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var (_, _, resetToken) = await _identityService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse(
            "Şifre sıfırlama talimatları e-posta adresinize gönderildi.",
            resetToken));
    }
}
