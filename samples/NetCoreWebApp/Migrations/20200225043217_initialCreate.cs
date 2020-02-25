using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetCoreWebApp.Migrations
{
    public partial class initialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregateEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RootId = table.Column<Guid>(nullable: false),
                    AggregateVersion = table.Column<int>(nullable: false),
                    AggregateName = table.Column<string>(maxLength: 40, nullable: true),
                    EventName = table.Column<string>(maxLength: 40, nullable: true),
                    EventVersion = table.Column<int>(nullable: false),
                    EventData = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(nullable: false),
                    MetaData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateEvents_AggregateName",
                table: "AggregateEvents",
                column: "AggregateName");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateEvents_RootId",
                table: "AggregateEvents",
                column: "RootId")
                .Annotation("SqlServer:Include", new[] { "AggregateVersion", "Timestamp", "Id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateEvents");
        }
    }
}
