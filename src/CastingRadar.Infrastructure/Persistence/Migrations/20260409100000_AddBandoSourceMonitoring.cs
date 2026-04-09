using System;
using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260409100000_AddBandoSourceMonitoring")]
    public partial class AddBandoSourceMonitoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRunAt",
                table: "BandoSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastRunFound",
                table: "BandoSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRunEligible",
                table: "BandoSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRunNew",
                table: "BandoSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastRunError",
                table: "BandoSources",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "LastRunAt", table: "BandoSources");
            migrationBuilder.DropColumn(name: "LastRunFound", table: "BandoSources");
            migrationBuilder.DropColumn(name: "LastRunEligible", table: "BandoSources");
            migrationBuilder.DropColumn(name: "LastRunNew", table: "BandoSources");
            migrationBuilder.DropColumn(name: "LastRunError", table: "BandoSources");
        }
    }
}
