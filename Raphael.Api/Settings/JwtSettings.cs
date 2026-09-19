namespace Raphael.Api.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        /// <summary>
        /// ⚠️ Superseded on 2026-09-19 and read by nothing. Access token lifetimes now come
        /// from <see cref="SessionPolicyOptions"/>, per client application. The value is left
        /// in configuration because it documents what "Default" inherited, and removing it
        /// would only make the 600 there look arbitrary. Delete both together.
        /// </summary>
        public int ExpiresInMinutes { get; set; }
    }
}

