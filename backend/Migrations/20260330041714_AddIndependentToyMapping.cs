using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndependentToyMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JigToyMappings");
        }
    }
}
