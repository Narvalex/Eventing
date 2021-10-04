using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.EntityFramework.Migrations.EventLogDb
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Checkpoint",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventNumber = table.Column<long>(type: "bigint", nullable: false),
                    CommitPosition = table.Column<long>(type: "bigint", nullable: false),
                    PreparePosition = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checkpoint", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventNumber = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CausationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommitId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventSourcedType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StreamId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventSourcedVersion = table.Column<long>(type: "bigint", nullable: false),
                    CausationNumber = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommitPosition = table.Column<long>(type: "bigint", nullable: false),
                    PreparePosition = table.Column<long>(type: "bigint", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayMode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommandTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PositionLatitude = table.Column<double>(type: "float", nullable: true),
                    PositionLongitude = table.Column<double>(type: "float", nullable: true),
                    PositionAccuracy = table.Column<double>(type: "float", nullable: true),
                    PositionAltitude = table.Column<double>(type: "float", nullable: true),
                    PositionAltitudeAccuracy = table.Column<double>(type: "float", nullable: true),
                    PositionHeading = table.Column<double>(type: "float", nullable: true),
                    PositionSpeed = table.Column<double>(type: "float", nullable: true),
                    PositionTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PositionError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventNumber);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventSourcedType_EventSourcedVersion",
                table: "Events",
                columns: new[] { "EventSourcedType", "EventSourcedVersion" })
                .Annotation("SqlServer:Clustered", false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Checkpoint");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
