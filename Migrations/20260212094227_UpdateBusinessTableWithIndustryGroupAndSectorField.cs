using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBusinessTableWithIndustryGroupAndSectorField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Industry",
                table: "Businesses",
                newName: "IndustrySector");

            migrationBuilder.AddColumn<string>(
                name: "IndustryGroup",
                table: "Businesses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndustryGroup",
                table: "Businesses");

            migrationBuilder.RenameColumn(
                name: "IndustrySector",
                table: "Businesses",
                newName: "Industry");
        }
    }
}
