namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// An administrator sets a new password for someone else, without knowing the old one
    /// (<c>PUT api/Users/{id}/reset-password</c>).
    /// </summary>
    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
