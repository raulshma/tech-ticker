using TechTicker.Shared.Models;
using TechTicker.UserService.DTOs;

namespace TechTicker.UserService.Services
{
    /// <summary>
    /// Interface for token generation and management
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates an access token for the user
        /// </summary>
        Task<TokenResponse> GenerateAccessTokenAsync(User user);

        /// <summary>
        /// Validates an access token
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes a token
        /// </summary>
        Task<bool> RevokeTokenAsync(string token);
    }
}
