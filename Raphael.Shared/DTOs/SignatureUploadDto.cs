namespace Raphael.Shared.DTOs
{
    public class SignatureUploadDto
    {
        // The signature will be sent as a Base64 string, which is the standard for JSON/HTTP
        public string SignatureBase64 { get; set; }
    }
}
