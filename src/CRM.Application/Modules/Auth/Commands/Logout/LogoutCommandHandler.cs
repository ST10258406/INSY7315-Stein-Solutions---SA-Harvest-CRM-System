using CRM.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CRM.Application.Modules.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _context;

    public LogoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, ct);

        // If it doesn't exist or is already revoked, do nothing —
        // still return success either way. Don't throw NotFoundException
        // here, that would leak whether the token was ever valid.
        if (token is not null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync(ct);
        }
    }
}
