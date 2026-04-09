using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveToyNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JigToyMappings");

            migrationBuilder.DropColumn(
                name: "ToyNumber",
                table: "PartMasters");

            migrationBuilder.DropColumn(
                name: "ToyNumber",
                table: "Jigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToyNumber",
                table: "PartMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToyNumber",
                table: "Jigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JigToyMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ToolNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ToyNumber = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JigToyMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JigToyMappings_ToolNo_ToyNumber",
                table: "JigToyMappings",
                columns: new[] { "ToolNo", "ToyNumber" },
                unique: true);
        }
    }
}
