using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raphael.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessEventCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEventCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessEventCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    SeverityId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEventGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEventGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEventGroups_BusinessEventCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BusinessEventCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationActions_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMetadata_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEvents_BusinessEventGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "BusinessEventGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEventDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratesNotification = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEventDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEventDefinitions_BusinessEvents_BusinessEventId",
                        column: x => x.BusinessEventId,
                        principalTable: "BusinessEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessEventDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    SeverityId = table.Column<int>(type: "int", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRules_BusinessEventDefinitions_BusinessEventDefinitionId",
                        column: x => x.BusinessEventDefinitionId,
                        principalTable: "BusinessEventDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRuleActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRuleActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRuleActions_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRuleChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<int>(type: "int", nullable: false),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRuleChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRuleChannels_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRuleConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRuleConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRuleConditions_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRuleRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientTypeId = table.Column<int>(type: "int", nullable: false),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRuleRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRuleRecipients_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEventDefinitions_BusinessEventId",
                table: "BusinessEventDefinitions",
                column: "BusinessEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEventGroups_CategoryId",
                table: "BusinessEventGroups",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessEvents_GroupId",
                table: "BusinessEvents",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationActions_NotificationId",
                table: "NotificationActions",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                table: "NotificationDeliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMetadata_NotificationId",
                table: "NotificationMetadata",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_NotificationId",
                table: "NotificationRecipients",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRuleActions_NotificationRuleId",
                table: "NotificationRuleActions",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRuleChannels_NotificationRuleId",
                table: "NotificationRuleChannels",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRuleConditions_NotificationRuleId",
                table: "NotificationRuleConditions",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRuleRecipients_NotificationRuleId",
                table: "NotificationRuleRecipients",
                column: "NotificationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_BusinessEventDefinitionId",
                table: "NotificationRules",
                column: "BusinessEventDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationActions");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "NotificationMetadata");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "NotificationRuleActions");

            migrationBuilder.DropTable(
                name: "NotificationRuleChannels");

            migrationBuilder.DropTable(
                name: "NotificationRuleConditions");

            migrationBuilder.DropTable(
                name: "NotificationRuleRecipients");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationRules");

            migrationBuilder.DropTable(
                name: "BusinessEventDefinitions");

            migrationBuilder.DropTable(
                name: "BusinessEvents");

            migrationBuilder.DropTable(
                name: "BusinessEventGroups");

            migrationBuilder.DropTable(
                name: "BusinessEventCategory");
        }
    }
}
