namespace Raphael.Shared.DTOs
{
    public class ExpoPushResult
    {
        public bool Success { get; set; }
        public string RawResponse { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
