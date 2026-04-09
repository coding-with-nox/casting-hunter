using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260409110000_AddRegioneSeedTeatri")]
    public partial class AddRegioneSeedTeatri : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Regione",
                table: "BandoSources",
                type: "text",
                nullable: true);

            // Imposta regione ai teatri già presenti
            migrationBuilder.Sql(@"
UPDATE ""BandoSources"" SET ""Regione"" = 'Lombardia'       WHERE ""Id"" = 5;
UPDATE ""BandoSources"" SET ""Regione"" = 'Lazio'           WHERE ""Id"" = 6;
UPDATE ""BandoSources"" SET ""Regione"" = 'Emilia-Romagna'  WHERE ""Id"" = 7;
UPDATE ""BandoSources"" SET ""Regione"" = 'Campania'        WHERE ""Id"" = 8;
UPDATE ""BandoSources"" SET ""Regione"" = 'Sicilia'         WHERE ""Id"" = 9;
UPDATE ""BandoSources"" SET ""Regione"" = 'Piemonte'        WHERE ""Id"" = 10;
");

            // Toscana
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(11, 'Teatro del Maggio Musicale Fiorentino', 'P2 - Teatri e fondazioni ad alta resa', 'https://www.maggiofiorentino.com/',    16, true, true, 'Toscana', 0, 0, 0),
(12, 'Teatro della Pergola - Firenze',        'P2 - Teatri e fondazioni ad alta resa', 'https://www.teatrodellapergola.com/',  16, true, true, 'Toscana', 0, 0, 0),
(13, 'Teatro Metastasio - Prato',             'P2 - Teatri regionali',                 'https://www.metastasio.net/',          17, true, true, 'Toscana', 0, 0, 0),
(14, 'Teatro Goldoni - Livorno',              'P2 - Teatri regionali',                 'https://www.goldoniteatro.it/',        17, true, true, 'Toscana', 0, 0, 0),
(15, 'Teatro del Giglio - Lucca',             'P2 - Teatri regionali',                 'https://www.teatrodelgiglio.it/',      17, true, true, 'Toscana', 0, 0, 0),
(16, 'Teatro Verdi - Pisa',                   'P2 - Teatri regionali',                 'https://www.teatrodipisa.pi.it/',      17, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // Lazio
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(17, 'Teatro di Roma',                            'P2 - Teatri e fondazioni ad alta resa', 'https://www.teatrodiroma.net/',       16, true, true, 'Lazio', 0, 0, 0),
(18, 'Teatro Quirino - Roma',                     'P2 - Teatri regionali',                 'https://www.teatroquirino.it/',       17, true, true, 'Lazio', 0, 0, 0),
(19, 'Teatro Brancaccio - Roma',                  'P2 - Teatri regionali',                 'https://www.teatrobrancaccio.it/',    17, true, true, 'Lazio', 0, 0, 0),
(20, 'Accademia Nazionale di Santa Cecilia',      'P2 - Teatri e fondazioni ad alta resa', 'https://www.santacecilia.it/',        16, true, true, 'Lazio', 0, 0, 0),
(21, 'Teatro Sistina - Roma',                     'P2 - Teatri regionali',                 'https://www.ilsistina.it/',           17, true, true, 'Lazio', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // Emilia-Romagna (TCBO/Bologna già presente con Id=7)
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(22, 'Teatro Regio - Parma',          'P2 - Teatri e fondazioni ad alta resa', 'https://www.teatroregioparma.it/',     16, true, true, 'Emilia-Romagna', 0, 0, 0),
(23, 'Teatro Alighieri - Ravenna',    'P2 - Teatri regionali',                 'https://www.teatroalighieri.org/',     17, true, true, 'Emilia-Romagna', 0, 0, 0),
(24, 'Teatro Municipale - Piacenza',  'P2 - Teatri regionali',                 'https://www.teatripiacenza.it/',       17, true, true, 'Emilia-Romagna', 0, 0, 0),
(25, 'I Teatri - Reggio Emilia',      'P2 - Teatri regionali',                 'https://www.iteatri.re.it/',           17, true, true, 'Emilia-Romagna', 0, 0, 0),
(26, 'Teatro Bonci - Cesena',         'P2 - Teatri regionali',                 'https://www.teatrobonci.it/',          17, true, true, 'Emilia-Romagna', 0, 0, 0),
(27, 'Teatro Comunale - Ferrara',     'P2 - Teatri regionali',                 'https://www.teatrocomunaleferrara.it/',17, true, true, 'Emilia-Romagna', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""BandoSources"" WHERE ""Id"" BETWEEN 11 AND 27;");
            migrationBuilder.DropColumn(name: "Regione", table: "BandoSources");
        }
    }
}
