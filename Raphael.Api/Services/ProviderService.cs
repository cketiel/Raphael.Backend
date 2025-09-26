using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using System.Threading.Tasks;

namespace Raphael.Api.Services
{
    public class ProviderService : IProviderService
    {
        private readonly RaphaelContext _context;
        private const int ContactProviderId = 1;

        public ProviderService(RaphaelContext context)
        {
            _context = context;
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
                Longitude = provider.Longitude
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

            await _context.SaveChangesAsync();
            return true;
        }
    }
}