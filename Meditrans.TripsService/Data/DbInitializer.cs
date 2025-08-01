using Microsoft.EntityFrameworkCore;
using Raphael.TripsService.Models;
namespace Raphael.TripsService.Data
{
    public class DbInitializer
    {
        public static void Seed(TripsDbContext context)
        {
            // Aplica migraciones pendientes
            context.Database.Migrate();

            // Verifica si ya hay usuarios
            if (context.Trips.Any()) return;

            var trips = new List<Trip>
            {  
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="09:10 AM", PickupAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", DropoffAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", PickupLatitude=27.351814, PickupLongitude=-82.542549, DropoffLatitude=27.334042, DropoffLongitude=-82.514795 },
                new Trip { SpaceType = "AMB", PatientName="Alicia Ambrose", Date="2025-04-04", FromTime="01:00 PM", PickupAddress="240b South Tuttle Avenue, Sarasota, FL 34237, EE. UU.", DropoffAddress="1401 16th Street, Sarasota, FL 34236, EE. UU.", PickupLatitude=27.334042, PickupLongitude=-82.514795, DropoffLatitude=27.351814, DropoffLongitude=-82.542549 },

                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="08:50 AM", PickupAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", DropoffAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", PickupLatitude=27.292011, PickupLongitude=-82.429845, DropoffLatitude=27.310791, DropoffLongitude=-82.530251 },
                new Trip { SpaceType = "AMB", PatientName="Regina Baker", Date="2025-04-04", FromTime="11:31 AM", PickupAddress="2540 South Tamiami Trail, Sarasota, FL 34239, EE. UU.", DropoffAddress="7059 Jarvis Road, Sarasota, FL 34241, EE. UU.", PickupLatitude=27.310791, PickupLongitude=-82.530251, DropoffLatitude=27.292011, DropoffLongitude=-82.429845 }
            
            };

            context.Trips.AddRange(trips);
            context.SaveChanges();
        }
    }
}

