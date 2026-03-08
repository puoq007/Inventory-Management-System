using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CombineJigSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feed",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Item",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartType",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PictureUrl",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Rev",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scan",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToolType",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToyNumber",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Week",
                table: "PhysicalJigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feed",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "Item",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "PartType",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "PictureUrl",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "Rev",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "Scan",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "ToolType",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "ToyNumber",
                table: "PhysicalJigs");

            migrationBuilder.DropColumn(
                name: "Week",
                table: "PhysicalJigs");
        }
    }
}
