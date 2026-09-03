using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Raphael.Shared.Entities
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Username { get; set; }
        /// <summary>
        /// ⚠️ Never leaves the server.
        /// </summary>
        /// <remarks>
        /// This entity is returned whole by endpoints that were never meant to publish it —
        /// GET /api/Runs serialises the route's Driver, and the Driver is a User — so the hash
        /// of every driver's password was going out to any client that asked for the route
        /// list. It is only ever read by PasswordHasher.Verify and only ever written by the
        /// password endpoints, so cutting it from serialisation costs nothing and closes it
        /// everywhere at once rather than one endpoint at a time.
        /// </remarks>
        [Required]
        [JsonIgnore]
        public string PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? DriverLicense { get; set; }
        public bool IsActive { get; set; }
        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; }
        //public ICollection<VehicleRoute> VehicleRoutes { get; set; }

        public int? IntegratorId { get; set; }
        [ForeignKey("IntegratorId")]
        public virtual Integrator? Integrator { get; set; }

        public int? ProviderId { get; set; }
        [ForeignKey("ProviderId")]
        public virtual Provider? Provider { get; set; }

        // Token for native push notifications (FCM/APNs).
        // Cut from serialisation for the same reason as the password hash: it rode out inside
        // the route list, and a push token is a credential for sending to somebody's phone.
        // The endpoints that report on it build their own response objects and are unaffected.
        [JsonIgnore]
        public string? PushToken { get; set; }
    }
}

