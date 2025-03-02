

using Authentication.Domain.Entities;

/// <summary>
/// Interface for OAuth token repository
/// </summary>
public interface IOAuthTokenRepository
{
    Task AddAsync(OauthToken token);
    Task UpdateAsync(OauthToken token);
    Task DeleteAsync(long tokenId);
    Task<OauthToken> GetByAccessTokenAsync(string accessToken);
    Task<OauthToken> GetByRefreshTokenAsync(string refreshToken);
    Task RevokeAllTokensForUserAsync(string username);
    Task<bool> TokenExistsAsync(string clientId, string username);
    Task CleanupExpiredTokensAsync(DateTime expirationThreshold);
}