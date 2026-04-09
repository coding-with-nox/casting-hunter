using System;
using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260408153000_AddBandiPhase1")]
    public partial class AddBandiPhase1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bandi",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IssuerName = table.Column<string>(type: "text", nullable: false),
                    IssuerType = table.Column<string>(type: "text", nullable: false),
                    SourceName = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    ApplicationUrl = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Discipline = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: true),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bandi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BandoSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BandoSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bandi_ContentHash",
                table: "Bandi",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BandoSources_Name",
                table: "BandoSources",
                column: "Name",
                unique: true);

            // Raw SQL to avoid EF model resolution issues when Designer file is absent
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""BaseUrl"", ""Category"", ""IsEnabled"", ""IsOfficial"", ""Name"", ""Priority"") VALUES
(1,  'https://www.inpa.gov.it/bandi-e-avvisi/',                         'P1 - Fonti nazionali',                 true, true, 'inPA - bandi e avvisi', 1),
(2,  'https://www.gazzettaufficiale.it/gazzetta/concorsi/',             'P1 - Fonti nazionali',                 true, true, 'Gazzetta Ufficiale - 4a serie concorsi', 2),
(3,  'https://spettacolo.cultura.gov.it/',                              'P1 - Fonti nazionali',                 true, true, 'MiC Spettacolo', 3),
(4,  'https://spettacolo.cultura.gov.it/fondazioni-liriche/',           'P1 - Fonti nazionali',                 true, true, 'MiC - fondazioni lirico-sinfoniche ed elenchi organismi', 4),
(5,  'https://www.teatroallascala.org/',                                'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro alla Scala', 10),
(6,  'https://www.operaroma.it/',                                       'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro dell Opera di Roma', 11),
(7,  'https://www.tcbo.it/',                                            'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro Comunale di Bologna', 12),
(8,  'https://www.teatrosancarlo.it/',                                  'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro di San Carlo', 13),
(9,  'https://www.teatromassimo.it/',                                   'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro Massimo', 14),
(10, 'https://www.teatrostabiletorino.it/',                             'P2 - Teatri e fondazioni ad alta resa', true, true, 'Teatro Stabile di Torino', 15)
ON CONFLICT (""Id"") DO NOTHING;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bandi");

            migrationBuilder.DropTable(
                name: "BandoSources");
        }
    }
}
