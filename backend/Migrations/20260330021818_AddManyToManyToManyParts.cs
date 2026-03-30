using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddManyToManyToManyParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JigPartMappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ToolNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JigPartMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartMasters",
                columns: table => new
                {
                    PartNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ToyNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartMasters", x => x.PartNumber);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JigPartMappings_ToolNo_PartNumber",
                table: "JigPartMappings",
                columns: new[] { "ToolNo", "PartNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JigPartMappings");

            migrationBuilder.DropTable(
                name: "PartMasters");
        }
    }
}
