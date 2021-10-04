using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.EntityFramework.Migrations.EventStoreDb
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Position = table.Column<long>(nullable: false),
                    Category = table.Column<string>(nullable: false),
                    SourceId = table.Column<string>(nullable: false),
                    Version = table.Column<long>(nullable: false),
                    EventType = table.Column<string>(nullable: true),
                    TimeStamp = table.Column<DateTime>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    Metadata = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => new { x.Category, x.SourceId, x.Version });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Position",
                table: "Events",
                column: "Position");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
