using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceService.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToBusinessEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Name_Email",
                table: "Businesses",
                columns: new[] { "Name", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_Name_Email",
                table: "Businesses");
        }
    }
}
