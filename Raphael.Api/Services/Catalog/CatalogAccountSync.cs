using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>The name, address and contact details of an entity, as an account screen edits them.</summary>
    public sealed record AccountIdentity(
        string Name,
        string? Address,
        string? Phone,
        string? Email,
        string? Website,
        string? ContactName,
        double? Latitude,
        double? Longitude);

    /// <summary>
    /// Another catalog entry already has this group, name and zip: saving would make two entries of one entity.
    /// </summary>
    public sealed class CatalogConflictException : Exception
    {
        public CatalogConflictException()
            : base("Another catalog entry already has this name and zip. If it is the same entity, edit that one.") { }
    }

    /// <summary>
    /// Keeps one version of an entity's identity: the catalog's (CATALOG_MODEL.md §4.1).
    /// </summary>
    /// <remarks>
    /// An account (<see cref="Provider"/>, <see cref="Integrator"/>) linked to its catalog row
    /// writes its name, address and contact details there, and every account linked to that row
    /// gets the result. A change made in the catalog reaches the accounts the same way. The account
    /// columns are kept equal to the catalog rather than dropped: reports, routes and the Desktop
    /// read them directly, and none of them has to change.
    ///
    /// <para>
    /// Not synchronised, on purpose: comments (the catalog's speak about the entity, an account's
    /// are its company's own notes) and <c>IsActive</c> (an entity can exist while an account is
    /// switched off, and the other way round).
    /// </para>
    /// </remarks>
    public interface ICatalogAccountSync
    {
        /// <summary>An account was saved: its identity goes to its catalog row, and from there to every account linked to it.</summary>
        /// <exception cref="CatalogConflictException">The new name and zip belong to another catalog entry.</exception>
        Task SaveProviderIdentityAsync(Provider account, AccountIdentity identity, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="SaveProviderIdentityAsync"/>
        Task SaveIntegratorIdentityAsync(Integrator account, AccountIdentity identity, CancellationToken cancellationToken = default);

        /// <summary>A catalog row changed: every account linked to it takes its identity. Does not save.</summary>
        Task SpreadAsync(CatalogProvider row, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="SpreadAsync(CatalogProvider, CancellationToken)"/>
        Task SpreadAsync(CatalogIntegrator row, CancellationToken cancellationToken = default);
    }

    public sealed class CatalogAccountSync : ICatalogAccountSync
    {
        private readonly RaphaelContext _context;
        private readonly IRoutingService _routing;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CatalogAccountSync> _logger;

        public CatalogAccountSync(
            RaphaelContext context,
            IRoutingService routing,
            ICurrentUserService currentUser,
            ILogger<CatalogAccountSync> logger)
        {
            _context = context;
            _routing = routing;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task SaveProviderIdentityAsync(Provider account, AccountIdentity identity, CancellationToken cancellationToken = default)
        {
            if (account.CatalogProviderId is null)
            {
                CopyTo(account, identity);
                return;
            }

            var row = await _context.CatalogProviders.FirstAsync(c => c.Id == account.CatalogProviderId, cancellationToken);
            var place = new Place(row.Address, row.City, row.State, row.Zip, row.Latitude, row.Longitude);

            row.Name = identity.Name.Trim();
            row.Phone = Clean(identity.Phone);
            row.Email = Clean(identity.Email);
            row.Website = Clean(identity.Website);
            row.ContactName = Clean(identity.ContactName);
            var moved = await ResolvePlaceAsync(place, identity, cancellationToken);
            (row.Address, row.City, row.State, row.Zip, row.Latitude, row.Longitude) = (moved.Street, moved.City, moved.State, moved.Zip, moved.Latitude, moved.Longitude);
            if (moved.Changed) row.GeocodeStatus = moved.Resolved ? GeocodeStatus.Resolved : GeocodeStatus.NotFound;

            row.PhoneDigits = CatalogText.DigitsOnly(row.Phone);
            row.MatchKey = CatalogText.BuildMatchKey(row.CategoryId, row.Name, row.Zip);
            row.SearchText = CatalogText.BuildSearchText(row.Name, row.City, row.CountyRaw, row.Zip, row.Address, row.ContactName);

            if (await _context.CatalogProviders.AnyAsync(c => c.Id != row.Id && c.MatchKey == row.MatchKey, cancellationToken))
            {
                throw new CatalogConflictException();
            }

            Stamp(row);
            await SpreadAsync(row, cancellationToken);
            CopyFrom(row, account);
        }

        public async Task SaveIntegratorIdentityAsync(Integrator account, AccountIdentity identity, CancellationToken cancellationToken = default)
        {
            if (account.CatalogIntegratorId is null)
            {
                CopyTo(account, identity);
                return;
            }

            var row = await _context.CatalogIntegrators.FirstAsync(c => c.Id == account.CatalogIntegratorId, cancellationToken);
            var place = new Place(row.Address, row.City, row.State, row.Zip, row.Latitude, row.Longitude);

            row.Name = identity.Name.Trim();
            row.Phone = Clean(identity.Phone);
            row.Email = Clean(identity.Email);
            row.Website = Clean(identity.Website);
            row.ContactName = Clean(identity.ContactName);
            var moved = await ResolvePlaceAsync(place, identity, cancellationToken);
            (row.Address, row.City, row.State, row.Zip, row.Latitude, row.Longitude) = (moved.Street, moved.City, moved.State, moved.Zip, moved.Latitude, moved.Longitude);
            if (moved.Changed) row.GeocodeStatus = moved.Resolved ? GeocodeStatus.Resolved : GeocodeStatus.NotFound;

            row.PhoneDigits = CatalogText.DigitsOnly(row.Phone);
            row.MatchKey = CatalogText.BuildMatchKey(row.CategoryId, row.Name, row.Zip);
            row.SearchText = CatalogText.BuildSearchText(row.Name, row.City, row.CountyRaw, row.Zip, row.Address, row.ContactName);

            if (await _context.CatalogIntegrators.AnyAsync(c => c.Id != row.Id && c.MatchKey == row.MatchKey, cancellationToken))
            {
                throw new CatalogConflictException();
            }

            Stamp(row);
            await SpreadAsync(row, cancellationToken);
            CopyFrom(row, account);
        }

        public async Task SpreadAsync(CatalogProvider row, CancellationToken cancellationToken = default)
        {
            var accounts = await _context.Providers.Where(p => p.CatalogProviderId == row.Id).ToListAsync(cancellationToken);
            foreach (var account in accounts) CopyFrom(row, account);
        }

        public async Task SpreadAsync(CatalogIntegrator row, CancellationToken cancellationToken = default)
        {
            var accounts = await _context.Integrators.Where(i => i.CatalogIntegratorId == row.Id).ToListAsync(cancellationToken);
            foreach (var account in accounts) CopyFrom(row, account);
        }

        // ---- Copies --------------------------------------------------------------------------

        private static void CopyFrom(CatalogProvider row, Provider account)
        {
            account.Name = row.Name;
            account.Address = RouteCacheKey.ComposeAddress(row.Address, row.City, row.State, row.Zip);
            account.Phone = row.Phone;
            account.Email = row.Email;
            account.Website = row.Website;
            account.ContactName = row.ContactName;
            account.Latitude = row.Latitude;
            account.Longitude = row.Longitude;
        }

        private static void CopyFrom(CatalogIntegrator row, Integrator account)
        {
            account.Name = row.Name;
            account.Address = RouteCacheKey.ComposeAddress(row.Address, row.City, row.State, row.Zip);
            account.Phone = row.Phone;
            account.Email = row.Email;
            account.Website = row.Website;
            account.ContactName = row.ContactName;
            account.Latitude = row.Latitude;
            account.Longitude = row.Longitude;
        }

        /// <summary>An account with no catalog row keeps its own copy, exactly as before.</summary>
        private static void CopyTo(Provider account, AccountIdentity identity)
        {
            account.Name = identity.Name;
            account.Address = identity.Address;
            account.Phone = identity.Phone;
            account.Email = identity.Email;
            account.Website = identity.Website;
            account.ContactName = identity.ContactName;
            account.Latitude = identity.Latitude;
            account.Longitude = identity.Longitude;
        }

        private static void CopyTo(Integrator account, AccountIdentity identity)
        {
            account.Name = identity.Name;
            account.Address = identity.Address;
            account.Phone = identity.Phone;
            account.Email = identity.Email;
            account.Website = identity.Website;
            account.ContactName = identity.ContactName;
            account.Latitude = identity.Latitude;
            account.Longitude = identity.Longitude;
        }

        // ---- The address ---------------------------------------------------------------------

        private sealed record Place(string? Street, string? City, string? State, string? Zip, double? Latitude, double? Longitude)
        {
            public bool Changed { get; init; }
            public bool Resolved { get; init; }
        }

        /// <summary>
        /// The catalog keeps the address in parts and an account screen sends one line. When the line
        /// changed, it is resolved through the backend's geocode cache (a request is paid only for an
        /// address nobody has resolved before) and split into street, city, state and zip.
        /// </summary>
        private async Task<Place> ResolvePlaceAsync(Place current, AccountIdentity identity, CancellationToken cancellationToken)
        {
            var line = Clean(identity.Address);
            var currentLine = RouteCacheKey.ComposeAddress(current.Street, current.City, current.State, current.Zip);

            if (line is null || string.Equals(line, currentLine, StringComparison.OrdinalIgnoreCase))
            {
                // Same address: only the pin may have been nudged on the screen.
                return current with
                {
                    Latitude = identity.Latitude ?? current.Latitude,
                    Longitude = identity.Longitude ?? current.Longitude
                };
            }

            // The screen's own pin is trusted when it sent one; otherwise the line is geocoded.
            double? lat = identity.Latitude, lng = identity.Longitude;
            if (lat is null || lng is null)
            {
                var found = await _routing.GeocodeAsync(new GeocodeRequestDto { Address = line }, cancellationToken);
                lat = found.Latitude;
                lng = found.Longitude;
            }

            if (lat is not null && lng is not null)
            {
                var parts = await _routing.ReverseGeocodeCityAsync(
                    new ReverseGeocodeRequestDto { Latitude = lat.Value, Longitude = lng.Value }, cancellationToken);

                if (parts.Status == RoutingContract.Statuses.Ok && !string.IsNullOrWhiteSpace(parts.Street))
                {
                    return new Place(parts.Street, parts.City, parts.State, parts.Zip, lat, lng) { Changed = true, Resolved = true };
                }
            }

            // Could not be split: the line goes whole into the street, and the parts that no longer
            // belong to it are cleared rather than left describing the old place.
            _logger.LogWarning("A catalog address could not be resolved into parts; it was stored as one line.");
            return new Place(line, null, null, null, lat, lng) { Changed = true, Resolved = false };
        }

        // ---- Helpers ---------------------------------------------------------------------------

        private void Stamp(CatalogProvider row)
        {
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.UpdatedByUserId = _currentUser.UserId;
            row.UpdatedByProviderId = _currentUser.ProviderId;
        }

        private void Stamp(CatalogIntegrator row)
        {
            row.UpdatedAtUtc = DateTime.UtcNow;
            row.UpdatedByUserId = _currentUser.UserId;
            row.UpdatedByProviderId = _currentUser.ProviderId;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
