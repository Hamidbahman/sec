using System;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Domain.Entities;
using Data;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Repositories;
    public class OAuthTokenRepository : IOAuthTokenRepository
    {

        private readonly AutheDbContext _context;
        private readonly DbSet<OauthToken> _tokens;

    
        public OAuthTokenRepository(AutheDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tokens = context.Set<OauthToken>();
        }

        /// <summary>
        /// Adds a new OAuth token to the repository
        /// </summary>
        public async Task AddAsync(OauthToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            await _tokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing OAuth token
        /// </summary>
        public async Task UpdateAsync(OauthToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            _tokens.Update(token);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes an OAuth token by its ID
        /// </summary>
        public async Task DeleteAsync(long tokenId)
        {
            var token = await _tokens.FindAsync(tokenId);
            if (token != null)
            {
                _tokens.Remove(token);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves an OAuth token by its access token
        /// </summary>
        public async Task<OauthToken> GetByAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                return null;

            return await _tokens
                .FirstOrDefaultAsync(t => t.AccessToken == accessToken);
        }

        /// <summary>
        /// Retrieves an OAuth token by its refresh token
        /// </summary>
        public async Task<OauthToken> GetByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            return await _tokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }

        /// <summary>
        /// Revokes all tokens for a specific user
        /// </summary>
        public async Task RevokeAllTokensForUserAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                return;

            var userTokens = await _tokens
                .Where(t => t.UserName == username)
                .ToListAsync();

            if (userTokens.Any())
            {
                _tokens.RemoveRange(userTokens);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Checks if a token exists for a given client and user
        /// </summary>
        public async Task<bool> TokenExistsAsync(string clientId, string username)
        {
            return await _tokens
                .AnyAsync(t => t.ClientId == clientId && t.UserName == username);
        }

        /// <summary>
        /// Cleans up expired tokens
        /// </summary>
        public async Task CleanupExpiredTokensAsync(DateTime expirationThreshold)
        {
            var expiredTokens = await _tokens
                .Where(t => t.CreateDate < expirationThreshold)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _tokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
    
        }   
    }