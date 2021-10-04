using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.EntityFramework.Migrations
{
    public partial class AddSchemas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "Snapshots",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assembly",
                table: "Snapshots",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SchemaVersion",
                table: "Snapshots",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Snapshots",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Schemas",
                columns: table => new
                {
                    Type = table.Column<string>(nullable: false),
                    Assembly = table.Column<string>(nullable: false),
                    Version = table.Column<int>(nullable: false),
                    Hash = table.Column<string>(nullable: false),
                    ThereAreStaleSnapshots = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schemas", x => x.Type);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Type_SchemaVersion",
                table: "Snapshots",
                columns: new[] { "Type", "SchemaVersion" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schemas");

            migrationBuilder.DropIndex(
                name: "IX_Snapshots_Type_SchemaVersion",
                table: "Snapshots");

            migrationBuilder.DropColumn(
                name: "Assembly",
                table: "Snapshots");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "Snapshots");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Snapshots");

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "Snapshots",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
