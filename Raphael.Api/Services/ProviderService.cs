using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using System.Threading.Tasks;

namespace Raphael.Api.Services
{
    public class ProviderService : IProviderService
    {
        private readonly RaphaelContext _context;
        private const int ContactProviderId = 1;
        private readonly IWebHostEnvironment _environment;
        private readonly ICurrentUserService _currentUser;

        public ProviderService(
            RaphaelContext context,
            IWebHostEnvironment environment,
            ICurrentUserService currentUser)
        {
            _context = context;
            _environment = environment;
            _currentUser = currentUser;
        }

        public async Task<ProviderDto?> GetContactProviderAsync()
        {
            var provider = await _context.Providers.FindAsync(ContactProviderId);
          
            if (provider == null)
            {
                provider = new Provider { Id = ContactProviderId, Name = "Default Provider Name" };
                _context.Providers.Add(provider);
                await _context.SaveChangesAsync();
            }
          
            return new ProviderDto
            {
                Name = provider.Name,
                Address = provider.Address,
                Email = provider.Email,
                Phone = provider.Phone,
                Logo = provider.Logo,
                Latitude = provider.Latitude,
                Longitude = provider.Longitude,
                TimeZoneId = provider.TimeZoneId,
                Website = provider.Website,
                ContactName = provider.ContactName,
                Comments = provider.Comments
            };
        }

        public async Task<bool> UpdateContactProviderAsync(ProviderDto providerDto)
        {
            var provider = await _context.Providers.FindAsync(ContactProviderId);

            if (provider == null)
            {
                return false; 
            }
           
            provider.Name = providerDto.Name;
            provider.Address = providerDto.Address;
            provider.Email = providerDto.Email;
            provider.Phone = providerDto.Phone;
            provider.Logo = providerDto.Logo;
            provider.Latitude = providerDto.Latitude;
            provider.Longitude = providerDto.Longitude;

            // ⚠️ Only when the caller actually sent them, and this is not a nicety.
            //
            // Raphael.Driver has its own copy of ProviderDto (Raphael.Driver/DTOs/ProviderDto.cs)
            // that predates these three fields, and it PUTs this endpoint from the contact card
            // on the driver's screen. Writing them through unconditionally means a driver
            // saving the office phone number silently erases the website, the contact name and
            // the notes somebody typed in the back office — on an app installed on 31 phones
            // that nobody can update today.
            //
            // Expand / contract, GIT_WORKFLOW.md section 4: a backend change may never assume
            // the client already knows about the field. Clearing one of these is an edit made
            // on the screen that owns them.
            if (providerDto.Website is not null) provider.Website = providerDto.Website;
            if (providerDto.ContactName is not null) provider.ContactName = providerDto.ContactName;
            if (providerDto.Comments is not null) provider.Comments = providerDto.Comments;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ProviderDto>> GetAllAsync()
        {
            return await _context.Providers
                .Select(p => new ProviderDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    Email = p.Email,
                    Phone = p.Phone,
                    Logo = p.Logo,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    TimeZoneId = p.TimeZoneId,
                    Website = p.Website,
                    ContactName = p.ContactName,
                    Comments = p.Comments,
                    CatalogProviderId = p.CatalogProviderId,
                    OwnerProviderId = p.OwnerProviderId
                }).ToListAsync();
        }

        public async Task<ProviderDto> CreateAsync(ProviderDto dto)
        {
            string fileName = string.Empty;
            if (dto.LogoFile != null)
            {
                fileName = await SavePhysicalFile(dto.LogoFile);
            }

            var provider = new Provider
            {
                Name = dto.Name,
                Address = dto.Address,
                Email = dto.Email,
                Phone = dto.Phone,
                Logo = fileName, // Guardamos solo "guid.jpg"
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TimeZoneId = dto.TimeZoneId,
                Website = dto.Website,
                ContactName = dto.ContactName,
                Comments = dto.Comments,
                CatalogProviderId = dto.CatalogProviderId,

                // From the token, never from the request. See IntegratorService.
                OwnerProviderId = _currentUser.ProviderId
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();
            dto.Id = provider.Id;
            dto.Logo = fileName;
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, ProviderDto dto)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null) return false;

            provider.Name = dto.Name;
            provider.Address = dto.Address;
            provider.Email = dto.Email;
            provider.Phone = dto.Phone;
            provider.Latitude = dto.Latitude;
            provider.Longitude = dto.Longitude;
            provider.TimeZoneId = dto.TimeZoneId;
            provider.Website = dto.Website;
            provider.ContactName = dto.ContactName;
            provider.Comments = dto.Comments;

            // Set once, when the row is created out of the catalog. See IntegratorService.
            if (provider.CatalogProviderId is null)
            {
                provider.CatalogProviderId = dto.CatalogProviderId;
            }

            if (dto.LogoFile != null)
            {
                // Try to delete the previous logo
                if (!string.IsNullOrEmpty(provider.Logo))
                {
                    DeletePhysicalFile(provider.Logo);
                }

                // Save the new logo
                provider.Logo = await SavePhysicalFile(dto.LogoFile);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> SavePhysicalFile(IFormFile file)
        {          
            string folder = Path.Combine(_environment.WebRootPath, "logos");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName; // We return only the file name
        }

        private void DeletePhysicalFile(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_environment.WebRootPath, "logos", fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
                // If there are no permissions to delete, we ignore the error so the flow continues
                // It is common in shared hosting to have deletion restrictions
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null) return false;

            if (!string.IsNullOrEmpty(provider.Logo))
            {
                DeletePhysicalFile(provider.Logo);
            }

            _context.Providers.Remove(provider);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> SaveLogoAsync(IFormFile file)
        {
            var folderName = Path.Combine("wwwroot", "logos");
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (!Directory.Exists(pathToSave)) Directory.CreateDirectory(pathToSave);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(pathToSave, fileName);
            var dbPath = "logos/" + fileName; // This is the relative URL

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return dbPath;
        }
    }
}