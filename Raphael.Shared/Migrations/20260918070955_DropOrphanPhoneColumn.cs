using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <summary>
    /// Removes a 'Phone' column from dbo.Users that no entity maps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The User entity has PhoneNumber and has had for a long time. Something also left a
    /// separate, NOT NULL 'Phone' column behind, and because nothing in the model names it,
    /// every INSERT into Users omits it and SQL Server rejects the row:
    /// </para>
    /// <code>
    /// Cannot insert the value NULL into column 'Phone', table '...dbo.Users';
    /// column does not allow nulls. UPDATE fails.
    /// </code>
    /// <para>
    /// It stayed invisible because the production database has never been rebuilt from this
    /// chain of migrations: it grew one migration at a time over eighteen months. The first
    /// database ever created from the chain in one go was the Azure one, on 2026-09-18, and it
    /// could not seed a single user.
    /// </para>
    /// <para>
    /// Written as raw SQL rather than migrationBuilder.DropColumn, and guarded, for one
    /// reason: this runs against databases that do not have the column, where DropColumn
    /// would throw. The guard makes it do nothing there.
    /// </para>
    /// </remarks>
    public partial class DropOrphanPhoneColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A column cannot be dropped while a default constraint is bound to it, and the
            // constraint's name is generated, so it has to be looked up rather than named.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'Phone')
BEGIN
    DECLARE @constraint sysname;

    SELECT @constraint = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[Users]') AND c.name = N'Phone';

    IF @constraint IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @constraint + N']');

    ALTER TABLE [dbo].[Users] DROP COLUMN [Phone];
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately nullable, although the column this reverses was NOT NULL.
            // Recreating it exactly would recreate the failure: a NOT NULL column that no
            // entity maps makes every insert into Users fail, and on a table with rows the
            // ALTER itself would be rejected too. Going back should not restore a defect.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'Phone')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [Phone] NVARCHAR(MAX) NULL;
END
");
        }
    }
}
