using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "InvoiceItems");
        }
    }
}
