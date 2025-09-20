using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;

namespace Raphael.Api.Services
{
    public class RunService : IRunService
    {
        private readonly RaphaelContext _context;

        public RunService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VehicleRoute>> GetAllAsync()
        {
            // Eager loading of all related collections to avoid N+1 problems.
            // AsNoTracking() improves performance on read-only operations.
            return await _context.VehicleRoutes
                .Include(vr => vr.Vehicle).ThenInclude(v => v.VehicleGroup)
                .Include(vr => vr.Driver)
                .Include(vr => vr.Suspensions)
                .Include(vr => vr.Availabilities)
                .Include(vr => vr.FundingSources).ThenInclude(fs => fs.FundingSource)
                .AsNoTracking()
                .ToListAsync();
        }
        /*public async Task<IEnumerable<VehicleRoute>> GetAllAsync()
        {
            return await _context.VehicleRoutes
                .Include(vr => vr.Vehicle)
                .Include(vr => vr.Driver)
                .ToListAsync();
        }*/

        public async Task<VehicleRoute?> GetByIdAsync(int id)
        {
            // We do the same for a single record, but without AsNoTracking()
            // in case it is going to be modified later in the same transaction.
            return await _context.VehicleRoutes
                .Include(vr => vr.Vehicle).ThenInclude(v => v.VehicleGroup)
                .Include(vr => vr.Driver)
                .Include(vr => vr.Suspensions)
                .Include(vr => vr.Availabilities)
                .Include(vr => vr.FundingSources).ThenInclude(fs => fs.FundingSource)
                .FirstOrDefaultAsync(vr => vr.Id == id);
        }

