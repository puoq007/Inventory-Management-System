using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AllowEditableJigId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop old PK
            migrationBuilder.DropPrimaryKey(
                name: "PK_Jigs",
                table: "Jigs");

            // 2. Rename column in Transactions (this was logical JigId, now renamed to JigUid)
            migrationBuilder.RenameColumn(
                name: "JigId",
                table: "Transactions",
                newName: "JigUid");

            // 3. Add Uid column (nullable first to populate)
            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "Jigs",
                type: "nvarchar(450)",
                nullable: true);

            // 4. Populate Uid with NEWID() for existing rows
            migrationBuilder.Sql("UPDATE Jigs SET Uid = NEWID() WHERE Uid IS NULL");

            // 5. Update Transactions.JigUid (which contains old logical Id) to use the new Uid
            migrationBuilder.Sql("UPDATE t SET t.JigUid = j.Uid FROM Transactions t INNER JOIN Jigs j ON t.JigUid = j.Id");

            // 6. Make Uid NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "Uid",
                table: "Jigs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            // 7. Make Id nullable (logical ID)
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Jigs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // 8. Add new PK on Uid
            migrationBuilder.AddPrimaryKey(
                name: "PK_Jigs",
                table: "Jigs",
                column: "Uid");

            // 9. Add Unique index on logical Id
            migrationBuilder.CreateIndex(
                name: "IX_Jigs_Id",
                table: "Jigs",
                column: "Id",
                unique: true,
                filter: "[Id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Jigs",
                table: "Jigs");

            migrationBuilder.DropIndex(
                name: "IX_Jigs_Id",
                table: "Jigs");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "Jigs");

            migrationBuilder.RenameColumn(
                name: "JigUid",
                table: "Transactions",
                newName: "JigId");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Jigs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jigs",
                table: "Jigs",
                column: "Id");
        }
    }
}
