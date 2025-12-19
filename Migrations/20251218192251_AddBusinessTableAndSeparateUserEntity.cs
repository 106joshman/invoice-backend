using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTableAndSeparateUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentInfo_Users_UserId",
                table: "PaymentInfo");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyLogo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastInvoiceReset",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MonthlyInvoiceCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PaymentInfo",
                newName: "BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentInfo_UserId",
                table: "PaymentInfo",
                newName: "IX_PaymentInfo_BusinessId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Invoices",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                newName: "IX_Invoices_CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Customers",
                newName: "BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                newName: "IX_Customers_BusinessId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Email_UserId",
                table: "Customers",
                newName: "IX_Customers_Email_BusinessId");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedByUserId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeBy = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsMultiUserEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    TaxId = table.Column<string>(type: "text", nullable: false),
                    CompanyLogoUrl = table.Column<string>(type: "text", nullable: false),
                    BrandColor = table.Column<string>(type: "text", nullable: false),
                    MonthlyInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastInvoiceReset = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessUsers_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessId",
                table: "Invoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UpdatedByUserId",
                table: "Invoices",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Name",
                table: "Businesses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUsers_BusinessId",
                table: "BusinessUsers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUsers_UserId",
                table: "BusinessUsers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Businesses_BusinessId",
                table: "Customers",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Businesses_BusinessId",
                table: "Invoices",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedByUserId",
                table: "Invoices",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UpdatedByUserId",
                table: "Invoices",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentInfo_Businesses_BusinessId",
                table: "PaymentInfo",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Businesses_BusinessId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Businesses_BusinessId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedByUserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_UpdatedByUserId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentInfo_Businesses_BusinessId",
                table: "PaymentInfo");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessUsers");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_BusinessId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_UpdatedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "BusinessId",
                table: "PaymentInfo",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentInfo_BusinessId",
                table: "PaymentInfo",
                newName: "IX_PaymentInfo_UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Invoices",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_CreatedByUserId",
                table: "Invoices",
                newName: "IX_Invoices_UserId");

            migrationBuilder.RenameColumn(
                name: "BusinessId",
                table: "Customers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_Email_BusinessId",
                table: "Customers",
                newName: "IX_Customers_Email_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_BusinessId",
                table: "Customers",
                newName: "IX_Customers_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyLogo",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInvoiceReset",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyInvoiceCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_UserId",
                table: "Customers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_UserId",
                table: "Invoices",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentInfo_Users_UserId",
                table: "PaymentInfo",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
