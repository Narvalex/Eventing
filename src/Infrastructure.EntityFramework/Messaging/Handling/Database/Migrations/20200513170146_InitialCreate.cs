using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.EntityFramework.Migrations.CheckpointStoreDb
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Checkpoints",
                columns: table => new
                {
                    SubscriptionId = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: false),
                    EventNumber = table.Column<long>(nullable: false),
                    CommitPosition = table.Column<long>(nullable: false),
                    PreparePosition = table.Column<long>(nullable: false),
                    TimeStamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoints", x => x.SubscriptionId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Checkpoints");
        }
    }
}