        public async Task<VehicleRoute> CreateAsync(VehicleRouteDto dto)
        {
            // Mapping the DTO to the main entity
            var route = new VehicleRoute
            {
                Name = dto.Name,
                Description = dto.Description,
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Garage = dto.Garage,
                GarageLatitude = dto.GarageLatitude,
                GarageLongitude = dto.GarageLongitude,
                SmartphoneLogin = dto.SmartphoneLogin,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                // Initialization of collections to be able to add elements to them
                Suspensions = new List<RouteSuspension>(),
                Availabilities = new List<RouteAvailability>(),
                FundingSources = new List<RouteFundingSource>()
            };

            // Mapping optional collections
            if (dto.Suspensions != null)
            {
                foreach (var sDto in dto.Suspensions)
                {
                    route.Suspensions.Add(new RouteSuspension
                    {
                        SuspensionStart = sDto.SuspensionStart,
                        SuspensionEnd = sDto.SuspensionEnd,
                        Reason = sDto.Reason
                    });
                }
            }

            if (dto.Availabilities != null)
            {
                foreach (var aDto in dto.Availabilities)
                {
                    route.Availabilities.Add(new RouteAvailability
                    {
                        DayOfWeek = aDto.DayOfWeek,
                        StartTime = aDto.StartTime,
                        EndTime = aDto.EndTime,
                        IsActive = aDto.IsActive
                    });
                }
            }

            if (dto.FundingSources != null)
            {
                foreach (var fsDto in dto.FundingSources)
                {
                    route.FundingSources.Add(new RouteFundingSource
                    {
                        FundingSourceId = fsDto.FundingSourceId
                    });
                }
            }

            _context.VehicleRoutes.Add(route);
            await _context.SaveChangesAsync();
            //return route; // EF Core automatically updates the ID of the 'route' entity
            // The API creates the entity in the database and, by default,
            // returns the VehicleRoute entity as it has it in memory after saving.
            // At this point, the navigation properties(Driver and Vehicle) of that object
            // are null because Entity Framework does not automatically load them after an Add.
            return await GetByIdAsync(route.Id);
        }
        public async Task<bool> UpdateAsync(int id, VehicleRouteDto dto)
        {
            // It is CRUCIAL to include the child collections when searching for the entity to update.
            // If they are not included, EF will think they do not exist and delete them.
            var route = await _context.VehicleRoutes
            .Include(r => r.Suspensions)
                .Include(r => r.Availabilities)
                .Include(r => r.FundingSources)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return false;

            // Simple property mapping
            route.Name = dto.Name;
            route.Description = dto.Description;
            route.DriverId = dto.DriverId;
            route.VehicleId = dto.VehicleId;
            route.Garage = dto.Garage;
            route.GarageLatitude = dto.GarageLatitude;
            route.GarageLongitude = dto.GarageLongitude;
            route.SmartphoneLogin = dto.SmartphoneLogin;
            route.FromDate = dto.FromDate;
            route.ToDate = dto.ToDate;
            route.FromTime = dto.FromTime;
            route.ToTime = dto.ToTime;

            // Update logic for collections
            UpdateSuspensions(route, dto.Suspensions);
            UpdateAvailabilities(route, dto.Availabilities);
            UpdateFundingSources(route, dto.FundingSources);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var route = await _context.VehicleRoutes.FindAsync(id);
            if (route == null) return false;

            _context.VehicleRoutes.Remove(route);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Cancels a route by setting its end date (ToDate) to the current date and time.
        /// </summary>
        /// <param name="id">The ID of the route to cancel.</param>
        /// <returns>True if the route was found and canceled, otherwise False.</returns>
        public async Task<bool> CancelAsync(int id)
        {
            // 1. Search the entity by its ID. We use FindAsync which is efficient for primary key searches.
            var route = await _context.VehicleRoutes.FindAsync(id);

            // 2. Check if the route exists. If not, nothing can be done.
            if (route == null)
            {
                return false;
            }

            // 3. Apply business logic: set ToDate to the current date and time.
            // It is good practice to use UtcNow on the server to avoid time zone issues.
            route.ToDate = DateTime.UtcNow;

            // 4. Save changes to the database.
            await _context.SaveChangesAsync();

            // 5. Return 'true' to indicate that the operation was successful.
            return true;
        }

        public async Task<VehicleRoute?> GetActiveRunByDriverIdAsync(int driverId)
        {
            var today = DateTime.UtcNow.Date;

            // We look for the route that meets the conditions:
            // 1. Matches the DriverId.
            // 2. The start date is today or earlier.
            // 3. The end date (ToDate) does not exist (is null) Either it is today or in the future.
            // We include Driver and Vehicle because we will need them in the app.
            return await _context.VehicleRoutes
                .Include(vr => vr.Driver)
                .Include(vr => vr.Vehicle)
                .Where(vr => vr.DriverId == driverId &&
                             vr.FromDate.Date <= today &&
                             (vr.ToDate == null || vr.ToDate.Value.Date >= today))
                .FirstOrDefaultAsync();
        }

        #region Private methods to organize collection update logic
        private void UpdateSuspensions(VehicleRoute route, List<RouteSuspensionDto>? suspensionDtos)
        {
            // If the DTO is null, we eliminate all existing suspensions
            suspensionDtos ??= new List<RouteSuspensionDto>();

            // Delete those that no longer come in the DTO
            var suspensionsToRemove = route.Suspensions
                .Where(s => !suspensionDtos.Any(dto => dto.Id == s.Id && s.Id != 0))
                .ToList();
            _context.RouteSuspensions.RemoveRange(suspensionsToRemove);

            // Update existing and add new
            foreach (var dto in suspensionDtos)
            {
                var existing = route.Suspensions.FirstOrDefault(s => s.Id == dto.Id && s.Id != 0);
                if (existing != null) // Update
                {
                    existing.SuspensionStart = dto.SuspensionStart;
                    existing.SuspensionEnd = dto.SuspensionEnd;
                    existing.Reason = dto.Reason;
                }
                else // Add
                {
                    route.Suspensions.Add(new RouteSuspension
                    {
                        SuspensionStart = dto.SuspensionStart,
                        SuspensionEnd = dto.SuspensionEnd,
                        Reason = dto.Reason
                    });
                }
            }
        }

        private void UpdateAvailabilities(VehicleRoute route, List<RouteAvailabilityDto>? availabilityDtos)
        {
            // The availabilities do not have an ID, they are identified by DayOfWeek. It's easier to delete them and create them again.
            route.Availabilities.Clear();
            if (availabilityDtos != null)
            {
                foreach (var dto in availabilityDtos)
                {
                    route.Availabilities.Add(new RouteAvailability
                    {
                        DayOfWeek = dto.DayOfWeek,
                        StartTime = dto.StartTime,
                        EndTime = dto.EndTime,
                        IsActive = dto.IsActive
                    });
                }
            }
        }

        private void UpdateFundingSources(VehicleRoute route, List<RouteFundingSourceDto>? fundingSourceDtos)
        {
            // Similar to availabilities, the relationship is simple, so deleting and recreating is efficient.
            route.FundingSources.Clear();
            if (fundingSourceDtos != null)
            {
                foreach (var dto in fundingSourceDtos)
                {
                    route.FundingSources.Add(new RouteFundingSource
                    {
                        FundingSourceId = dto.FundingSourceId
                    });
                }
            }
        }

        #endregion


    }

}

