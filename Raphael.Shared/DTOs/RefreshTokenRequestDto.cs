using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// The body of <c>POST /api/Auth/refresh</c>.
    /// </summary>
    /// <remarks>
    /// In the body and never in the query string. A refresh token is a credential with a
    /// lifetime measured in weeks, and a query string is written to server logs, proxy logs
    /// and browser history.
    /// </remarks>
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
