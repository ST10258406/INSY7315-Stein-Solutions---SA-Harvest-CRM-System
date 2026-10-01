using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Utilities;
using CRM.Application.Interfaces;
using CRM.Application.Modules.Auth;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Auth.Commands;

public class RefreshTokenCommandHandlerTests
{
    private const string RawToken = "presented-refresh-token";

    private readonly IRefreshTokenRepository _refreshTokensMock = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IJwtTokenService _jwtTokenServiceMock = Substitute.For<IJwtTokenService>();
    private readonly RefreshTokenCommandHandler _handler;
    private readonly User _user;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshTokensMock, _unitOfWorkMock, _jwtTokenServiceMock,
            Options.Create(new RefreshTokenOptions { ReuseGraceSeconds = 20 }),
            NullLogger<RefreshTokenCommandHandler>.Instance);

        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "refresh-test@example.com",
            FirstName = "Refresh",
            LastName = "User",
            UserRoles = new List<UserRole> { new() { Role = new Role { Name = "Admin" } } }
        };

        _jwtTokenServiceMock.GenerateAccessToken(_user, Arg.Any<Guid>()).Returns("new-access-token");
        _jwtTokenServiceMock.GenerateRefreshToken().Returns("rotated-refresh-token");
        _jwtTokenServiceMock.AccessTokenExpirySeconds.Returns(3600);
        _refreshTokensMock.TryClaimRotationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        _refreshTokensMock.CountOtherActiveInFamilyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(1);
    }

    private RefreshToken StoreToken(Action<RefreshToken>? configure = null)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            User = _user,
            TokenHash = SecureTokens.Hash(RawToken),
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(5),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        configure?.Invoke(token);
        _refreshTokensMock.GetByHashWithUserAndRolesAsync(token.TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        return token;
    }

    private Task<CRM.Application.Modules.Auth.Dtos.RefreshTokenResponseDto> Refresh()
        => _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAccessTokenAndUser()
    {
        StoreToken();

        var result = await Refresh();

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Equal(_user.Id, result.User.Id);
        Assert.Equal(["Admin"], result.User.Roles);
    }

    [Fact]
    public async Task Handle_LooksUpByHash_NeverByRawValue()
    {
        StoreToken();

        await Refresh();

        await _refreshTokensMock.Received(1).GetByHashWithUserAndRolesAsync(SecureTokens.Hash(RawToken), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().GetByHashWithUserAndRolesAsync(RawToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesWithinFamilyWithoutExtendingExpiry()
    {
        var presented = StoreToken();

        var result = await Refresh();

        // The presented token is retired atomically and points at its replacement...
        await _refreshTokensMock.Received(1).TryClaimRotationAsync(
            presented.Id, SecureTokens.Hash("rotated-refresh-token"), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        // ...and the replacement (raw value returned once, hash stored) stays in the family.
        Assert.Equal("rotated-refresh-token", result.RefreshToken);
        Assert.Equal(presented.ExpiresAt, result.RefreshTokenExpiresAt);
        await _refreshTokensMock.Received(1).AddAsync(
            Arg.Is<RefreshToken>(rt =>
                rt.TokenHash == SecureTokens.Hash("rotated-refresh-token") &&
                rt.FamilyId == presented.FamilyId &&
                rt.UserId == _user.Id &&
                rt.ExpiresAt == presented.ExpiresAt &&
                !rt.IsRevoked),
            Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedException()
    {
        StoreToken(t => t.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorizedException()
    {
        _user.IsActive = false;
        StoreToken();

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentToken_ThrowsUnauthorizedException()
    {
        _refreshTokensMock.GetByHashWithUserAndRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);
    }

    [Fact]
    public async Task Handle_RevokedByLogout_ThrowsAndRevokesFamily()
    {
        // Revoked without a replacement (logout / reset / admin action) — never in grace.
        var presented = StoreToken(t => t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-1)));

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);
        await _refreshTokensMock.Received(1).RevokeFamilyAsync(presented.FamilyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RotatedTokenReusedAfterGraceWindow_RevokesWholeFamily()
    {
        // F-04 reuse detection: an old, already-rotated token coming back means it was copied.
        var familyId = Guid.NewGuid();
        var presented = StoreToken(t =>
        {
            t.FamilyId = familyId;
            t.Revoke(DateTimeOffset.UtcNow.AddMinutes(-5), replacedByTokenHash: "newer-hash");
        });
        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);

        await _refreshTokensMock.Received(1).RevokeFamilyAsync(familyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RotatedTokenReusedWithinGraceWindow_IssuesSiblingWithoutRevokingFamily()
    {
        // Two tabs reloading at once both present the same cookie: the second must not log
        // the user out.
        var presented = StoreToken(t => t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-3), replacedByTokenHash: "first-replacement"));
        _refreshTokensMock.GetByHashAsync("first-replacement", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { TokenHash = "first-replacement", FamilyId = presented.FamilyId, IsRevoked = false });
        _refreshTokensMock.TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Refresh();

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("first-replacement", presented.ReplacedByTokenHash); // original link untouched
        await _refreshTokensMock.DidNotReceive().RevokeFamilyAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().TryClaimRotationAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.Received(1).AddAsync(
            Arg.Is<RefreshToken>(rt => rt.FamilyId == presented.FamilyId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithinGraceButFamilyAlreadyKilled_IsTreatedAsReuse()
    {
        // If the replacement is no longer live, the family was already revoked (e.g. by reuse
        // detection) — the grace window must not resurrect it.
        StoreToken(t => t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-3), replacedByTokenHash: "dead-replacement"));
        _refreshTokensMock.GetByHashAsync("dead-replacement", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { TokenHash = "dead-replacement", IsRevoked = true });

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IssuesAccessTokenBoundToTheSessionFamily()
    {
        var presented = StoreToken();

        await Refresh();

        _jwtTokenServiceMock.Received(1).GenerateAccessToken(_user, presented.FamilyId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_NoCookie_ThrowsUnauthorized(string? rawToken)
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None));
        await _refreshTokensMock.DidNotReceive().GetByHashWithUserAndRolesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GraceReplayAlreadyUsed_RevokesFamilyInsteadOfMintingAgain()
    {
        // The grace replay is single-use: a further replay of the same old cookie is no
        // longer the two-tabs race, so it can't keep minting sibling tokens.
        var familyId = Guid.NewGuid();
        StoreToken(t =>
        {
            t.FamilyId = familyId;
            t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-3), replacedByTokenHash: "first-replacement");
            t.GraceReplayedAt = DateTimeOffset.UtcNow.AddSeconds(-2);
        });
        _refreshTokensMock.GetByHashAsync("first-replacement", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { TokenHash = "first-replacement", FamilyId = familyId, IsRevoked = false });

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);

        await _refreshTokensMock.Received(1).RevokeFamilyAsync(familyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LosingTheGraceClaimRace_IsRefusedWithoutRevokingFamily()
    {
        var presented = StoreToken(t => t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-3), replacedByTokenHash: "first-replacement"));
        _refreshTokensMock.GetByHashAsync("first-replacement", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { TokenHash = "first-replacement", FamilyId = presented.FamilyId, IsRevoked = false });
        _refreshTokensMock.TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);

        await _refreshTokensMock.DidNotReceive().RevokeFamilyAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    // ---- first-rotation race (two requests present the same still-active cookie) ----

    [Fact]
    public async Task Handle_LosesRotationRace_FallsBackToTheSingleGraceReplay()
    {
        var presented = StoreToken();
        _refreshTokensMock.TryClaimRotationAsync(presented.Id, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);
        _refreshTokensMock.TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Refresh();

        // The other request owns the rotation; this one consumed the one grace replay.
        Assert.Equal("new-access-token", result.AccessToken);
        await _refreshTokensMock.Received(1).TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.Received(1).AddAsync(Arg.Is<RefreshToken>(rt => rt.FamilyId == presented.FamilyId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LosesRotationRaceAndGraceAlreadyTaken_IsRefusedWithoutMinting()
    {
        // A third simultaneous request: rotation and grace replay are both gone, so at most
        // two tokens ever descend from one cookie.
        var presented = StoreToken();
        _refreshTokensMock.TryClaimRotationAsync(presented.Id, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);
        _refreshTokensMock.TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);

        await _refreshTokensMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GraceSiblingIntoAFamilyKilledMeanwhile_IsRevokedAndRefused()
    {
        // Reuse detection revoked the family between our eligibility check and our insert:
        // the sibling must not resurrect the session.
        var presented = StoreToken(t => t.Revoke(DateTimeOffset.UtcNow.AddSeconds(-3), replacedByTokenHash: "first-replacement"));
        _refreshTokensMock.GetByHashAsync("first-replacement", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { TokenHash = "first-replacement", FamilyId = presented.FamilyId, IsRevoked = false });
        _refreshTokensMock.TryClaimGraceReplayAsync(presented.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        _refreshTokensMock.CountOtherActiveInFamilyAsync(presented.FamilyId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);

        await Assert.ThrowsAsync<UnauthorizedException>(Refresh);

        await _refreshTokensMock.Received(1).RevokeFamilyAsync(presented.FamilyId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalRotation_DoesNotRunTheGraceFamilyCheck()
    {
        var presented = StoreToken();

        await Refresh();

        await _refreshTokensMock.DidNotReceive().CountOtherActiveInFamilyAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _refreshTokensMock.DidNotReceive().TryClaimGraceReplayAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
