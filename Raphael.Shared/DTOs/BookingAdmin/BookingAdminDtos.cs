namespace Raphael.Shared.DTOs.BookingAdmin
{
    // What a clinic's admin manages in the Booking Portal's Admin tab (BOOKING_ADMIN.md). Every
    // request is scoped to the caller's own integrator, read from the token: none of these carries
    // an integrator id, so none can name another clinic.

    /// <summary>A role a clinic's admin can give: Admin (1) or Booking (6).</summary>
    public class BookingAdminRoleDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    /// <summary>One user of the clinic. Never the password hash.</summary>
    public class BookingAdminUserDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        /// <summary>The caller: the screen does not offer to disable or demote oneself.</summary>
        public bool IsCurrentUser { get; set; }
    }

    /// <summary>A new user of the clinic. It belongs to the caller's integrator and is born active.</summary>
    public class BookingAdminUserCreateDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        /// <summary>Final, not temporary: the admin sets it and tells the user (decided 2026-10-08).</summary>
        public string Password { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int RoleId { get; set; }
    }

    public class BookingAdminUserEditDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public int RoleId { get; set; }
    }

    /// <summary>A password the admin sets for another user of the clinic. Ends that user's sessions.</summary>
    public class BookingAdminPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class BookingAdminUserActiveDto
    {
        public bool IsActive { get; set; }
    }

    /// <summary>The clinic's own record. The API key is not here: it is asked for apart, on demand.</summary>
    public class BookingAdminOrganizationDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? ContactName { get; set; }

        public int? FundingSourceId { get; set; }

        public string? FundingSourceName { get; set; }
    }

    /// <summary>The contact details a clinic corrects. Name, state and funding source stay with the office.</summary>
    public class BookingAdminOrganizationEditDto
    {
        public string? Phone { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? ContactName { get; set; }
    }

    public class BookingAdminApiKeyDto
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    /// <summary>The clinic's funding source, and whether it may change it.</summary>
    public class BookingAdminFundingSourceDto : BookingAdminFundingSourceEditDto
    {
        public int Id { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Other integrators use it too: the office manages it, the clinic only reads it.</summary>
        public bool IsShared { get; set; }

        public bool CanEdit { get; set; }
    }

    /// <summary>
    /// What a clinic sets on its funding source. Not its state nor the Vectorcare facility id, which
    /// stay with the office.
    /// </summary>
    public class BookingAdminFundingSourceEditDto
    {
        public string Name { get; set; } = string.Empty;

        public string? AccountNumber { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? FAX { get; set; }

        public string? Email { get; set; }

        public string? ContactFirst { get; set; }

        public string? ContactLast { get; set; }

        public bool? SignaturePickup { get; set; }

        public bool? SignatureDropoff { get; set; }

        public bool? DriverSignaturePickup { get; set; }

        public bool? DriverSignatureDropoff { get; set; }

        public bool? RequireOdometer { get; set; }

        public bool? BarcodeScanRequired { get; set; }
    }

    /// <summary>One of the clinic's own billing items.</summary>
    public class BookingAdminBillingItemDto : BookingAdminBillingItemEditDto
    {
        public int Id { get; set; }

        public string UnitAbbreviation { get; set; } = string.Empty;

        /// <summary>Rated on a funding source at least once: it can no longer be deleted.</summary>
        public bool IsAssigned { get; set; }
    }

    public class BookingAdminBillingItemEditDto
    {
        public string Description { get; set; } = string.Empty;

        public int UnitId { get; set; }

        public bool IsCopay { get; set; }

        public string? ARAccount { get; set; }

        public string? ARSubAccount { get; set; }

        public string? ARCompany { get; set; }

        public string? APAccount { get; set; }

        public string? APSubAccount { get; set; }

        public string? APCompany { get; set; }
    }

    /// <summary>
    /// A rate on the clinic's funding source. The office's rates are listed too, read only, so the
    /// clinic sees everything it is billed with.
    /// </summary>
    public class BookingAdminRateDto : BookingAdminRateEditDto
    {
        public int Id { get; set; }

        public string BillingItemDescription { get; set; } = string.Empty;

        public string? BillingItemUnitAbbreviation { get; set; }

        public string SpaceTypeName { get; set; } = string.Empty;

        /// <summary>The billing item is the office's: shown, never editable here.</summary>
        public bool IsOffice { get; set; }

        public bool CanEdit { get; set; }
    }

    /// <summary>
    /// A rate the clinic sets. Rates are not deleted: one that stops applying is closed with its
    /// <see cref="ToDate"/>, so what was billed before can still be explained.
    /// </summary>
    public class BookingAdminRateEditDto
    {
        public int BillingItemId { get; set; }

        public int SpaceTypeId { get; set; }

        public decimal Rate { get; set; }

        public string? Per { get; set; }

        public bool IsDefault { get; set; }

        public string? ProcedureCode { get; set; }

        public decimal? MinCharge { get; set; }

        public decimal? MaxCharge { get; set; }

        public int? GreaterThanMinQty { get; set; }

        public int? LessOrEqualMaxQty { get; set; }

        public int? FreeQty { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }
    }
}
