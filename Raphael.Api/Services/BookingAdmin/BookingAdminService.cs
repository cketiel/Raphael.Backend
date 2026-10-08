using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.BookingAdmin;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services.BookingAdmin
{
    /// <summary>The caller is not a clinic's admin (403), or the thing asked for is not its clinic's (404).</summary>
    public sealed class BookingAdminDeniedException : Exception
    {
        public BookingAdminDeniedException(bool notFound) => NotFound = notFound;

        public bool NotFound { get; }
    }

    /// <summary>A rule of the Admin tab refused the request (400), with a message the clinic can read.</summary>
    public sealed class BookingAdminRuleException : Exception
    {
        public BookingAdminRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// The Booking Portal's Admin tab: a clinic's users, its own record, its funding source, and the
    /// billing items and rates it keeps on it (BOOKING_ADMIN.md).
    /// </summary>
    public interface IBookingAdminService
    {
        Task<List<BookingAdminRoleDto>> GetRolesAsync(CancellationToken ct);
        Task<List<BookingAdminUserDto>> GetUsersAsync(CancellationToken ct);
        Task<BookingAdminUserDto> CreateUserAsync(BookingAdminUserCreateDto dto, CancellationToken ct);
        Task<BookingAdminUserDto> EditUserAsync(int id, BookingAdminUserEditDto dto, CancellationToken ct);
        Task SetPasswordAsync(int id, BookingAdminPasswordDto dto, CancellationToken ct);
        Task<BookingAdminUserDto> SetActiveAsync(int id, bool isActive, CancellationToken ct);

        Task<BookingAdminOrganizationDto> GetOrganizationAsync(CancellationToken ct);
        Task<BookingAdminOrganizationDto> EditOrganizationAsync(BookingAdminOrganizationEditDto dto, CancellationToken ct);
        Task<BookingAdminApiKeyDto> GetApiKeyAsync(CancellationToken ct);

        Task<BookingAdminFundingSourceDto?> GetFundingSourceAsync(CancellationToken ct);
        Task<BookingAdminFundingSourceDto> CreateFundingSourceAsync(BookingAdminFundingSourceEditDto dto, CancellationToken ct);
        Task<BookingAdminFundingSourceDto> EditFundingSourceAsync(BookingAdminFundingSourceEditDto dto, CancellationToken ct);

        Task<List<BookingAdminBillingItemDto>> GetBillingItemsAsync(CancellationToken ct);
        Task<BookingAdminBillingItemDto> CreateBillingItemAsync(BookingAdminBillingItemEditDto dto, CancellationToken ct);
        Task<BookingAdminBillingItemDto> EditBillingItemAsync(int id, BookingAdminBillingItemEditDto dto, CancellationToken ct);
        Task DeleteBillingItemAsync(int id, CancellationToken ct);

        Task<List<BookingAdminRateDto>> GetRatesAsync(CancellationToken ct);
        Task<BookingAdminRateDto> CreateRateAsync(BookingAdminRateEditDto dto, CancellationToken ct);
        Task<BookingAdminRateDto> EditRateAsync(int id, BookingAdminRateEditDto dto, CancellationToken ct);
    }

    public sealed class BookingAdminService : IBookingAdminService
    {
        /// <summary>The only roles a clinic gives: Admin and Booking (decided 2026-10-08).</summary>
        private static readonly int[] ClinicRoles = [AdminRoleId, 6];

        private const int AdminRoleId = 1;

        /// <summary>Same rules the portal checks for a user's own password (passwordRules.ts).</summary>
        private const int PasswordMinLength = 8;

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ICatalogAccountSync _catalogSync;

        public BookingAdminService(RaphaelContext context, ICurrentUserService currentUser, ICatalogAccountSync catalogSync)
        {
            _context = context;
            _currentUser = currentUser;
            _catalogSync = catalogSync;
        }

        /// <summary>The caller's clinic, once it is known to be a clinic's admin: role 1 with an integrator.</summary>
        private int ClinicId =>
            _currentUser.RoleId == AdminRoleId && _currentUser.IntegratorId is int id
                ? id
                : throw new BookingAdminDeniedException(notFound: false);

        // ------------------------------------------------------------------ users

        public async Task<List<BookingAdminRoleDto>> GetRolesAsync(CancellationToken ct)
        {
            _ = ClinicId;
            return await _context.Roles.AsNoTracking()
                .Where(r => ClinicRoles.Contains(r.Id))
                .OrderBy(r => r.Id)
                .Select(r => new BookingAdminRoleDto { Id = r.Id, Name = r.RoleName })
                .ToListAsync(ct);
        }

        public async Task<List<BookingAdminUserDto>> GetUsersAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            return await UsersOf(clinicId)
                .OrderBy(u => u.FullName)
                .Select(ToUserDto())
                .ToListAsync(ct);
        }

        public async Task<BookingAdminUserDto> CreateUserAsync(BookingAdminUserCreateDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var (fullName, username) = await CheckUserAsync(dto.FullName, dto.Username, dto.RoleId, exceptId: null, ct);
            CheckPassword(dto.Password);

            var user = new User
            {
                FullName = fullName,
                Username = username,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Email = Clean(dto.Email),
                PhoneNumber = Clean(dto.PhoneNumber),
                RoleId = dto.RoleId,
                IsActive = true,
                // From the token, never from the request.
                IntegratorId = clinicId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            return await UserAsync(clinicId, user.Id, ct);
        }

        public async Task<BookingAdminUserDto> EditUserAsync(int id, BookingAdminUserEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var user = await UsersOf(clinicId).FirstOrDefaultAsync(u => u.Id == id, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);

            var (fullName, username) = await CheckUserAsync(dto.FullName, dto.Username, dto.RoleId, exceptId: id, ct);

            if (user.RoleId == AdminRoleId && dto.RoleId != AdminRoleId)
            {
                if (id == _currentUser.UserId)
                    throw new BookingAdminRuleException("You cannot remove your own administrator role.");
                await KeepAnAdminAsync(clinicId, id, ct);
            }

            user.FullName = fullName;
            user.Username = username;
            user.Email = Clean(dto.Email);
            user.PhoneNumber = Clean(dto.PhoneNumber);
            // A new role reaches the user's token at its next renewal, when the claims are read again.
            user.RoleId = dto.RoleId;
            await _context.SaveChangesAsync(ct);

            return await UserAsync(clinicId, id, ct);
        }

        public async Task SetPasswordAsync(int id, BookingAdminPasswordDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var user = await UsersOf(clinicId).FirstOrDefaultAsync(u => u.Id == id, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);

            // One's own password is changed from the user menu, which asks for the current one.
            if (id == _currentUser.UserId)
                throw new BookingAdminRuleException("Change your own password from your user menu.");

            CheckPassword(dto.NewPassword);
            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            await RevokeSessionsAsync(id, "password-set-by-admin", ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<BookingAdminUserDto> SetActiveAsync(int id, bool isActive, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var user = await UsersOf(clinicId).FirstOrDefaultAsync(u => u.Id == id, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);

            if (!isActive && user.IsActive)
            {
                if (id == _currentUser.UserId)
                    throw new BookingAdminRuleException("You cannot disable your own user.");
                if (user.RoleId == AdminRoleId) await KeepAnAdminAsync(clinicId, id, ct);

                // A refresh would end them anyway once it saw the account off; this ends them now.
                await RevokeSessionsAsync(id, "disabled-by-admin", ct);
            }

            user.IsActive = isActive;
            await _context.SaveChangesAsync(ct);
            return await UserAsync(clinicId, id, ct);
        }

        private IQueryable<User> UsersOf(int clinicId) => _context.Users.Where(u => u.IntegratorId == clinicId);

        private System.Linq.Expressions.Expression<Func<User, BookingAdminUserDto>> ToUserDto()
        {
            var me = _currentUser.UserId;
            return u => new BookingAdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                RoleId = u.RoleId,
                RoleName = u.Role.RoleName,
                IsActive = u.IsActive,
                IsCurrentUser = u.Id == me
            };
        }

        private async Task<BookingAdminUserDto> UserAsync(int clinicId, int id, CancellationToken ct) =>
            await UsersOf(clinicId).AsNoTracking().Where(u => u.Id == id).Select(ToUserDto()).FirstAsync(ct);

        private async Task<(string FullName, string Username)> CheckUserAsync(string fullName, string username, int roleId, int? exceptId, CancellationToken ct)
        {
            var name = fullName?.Trim() ?? "";
            var login = username?.Trim() ?? "";
            if (name.Length == 0) throw new BookingAdminRuleException("The full name is required.");
            if (login.Length == 0) throw new BookingAdminRuleException("The username is required.");
            if (!ClinicRoles.Contains(roleId)) throw new BookingAdminRuleException("The role must be Admin or Booking.");

            // Usernames are unique across the whole system, not per clinic: sign-in knows no clinic.
            if (await _context.Users.AnyAsync(u => u.Username == login && u.Id != exceptId, ct))
                throw new BookingAdminRuleException("That username is already taken.");

            return (name, login);
        }

        /// <summary>A clinic must never be left without an active admin: nobody could manage it any more.</summary>
        private async Task KeepAnAdminAsync(int clinicId, int leavingId, CancellationToken ct)
        {
            if (!await UsersOf(clinicId).AnyAsync(u => u.Id != leavingId && u.IsActive && u.RoleId == AdminRoleId, ct))
                throw new BookingAdminRuleException("Your organization must keep at least one active administrator.");
        }

        private static void CheckPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < PasswordMinLength)
                throw new BookingAdminRuleException($"The password must have at least {PasswordMinLength} characters.");
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                throw new BookingAdminRuleException("The password must contain letters and digits.");
        }

        /// <summary>Every open session of the user, on every device. Saved with the caller's changes.</summary>
        private async Task RevokeSessionsAsync(int userId, string reason, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var open = await _context.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToListAsync(ct);
            foreach (var token in open)
            {
                token.RevokedAtUtc = now;
                token.RevokedReason = reason;
            }
        }

        // ------------------------------------------------------------------ organization

        public async Task<BookingAdminOrganizationDto> GetOrganizationAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            return await _context.Integrators.AsNoTracking()
                .Where(i => i.Id == clinicId)
                .Select(i => new BookingAdminOrganizationDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    IsActive = i.IsActive,
                    Phone = i.Phone,
                    Website = i.Website,
                    Email = i.Email,
                    Address = i.Address,
                    ContactName = i.ContactName,
                    FundingSourceId = i.FundingSourceId,
                    FundingSourceName = i.FundingSource != null ? i.FundingSource.Name : null
                })
                .FirstOrDefaultAsync(ct) ?? throw new BookingAdminDeniedException(notFound: true);
        }

        public async Task<BookingAdminOrganizationDto> EditOrganizationAsync(BookingAdminOrganizationEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var integrator = await _context.Integrators.FirstOrDefaultAsync(i => i.Id == clinicId, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);

            var address = Clean(dto.Address);
            var sameAddress = string.Equals(address, integrator.Address, StringComparison.OrdinalIgnoreCase);

            // Through the catalog, like any other account screen: every app sees the same details.
            // The name stays: renaming is the office's. A new address drops the old point, which the
            // sync resolves again.
            await _catalogSync.SaveIntegratorIdentityAsync(
                integrator,
                integrator.CatalogIntegratorId,
                new AccountIdentity(
                    integrator.Name,
                    address,
                    Clean(dto.Phone),
                    Clean(dto.Email),
                    Clean(dto.Website),
                    Clean(dto.ContactName),
                    sameAddress ? integrator.Latitude : null,
                    sameAddress ? integrator.Longitude : null),
                ct);

            await _context.SaveChangesAsync(ct);
            return await GetOrganizationAsync(ct);
        }

        public async Task<BookingAdminApiKeyDto> GetApiKeyAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            var key = await _context.Integrators.AsNoTracking()
                .Where(i => i.Id == clinicId)
                .Select(i => i.ApiKey)
                .FirstOrDefaultAsync(ct);
            return new BookingAdminApiKeyDto { ApiKey = key ?? "" };
        }

        // ------------------------------------------------------------------ funding source

        public async Task<BookingAdminFundingSourceDto?> GetFundingSourceAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            var fundingSourceId = await FundingSourceIdAsync(clinicId, ct);
            if (fundingSourceId is null) return null;

            var fs = await _context.FundingSources.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fundingSourceId, ct);
            if (fs is null) return null;

            var shared = await IsSharedAsync(clinicId, fs.Id, ct);
            return new BookingAdminFundingSourceDto
            {
                Id = fs.Id,
                IsActive = fs.IsActive,
                IsShared = shared,
                CanEdit = !shared,
                Name = fs.Name,
                AccountNumber = fs.AccountNumber,
                Address = fs.Address,
                Phone = fs.Phone,
                FAX = fs.FAX,
                Email = fs.Email,
                ContactFirst = fs.ContactFirst,
                ContactLast = fs.ContactLast,
                SignaturePickup = fs.SignaturePickup,
                SignatureDropoff = fs.SignatureDropoff,
                DriverSignaturePickup = fs.DriverSignaturePickup,
                DriverSignatureDropoff = fs.DriverSignatureDropoff,
                RequireOdometer = fs.RequireOdometer,
                BarcodeScanRequired = fs.BarcodeScanRequired
            };
        }

        public async Task<BookingAdminFundingSourceDto> CreateFundingSourceAsync(BookingAdminFundingSourceEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var integrator = await _context.Integrators.FirstOrDefaultAsync(i => i.Id == clinicId, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);
            if (integrator.FundingSourceId is not null)
                throw new BookingAdminRuleException("Your organization already has a funding source.");

            var fs = new FundingSource { IsActive = true };
            await ApplyAsync(fs, dto, ct);
            _context.FundingSources.Add(fs);
            await _context.SaveChangesAsync(ct);

            integrator.FundingSourceId = fs.Id;
            await _context.SaveChangesAsync(ct);
            return (await GetFundingSourceAsync(ct))!;
        }

        public async Task<BookingAdminFundingSourceDto> EditFundingSourceAsync(BookingAdminFundingSourceEditDto dto, CancellationToken ct)
        {
            var fs = await EditableFundingSourceAsync(ct);
            await ApplyAsync(fs, dto, ct);
            await _context.SaveChangesAsync(ct);
            return (await GetFundingSourceAsync(ct))!;
        }

        private async Task ApplyAsync(FundingSource fs, BookingAdminFundingSourceEditDto dto, CancellationToken ct)
        {
            var name = dto.Name?.Trim() ?? "";
            if (name.Length == 0) throw new BookingAdminRuleException("The funding source name is required.");
            if (await _context.FundingSources.AnyAsync(f => f.Name == name && f.Id != fs.Id, ct))
                throw new BookingAdminRuleException("Another funding source already has that name.");

            fs.Name = name;
            fs.AccountNumber = Clean(dto.AccountNumber);
            fs.Address = Clean(dto.Address);
            fs.Phone = Clean(dto.Phone);
            fs.FAX = Clean(dto.FAX);
            fs.Email = Clean(dto.Email);
            fs.ContactFirst = Clean(dto.ContactFirst);
            fs.ContactLast = Clean(dto.ContactLast);
            fs.SignaturePickup = dto.SignaturePickup;
            fs.SignatureDropoff = dto.SignatureDropoff;
            fs.DriverSignaturePickup = dto.DriverSignaturePickup;
            fs.DriverSignatureDropoff = dto.DriverSignatureDropoff;
            fs.RequireOdometer = dto.RequireOdometer;
            fs.BarcodeScanRequired = dto.BarcodeScanRequired;
        }

        private async Task<int?> FundingSourceIdAsync(int clinicId, CancellationToken ct) =>
            await _context.Integrators.AsNoTracking().Where(i => i.Id == clinicId).Select(i => i.FundingSourceId).FirstOrDefaultAsync(ct);

        private Task<bool> IsSharedAsync(int clinicId, int fundingSourceId, CancellationToken ct) =>
            _context.Integrators.AnyAsync(i => i.FundingSourceId == fundingSourceId && i.Id != clinicId, ct);

        /// <summary>The clinic's funding source, when it may change it: it has one and no other integrator uses it.</summary>
        private async Task<FundingSource> EditableFundingSourceAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            var id = await FundingSourceIdAsync(clinicId, ct)
                ?? throw new BookingAdminRuleException("Your organization has no funding source yet.");
            if (await IsSharedAsync(clinicId, id, ct))
                throw new BookingAdminRuleException("This funding source is shared with other organizations. Ask the office to change it.");
            return await _context.FundingSources.FirstAsync(f => f.Id == id, ct);
        }

        // ------------------------------------------------------------------ billing items

        public async Task<List<BookingAdminBillingItemDto>> GetBillingItemsAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            return await ItemsOf(clinicId).AsNoTracking()
                .OrderBy(b => b.Description)
                .Select(ToItemDto())
                .ToListAsync(ct);
        }

        public async Task<BookingAdminBillingItemDto> CreateBillingItemAsync(BookingAdminBillingItemEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var item = new BillingItem { OwnerIntegratorId = clinicId };
            await ApplyAsync(item, dto, clinicId, ct);
            _context.BillingItems.Add(item);
            await _context.SaveChangesAsync(ct);
            return await ItemAsync(clinicId, item.Id, ct);
        }

        public async Task<BookingAdminBillingItemDto> EditBillingItemAsync(int id, BookingAdminBillingItemEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var item = await ItemsOf(clinicId).FirstOrDefaultAsync(b => b.Id == id, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);
            await ApplyAsync(item, dto, clinicId, ct);
            await _context.SaveChangesAsync(ct);
            return await ItemAsync(clinicId, id, ct);
        }

        public async Task DeleteBillingItemAsync(int id, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var item = await ItemsOf(clinicId).FirstOrDefaultAsync(b => b.Id == id, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);
            if (await _context.FundingSourceBillingItems.AnyAsync(r => r.BillingItemId == id, ct))
                throw new BookingAdminRuleException("This billing item has rates and cannot be deleted. Close its rates instead.");

            _context.BillingItems.Remove(item);
            await _context.SaveChangesAsync(ct);
        }

        private IQueryable<BillingItem> ItemsOf(int clinicId) => _context.BillingItems.Where(b => b.OwnerIntegratorId == clinicId);

        private static System.Linq.Expressions.Expression<Func<BillingItem, BookingAdminBillingItemDto>> ToItemDto() =>
            b => new BookingAdminBillingItemDto
            {
                Id = b.Id,
                Description = b.Description,
                UnitId = b.UnitId,
                UnitAbbreviation = b.Unit.Abbreviation,
                IsCopay = b.IsCopay,
                ARAccount = b.ARAccount,
                ARSubAccount = b.ARSubAccount,
                ARCompany = b.ARCompany,
                APAccount = b.APAccount,
                APSubAccount = b.APSubAccount,
                APCompany = b.APCompany,
                IsAssigned = b.FundingSourceBillingItems.Any()
            };

        private async Task<BookingAdminBillingItemDto> ItemAsync(int clinicId, int id, CancellationToken ct) =>
            await ItemsOf(clinicId).AsNoTracking().Where(b => b.Id == id).Select(ToItemDto()).FirstAsync(ct);

        private async Task ApplyAsync(BillingItem item, BookingAdminBillingItemEditDto dto, int clinicId, CancellationToken ct)
        {
            var description = dto.Description?.Trim() ?? "";
            if (description.Length == 0) throw new BookingAdminRuleException("The description is required.");
            if (!await _context.Units.AnyAsync(u => u.Id == dto.UnitId, ct)) throw new BookingAdminRuleException("Choose a unit.");
            if (await ItemsOf(clinicId).AnyAsync(b => b.Description == description && b.Id != item.Id, ct))
                throw new BookingAdminRuleException("You already have a billing item with that description.");

            item.Description = description;
            item.UnitId = dto.UnitId;
            item.IsCopay = dto.IsCopay;
            item.ARAccount = Clean(dto.ARAccount);
            item.ARSubAccount = Clean(dto.ARSubAccount);
            item.ARCompany = Clean(dto.ARCompany);
            item.APAccount = Clean(dto.APAccount);
            item.APSubAccount = Clean(dto.APSubAccount);
            item.APCompany = Clean(dto.APCompany);
        }

        // ------------------------------------------------------------------ rates

        public async Task<List<BookingAdminRateDto>> GetRatesAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            var fundingSourceId = await FundingSourceIdAsync(clinicId, ct);
            if (fundingSourceId is null) return [];

            var shared = await IsSharedAsync(clinicId, fundingSourceId.Value, ct);
            var rows = await _context.FundingSourceBillingItems.AsNoTracking()
                .Where(r => r.FundingSourceId == fundingSourceId)
                // The office's rates and the clinic's own; never another clinic's on a shared source.
                .Where(r => r.BillingItem.OwnerIntegratorId == null || r.BillingItem.OwnerIntegratorId == clinicId)
                .OrderBy(r => r.BillingItem.Description).ThenBy(r => r.SpaceType.Name).ThenByDescending(r => r.FromDate)
                .Select(ToRateDto())
                .ToListAsync(ct);

            foreach (var row in rows) row.CanEdit = !row.IsOffice && !shared;
            return rows;
        }

        public async Task<BookingAdminRateDto> CreateRateAsync(BookingAdminRateEditDto dto, CancellationToken ct)
        {
            var fs = await EditableFundingSourceAsync(ct);
            var rate = new FundingSourceBillingItem { FundingSourceId = fs.Id };
            await ApplyAsync(rate, dto, ct);
            _context.FundingSourceBillingItems.Add(rate);
            await _context.SaveChangesAsync(ct);
            return await RateAsync(rate.Id, ct);
        }

        public async Task<BookingAdminRateDto> EditRateAsync(int id, BookingAdminRateEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var fs = await EditableFundingSourceAsync(ct);
            var rate = await _context.FundingSourceBillingItems
                .FirstOrDefaultAsync(r => r.Id == id && r.FundingSourceId == fs.Id && r.BillingItem.OwnerIntegratorId == clinicId, ct)
                ?? throw new BookingAdminDeniedException(notFound: true);
            await ApplyAsync(rate, dto, ct);
            await _context.SaveChangesAsync(ct);
            return await RateAsync(id, ct);
        }

        private static System.Linq.Expressions.Expression<Func<FundingSourceBillingItem, BookingAdminRateDto>> ToRateDto() =>
            r => new BookingAdminRateDto
            {
                Id = r.Id,
                BillingItemId = r.BillingItemId,
                BillingItemDescription = r.BillingItem.Description,
                BillingItemUnitAbbreviation = r.BillingItem.Unit.Abbreviation,
                SpaceTypeId = r.SpaceTypeId,
                SpaceTypeName = r.SpaceType.Name,
                Rate = r.Rate,
                Per = r.Per,
                IsDefault = r.IsDefault,
                ProcedureCode = r.ProcedureCode,
                MinCharge = r.MinCharge,
                MaxCharge = r.MaxCharge,
                GreaterThanMinQty = r.GreaterThanMinQty,
                LessOrEqualMaxQty = r.LessOrEqualMaxQty,
                FreeQty = r.FreeQty,
                FromDate = r.FromDate,
                ToDate = r.ToDate,
                IsOffice = r.BillingItem.OwnerIntegratorId == null
            };

        private async Task<BookingAdminRateDto> RateAsync(int id, CancellationToken ct)
        {
            var row = await _context.FundingSourceBillingItems.AsNoTracking().Where(r => r.Id == id).Select(ToRateDto()).FirstAsync(ct);
            row.CanEdit = !row.IsOffice;
            return row;
        }

        private async Task ApplyAsync(FundingSourceBillingItem rate, BookingAdminRateEditDto dto, CancellationToken ct)
        {
            var clinicId = ClinicId;
            // Only the clinic's own items: an office item is rated by the office.
            if (!await ItemsOf(clinicId).AnyAsync(b => b.Id == dto.BillingItemId, ct))
                throw new BookingAdminRuleException("Choose one of your billing items.");
            if (!await _context.SpaceTypes.AnyAsync(s => s.Id == dto.SpaceTypeId, ct))
                throw new BookingAdminRuleException("Choose a space type.");
            if (dto.Rate < 0) throw new BookingAdminRuleException("The rate cannot be negative.");
            if (dto.MinCharge is < 0 || dto.MaxCharge is < 0) throw new BookingAdminRuleException("Charges cannot be negative.");
            if (dto.MinCharge is decimal min && dto.MaxCharge is decimal max && min > max)
                throw new BookingAdminRuleException("The minimum charge cannot be greater than the maximum.");
            if (dto.ToDate.Date < dto.FromDate.Date) throw new BookingAdminRuleException("The end date cannot be earlier than the start date.");

            rate.BillingItemId = dto.BillingItemId;
            rate.SpaceTypeId = dto.SpaceTypeId;
            rate.Rate = dto.Rate;
            rate.Per = Clean(dto.Per);
            rate.IsDefault = dto.IsDefault;
            rate.ProcedureCode = Clean(dto.ProcedureCode);
            rate.MinCharge = dto.MinCharge;
            rate.MaxCharge = dto.MaxCharge;
            rate.GreaterThanMinQty = dto.GreaterThanMinQty;
            rate.LessOrEqualMaxQty = dto.LessOrEqualMaxQty;
            rate.FreeQty = dto.FreeQty;
            // Calendar days, as the Desktop stores them: no time of day.
            rate.FromDate = dto.FromDate.Date;
            rate.ToDate = dto.ToDate.Date;
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
