using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTriggerUpdateTripStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE TRIGGER trg_UpdateTripStatusOnPerformed
            ON Schedules
            AFTER UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF UPDATE(Performed)
                BEGIN
                    -- Actualizar Trips
                    UPDATE T
                    SET T.Status = CASE 
                                        WHEN i.EventType = 1 THEN 'InProgress'
                                        WHEN i.EventType = 2 THEN 'Finished'
                                        ELSE T.Status 
                                   END
                    FROM Trips T
                    INNER JOIN inserted i ON T.Id = i.TripId
                    INNER JOIN deleted d ON i.Id = d.Id
                    WHERE i.Performed = 1 AND d.Performed = 0 AND i.TripId IS NOT NULL;

                    -- Insertar Log
                    INSERT INTO TripLogs (TripId, Status, [Date], [Time])
                    SELECT 
                        i.TripId,
                        CASE WHEN i.EventType = 1 THEN 'InProgress' ELSE 'Finished' END,
                        CAST(GETDATE() AS DATE),
                        CAST(GETDATE() AS TIME)
                    FROM inserted i
                    INNER JOIN deleted d ON i.Id = d.Id
                    WHERE i.Performed = 1 AND d.Performed = 0 AND i.TripId IS NOT NULL AND i.EventType IN (1, 2);
                END
            END
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_UpdateTripStatusOnPerformed");
        }
    }
}
