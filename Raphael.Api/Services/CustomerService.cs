using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public class CustomerService
    {
        private readonly RaphaelContext _context;

        public CustomerService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync()
        {
            return await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .Select(c => MapToResponseDto(c))
                .ToListAsync();
        }

        public async Task<CustomerResponseDto?> GetByIdAsync(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .FirstOrDefaultAsync(c => c.Id == id);

            return customer != null ? MapToResponseDto(customer) : null;
        }

        public async Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto)
        {
            var customer = new Customer
            {
                FullName = dto.FullName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Zip = dto.Zip,
                Phone = dto.Phone,
                MobilePhone = dto.MobilePhone,
                FundingSourceId = dto.FundingSourceId,
                SpaceTypeId = dto.SpaceTypeId,
                Email = dto.Email,
                Gender = dto.Gender,
                Created = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy,
                RiderId = dto.RiderId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return MapToResponseDto(customer);
        }

        public async Task<CustomerResponseDto?> UpdateAsync(int id, CustomerCreateDto dto)
        {
            var customer = await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return null;

            customer.FullName = dto.FullName;
            customer.Address = dto.Address;
            customer.City = dto.City;
            customer.State = dto.State;
            customer.Zip = dto.Zip;
            customer.Phone = dto.Phone;
            customer.MobilePhone = dto.MobilePhone;
            customer.FundingSourceId = dto.FundingSourceId;
            customer.SpaceTypeId = dto.SpaceTypeId;
            customer.Email = dto.Email;
            customer.Gender = dto.Gender;
            customer.Latitude = dto.Latitude;
            customer.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();
            return MapToResponseDto(customer);
        }

        private static CustomerResponseDto MapToResponseDto(Customer customer)
        {
            return new CustomerResponseDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Zip = customer.Zip,
                Phone = customer.Phone,
                MobilePhone = customer.MobilePhone,
                Email = customer.Email,
                FundingSourceId = customer.FundingSourceId,
                FundingSourceName = customer.FundingSource?.Name,
                SpaceTypeId = customer.SpaceTypeId,
                SpaceTypeName = customer.SpaceType?.Name,
                Gender = customer.Gender,
                Created = customer.Created,
                CreatedBy = customer.CreatedBy,
                RiderId = customer.RiderId,
                Latitude = customer.Latitude,
                Longitude = customer.Longitude
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CustomerResponseDto?> GetByRiderIdAsync(string riderId)
        {
            var customer = await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .FirstOrDefaultAsync(c => c.RiderId == riderId);

            return customer != null ? MapToResponseDto(customer) : null;
        }
    }
}

