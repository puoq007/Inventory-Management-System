using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JigSpecs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JigRequired = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Item = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rev = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PictureUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JigType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalStepPrint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitAmount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Feed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scan = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JigSpecs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locators",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Site = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cabinet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Shelf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartJigMappings",
                columns: table => new
                {
                    PartNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpecId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartJigMappings", x => new { x.PartNumber, x.SpecId });
                });

            migrationBuilder.CreateTable(
                name: "PhysicalJigs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpecId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocatorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentDestination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tool = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamePlateBlack = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamePlateWhite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Part = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JigType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StepPrint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HG = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FS = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JigCapacity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalJigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JigId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    User = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.EmployeeId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JigSpecs");

            migrationBuilder.DropTable(
                name: "Locators");

            migrationBuilder.DropTable(
                name: "PartJigMappings");

            migrationBuilder.DropTable(
                name: "PhysicalJigs");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
