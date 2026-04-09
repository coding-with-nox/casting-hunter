using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Copertura completa della Toscana: teatri, fondazioni, accademie e bandi comunali.
    /// IDs 51-90. Non duplica gli ID 11-16, 28-37, 41, 49, 50 già presenti.
    /// </summary>
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260409170000_AddAllToscanaTheatres")]
    public partial class AddAllToscanaTheatres : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── FIRENZE e provincia ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(51, 'Fondazione Teatro della Toscana',       'P2 - Teatri e fondazioni ad alta resa', 'https://www.teatrodellatoscana.it/',                  16, true, true, 'Toscana', 0, 0, 0),
(52, 'Teatro Puccini - Firenze',              'P2 - Teatri regionali',                 'https://www.teatropuccini.it/',                       17, true, true, 'Toscana', 0, 0, 0),
(53, 'Teatro dell''Affratellamento - Firenze','P2 - Teatri regionali',                 'https://www.teatroaffratellamento.it/',               17, true, true, 'Toscana', 0, 0, 0),
(54, 'Teatro Era - Pontedera',                'P2 - Teatri regionali',                 'https://www.teatrodellatoscana.it/teatro-era/',        17, true, true, 'Toscana', 0, 0, 0),
(55, 'Teatro Studio Mila Pieralli - Scandicci','P2 - Teatri regionali',                'https://www.teatrodellatoscana.it/teatro-studio/',     17, true, true, 'Toscana', 0, 0, 0),
(56, 'Teatro Busoni - Empoli',                'P2 - Teatri regionali',                 'https://www.teatroempoli.it/',                        17, true, true, 'Toscana', 0, 0, 0),
(57, 'Comune di Empoli - Bandi',              'P1 - Bandi comunali',                   'https://www.comune.empoli.fi.it/bandi-e-concorsi',    10, true, true, 'Toscana', 0, 0, 0),
(58, 'Comune di Scandicci - Bandi',           'P1 - Bandi comunali',                   'https://www.comune.scandicci.fi.it/',                 10, true, true, 'Toscana', 0, 0, 0),
(59, 'Comune di Sesto Fiorentino - Bandi',    'P1 - Bandi comunali',                   'https://www.comune.sesto-fiorentino.fi.it/',          10, true, true, 'Toscana', 0, 0, 0),
(60, 'Comune di Fiesole - Bandi',             'P1 - Bandi comunali',                   'https://www.comune.fiesole.fi.it/',                   10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── PISTOIA e provincia ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(61, 'Teatri di Pistoia',                     'P2 - Teatri regionali',                 'https://www.teatridipistoia.it/',                     17, true, true, 'Toscana', 0, 0, 0),
(62, 'Teatro Verdi - Montecatini Terme',      'P2 - Teatri regionali',                 'https://www.teatroverdimontecatini.it/',              17, true, true, 'Toscana', 0, 0, 0),
(63, 'Comune di Pistoia - Bandi',             'P1 - Bandi comunali',                   'https://www.comune.pistoia.it/bandi-di-gara',         10, true, true, 'Toscana', 0, 0, 0),
(64, 'Comune di Montecatini Terme - Bandi',   'P1 - Bandi comunali',                   'https://www.comune.montecatini-terme.pt.it/',         10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── LUCCA e provincia ────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(65, 'Teatro dell''Olivo - Camaiore',         'P2 - Teatri regionali',                 'https://www.teatrodellolivo.it/',                     17, true, true, 'Toscana', 0, 0, 0),
(66, 'Teatro Jenco - Viareggio',              'P2 - Teatri regionali',                 'https://www.teatrojenco.it/',                         17, true, true, 'Toscana', 0, 0, 0),
(67, 'Festival Pucciniano - Torre del Lago',  'P2 - Teatri e fondazioni ad alta resa', 'https://www.puccinifestival.it/',                     16, true, true, 'Toscana', 0, 0, 0),
(68, 'La Versiliana - Marina di Pietrasanta', 'P2 - Teatri regionali',                 'https://www.laversiliana.it/',                        17, true, true, 'Toscana', 0, 0, 0),
(69, 'Comune di Lucca - Bandi',               'P1 - Bandi comunali',                   'https://www.comune.lucca.it/concorsi-e-bandi',        10, true, true, 'Toscana', 0, 0, 0),
(70, 'Comune di Viareggio - Bandi',           'P1 - Bandi comunali',                   'https://www.comune.viareggio.lu.it/',                 10, true, true, 'Toscana', 0, 0, 0),
(71, 'Comune di Pietrasanta - Bandi',         'P1 - Bandi comunali',                   'https://www.comune.pietrasanta.lu.it/',               10, true, true, 'Toscana', 0, 0, 0),
(72, 'Comune di Camaiore - Bandi',            'P1 - Bandi comunali',                   'https://www.comune.camaiore.lu.it/',                  10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── PISA e provincia ─────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(73, 'Teatro Sant''Andrea - Pisa',            'P2 - Teatri regionali',                 'https://www.teatrosantandrea.it/',                    17, true, true, 'Toscana', 0, 0, 0),
(74, 'Fondazione Dramma Popolare San Miniato','P2 - Teatri e fondazioni ad alta resa', 'https://www.drammapopolaresentminiato.it/',           16, true, true, 'Toscana', 0, 0, 0),
(75, 'Comune di Pisa - Bandi',                'P1 - Bandi comunali',                   'https://www.comune.pisa.it/concorsi',                 10, true, true, 'Toscana', 0, 0, 0),
(76, 'Comune di Pontedera - Bandi',           'P1 - Bandi comunali',                   'https://www.comune.pontedera.pi.it/',                 10, true, true, 'Toscana', 0, 0, 0),
(77, 'Comune di San Miniato - Bandi',         'P1 - Bandi comunali',                   'https://www.comune.san-miniato.pi.it/',               10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── LIVORNO e provincia ──────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(78, 'Comune di Livorno - Bandi',             'P1 - Bandi comunali',                   'https://www.comune.livorno.it/concorsi',              10, true, true, 'Toscana', 0, 0, 0),
(79, 'Comune di Piombino - Bandi',            'P1 - Bandi comunali',                   'https://www.comune.piombino.li.it/',                  10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── SIENA e provincia ────────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(80, 'Accademia Musicale Chigiana - Siena',   'P2 - Teatri e fondazioni ad alta resa', 'https://www.chigiana.it/',                            16, true, true, 'Toscana', 0, 0, 0),
(81, 'Teatro Poliziano - Montepulciano',      'P2 - Teatri regionali',                 'https://www.teatropoliziano.it/',                     17, true, true, 'Toscana', 0, 0, 0),
(82, 'Cantiere Internazionale d''Arte - Montepulciano', 'P2 - Teatri regionali',       'https://www.cantiere.org/',                           17, true, true, 'Toscana', 0, 0, 0),
(83, 'Comune di Montepulciano - Bandi',       'P1 - Bandi comunali',                   'https://www.comune.montepulciano.si.it/',             10, true, true, 'Toscana', 0, 0, 0),
(84, 'Comune di Montalcino - Bandi',          'P1 - Bandi comunali',                   'https://www.comune.montalcino.si.it/',                10, true, true, 'Toscana', 0, 0, 0),
(85, 'Comune di Chianciano Terme - Bandi',    'P1 - Bandi comunali',                   'https://www.comune.chiancianoterme.si.it/',           10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── AREZZO e provincia ───────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(86, 'Teatro Pietro Aretino - Arezzo',        'P2 - Teatri regionali',                 'https://www.pietroaretino.it/',                       17, true, true, 'Toscana', 0, 0, 0),
(87, 'Comune di Arezzo - Bandi',              'P1 - Bandi comunali',                   'https://www.comune.arezzo.it/bandi-e-concorsi',       10, true, true, 'Toscana', 0, 0, 0),
(88, 'Comune di Cortona - Bandi',             'P1 - Bandi comunali',                   'https://www.comune.cortona.ar.it/',                   10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── GROSSETO e provincia ─────────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(89, 'Comune di Grosseto - Bandi',            'P1 - Bandi comunali',                   'https://www.comune.grosseto.it/bandi-e-concorsi',     10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── MASSA-CARRARA e provincia ────────────────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(90, 'Teatro Guglielmi - Massa',              'P2 - Teatri regionali',                 'https://www.teatroguglielmi.it/',                     17, true, true, 'Toscana', 0, 0, 0),
(91, 'Teatro Animosi - Carrara',              'P2 - Teatri regionali',                 'https://www.teatroanimosi.it/',                       17, true, true, 'Toscana', 0, 0, 0),
(92, 'Comune di Massa - Bandi',               'P1 - Bandi comunali',                   'https://www.comune.massa.ms.it/bandi-e-concorsi',     10, true, true, 'Toscana', 0, 0, 0),
(93, 'Comune di Carrara - Bandi',             'P1 - Bandi comunali',                   'https://www.comune.carrara.ms.it/',                   10, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""BandoSources"" WHERE ""Id"" BETWEEN 51 AND 93;");
        }
    }
}
