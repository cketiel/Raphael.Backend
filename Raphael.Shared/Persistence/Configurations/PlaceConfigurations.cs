using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Entities.Places;

namespace Raphael.Shared.Persistence.Configurations;

// RE-054. Restrict everywhere: a place, a note or a trip end is never deleted from under another
// row. Notes are removed by status, not by DELETE, so who removed what stays on record.

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("Places");

        // One active facility per address and company, one active home per patient, address and
        // company: the database, not the order of the work, keeps two processes from creating the
        // same place twice. The loser of the race reads the winner's row and uses it.
        builder.HasIndex(x => new { x.OwnerProviderId, x.AddressKey })
            .IsUnique()
            .HasFilter("[CustomerId] IS NULL AND [IsActive] = 1")
            .HasDatabaseName("UX_Places_Facility");

        builder.HasIndex(x => new { x.CustomerId, x.OwnerProviderId, x.AddressKey })
            .IsUnique()
            .HasFilter("[CustomerId] IS NOT NULL AND [IsActive] = 1")
            .HasDatabaseName("UX_Places_Home");

        builder.HasOne(x => x.Customer).WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CatalogIntegrator>().WithMany()
            .HasForeignKey(x => x.CatalogIntegratorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlaceEntranceConfiguration : IEntityTypeConfiguration<PlaceEntrance>
{
    public void Configure(EntityTypeBuilder<PlaceEntrance> builder)
    {
        builder.ToTable("PlaceEntrances");

        builder.HasOne(x => x.Place).WithMany(p => p.Entrances)
            .HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);

        // "Is there an entrance within a few metres of this point": a bounding box on latitude.
        builder.HasIndex(x => new { x.Latitude, x.Longitude })
            .HasDatabaseName("IX_PlaceEntrances_LatLng");
    }
}

public class PlaceNoteConfiguration : IEntityTypeConfiguration<PlaceNote>
{
    public void Configure(EntityTypeBuilder<PlaceNote> builder)
    {
        builder.ToTable("PlaceNotes");

        builder.HasOne(x => x.Place).WithMany(p => p.Notes)
            .HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PlaceEntrance).WithMany()
            .HasForeignKey(x => x.PlaceEntranceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PlaceId, x.Status }).HasDatabaseName("IX_PlaceNotes_Place_Status");
    }
}

public class CustomerNoteConfiguration : IEntityTypeConfiguration<CustomerNote>
{
    public void Configure(EntityTypeBuilder<CustomerNote> builder)
    {
        builder.ToTable("CustomerNotes");

        builder.HasOne(x => x.Customer).WithMany()
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CustomerId, x.Status }).HasDatabaseName("IX_CustomerNotes_Customer_Status");
    }
}

public class TripStopNoteConfiguration : IEntityTypeConfiguration<TripStopNote>
{
    public void Configure(EntityTypeBuilder<TripStopNote> builder)
    {
        builder.ToTable("TripStopNotes");

        builder.HasOne(x => x.Trip).WithMany()
            .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TripId, x.Side }).IsUnique().HasDatabaseName("UX_TripStopNotes_Trip_Side");

        builder.HasOne(x => x.Place).WithMany()
            .HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PlaceEntrance).WithMany()
            .HasForeignKey(x => x.PlaceEntranceId).OnDelete(DeleteBehavior.Restrict);

        // The refresh after a note changes: every trip end at this place.
        builder.HasIndex(x => x.PlaceId)
            .HasFilter("[PlaceId] IS NOT NULL")
            .HasDatabaseName("IX_TripStopNotes_PlaceId");
    }
}
