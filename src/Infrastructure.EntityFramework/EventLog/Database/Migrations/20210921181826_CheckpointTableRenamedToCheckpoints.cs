using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.EntityFramework.Migrations.EventLogDb
{
    public partial class CheckpointTableRenamedToCheckpoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Checkpoint",
                table: "Checkpoint");

            migrationBuilder.RenameTable(
                name: "Checkpoint",
                newName: "Checkpoints");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Checkpoints",
                table: "Checkpoints",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Checkpoints",
                table: "Checkpoints");

            migrationBuilder.RenameTable(
                name: "Checkpoints",
                newName: "Checkpoint");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Checkpoint",
                table: "Checkpoint",
                column: "Id");
        }
    }
}
