using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.TripsService.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.TripsService.Services
{
    public class CustomerService
    {
        private readonly MediTransContext _context;

        public CustomerService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer> CreateAsync(CustomerDto dto)
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
                ClientCode = dto.ClientCode,
                PolicyNumber = dto.PolicyNumber,
                FundingSourceId = dto.FundingSourceId,
                SpaceTypeId = dto.SpaceTypeId,
                Email = dto.Email,
                DOB = dto.DOB,
                Gender = dto.Gender,
                Created = dto.Created,
                CreatedBy = dto.CreatedBy
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<Customer?> UpdateAsync(int id, CustomerDto dto)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return null;

            customer.FullName = dto.FullName;
            customer.Address = dto.Address;
            customer.City = dto.City;
            customer.State = dto.State;
            customer.Zip = dto.Zip;
            customer.Phone = dto.Phone;
            customer.MobilePhone = dto.MobilePhone;
            customer.ClientCode = dto.ClientCode;
            customer.PolicyNumber = dto.PolicyNumber;
            customer.FundingSourceId = dto.FundingSourceId;
            customer.SpaceTypeId = dto.SpaceTypeId;
            customer.Email = dto.Email;
            customer.DOB = dto.DOB;
            customer.Gender = dto.Gender;
            customer.Created = dto.Created;
            customer.CreatedBy = dto.CreatedBy;

            await _context.SaveChangesAsync();
            return customer;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
