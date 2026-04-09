using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260409120000_FixMetastasioBandoUrl")]
    public partial class FixMetastasioBandoUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""BandoSources""
SET ""BaseUrl"" = 'https://www.metastasio.it/'
WHERE ""Name"" = 'Teatro Metastasio - Prato';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""BandoSources""
SET ""BaseUrl"" = 'https://www.metastasio.net/'
WHERE ""Name"" = 'Teatro Metastasio - Prato';
");
        }
    }
}
