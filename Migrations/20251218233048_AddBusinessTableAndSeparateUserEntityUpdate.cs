using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTableAndSeparateUserEntityUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceItems");
        }
    }
}
