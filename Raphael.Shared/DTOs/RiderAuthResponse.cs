namespace Raphael.Shared.DTOs
{
    public class RiderAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public CustomerResponseDto Customer { get; set; }
        public bool IsSuccess { get; set; }
    }
}
