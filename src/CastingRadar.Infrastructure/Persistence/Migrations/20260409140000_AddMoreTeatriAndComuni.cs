using CastingRadar.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CastingRadarDbContext))]
    [Migration("20260409140000_AddMoreTeatriAndComuni")]
    public partial class AddMoreTeatriAndComuni : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Toscana (integrazioni — IDs 28-37) ──────────────────────────────
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(28, 'ORT - Orchestra della Toscana',        'P2 - Teatri e fondazioni ad alta resa', 'https://www.orchestradellatoscana.it/',         16, true, true, 'Toscana', 0, 0, 0),
(29, 'Teatro Verdi - Firenze',               'P2 - Teatri regionali',                 'https://www.teatroverdifirenze.it/',            17, true, true, 'Toscana', 0, 0, 0),
(30, 'Teatro di Rifredi - Firenze',          'P2 - Teatri regionali',                 'https://www.teatrodirifedi.it/',                17, true, true, 'Toscana', 0, 0, 0),
(31, 'Teatro dei Rozzi - Siena',             'P2 - Teatri regionali',                 'https://www.teatrodeirozzi.it/',                17, true, true, 'Toscana', 0, 0, 0),
(32, 'Teatro dei Rinnovati - Siena',         'P2 - Teatri regionali',                 'https://www.comune.siena.it/il-comune/teatro',  17, true, true, 'Toscana', 0, 0, 0),
(33, 'Teatro Politeama Pratese - Prato',     'P2 - Teatri regionali',                 'https://www.politeamapratese.it/',              17, true, true, 'Toscana', 0, 0, 0),
(34, 'Teatro del Popolo - Castelfiorentino', 'P2 - Teatri regionali',                 'https://www.teatrodelpopolo.org/',              17, true, true, 'Toscana', 0, 0, 0),
(35, 'Teatro degli Industri - Grosseto',     'P2 - Teatri regionali',                 'https://www.fondazioneantoniomarzotto.it/',     17, true, true, 'Toscana', 0, 0, 0),
(36, 'Teatro Pacini - Pescia',               'P2 - Teatri regionali',                 'https://www.comune.pescia.pt.it/',              17, true, true, 'Toscana', 0, 0, 0),
(37, 'Fondazione Toscana Spettacolo',        'P2 - Teatri e fondazioni ad alta resa', 'https://www.toscanaspettacolo.it/',             16, true, true, 'Toscana', 0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");

            // ── Bandi comunali - sezioni cultura/concorsi dei principali Comuni (IDs 38-50) ─
            migrationBuilder.Sql(@"
INSERT INTO ""BandoSources"" (""Id"", ""Name"", ""Category"", ""BaseUrl"", ""Priority"", ""IsOfficial"", ""IsEnabled"", ""Regione"", ""LastRunFound"", ""LastRunEligible"", ""LastRunNew"")
VALUES
(38, 'Comune di Roma - Concorsi',            'P1 - Bandi comunali',  'https://www.comune.roma.it/web/it/concorsi.page',                              10, true, true,  'Lazio',            0, 0, 0),
(39, 'Comune di Milano - Concorsi',          'P1 - Bandi comunali',  'https://www.comune.milano.it/aree-tematiche/concorsi',                         10, true, true,  'Lombardia',        0, 0, 0),
(40, 'Comune di Torino - Concorsi',          'P1 - Bandi comunali',  'https://www.comune.torino.it/concorsi/',                                       10, true, true,  'Piemonte',         0, 0, 0),
(41, 'Comune di Firenze - Bandi',            'P1 - Bandi comunali',  'https://www.comune.fi.it/bandi-e-concorsi',                                    10, true, true,  'Toscana',          0, 0, 0),
(42, 'Comune di Bologna - Concorsi',         'P1 - Bandi comunali',  'https://www.comune.bologna.it/bandi-concorsi',                                 10, true, true,  'Emilia-Romagna',   0, 0, 0),
(43, 'Comune di Napoli - Concorsi',          'P1 - Bandi comunali',  'https://www.comune.napoli.it/flex/cm/pages/ServeBLOB.php/L/IT/IDPagina/27506', 10, true, true,  'Campania',         0, 0, 0),
(44, 'Comune di Genova - Concorsi',          'P1 - Bandi comunali',  'https://www.comune.genova.it/servizi/concorsi',                                10, true, true,  'Liguria',          0, 0, 0),
(45, 'Comune di Venezia - Concorsi',         'P1 - Bandi comunali',  'https://www.comune.venezia.it/it/content/bandi-e-concorsi-0',                  10, true, true,  'Veneto',           0, 0, 0),
(46, 'Comune di Palermo - Concorsi',         'P1 - Bandi comunali',  'https://www.comune.palermo.it/concorsi.php',                                   10, true, true,  'Sicilia',          0, 0, 0),
(47, 'Comune di Bari - Concorsi',            'P1 - Bandi comunali',  'https://www.comune.bari.it/web/egov/-/concorsi',                               10, true, true,  'Puglia',           0, 0, 0),
(48, 'Comune di Catania - Concorsi',         'P1 - Bandi comunali',  'https://www.comune.catania.it/il-comune/concorsi/',                            10, true, true,  'Sicilia',          0, 0, 0),
(49, 'Comune di Siena - Bandi',              'P1 - Bandi comunali',  'https://www.comune.siena.it/concorsi-e-avvisi',                                10, true, true,  'Toscana',          0, 0, 0),
(50, 'Comune di Prato - Bandi',              'P1 - Bandi comunali',  'https://www.comune.prato.it/it/home/amministrazione/bandi-e-concorsi.html',    10, true, true,  'Toscana',          0, 0, 0)
ON CONFLICT (""Id"") DO NOTHING;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""BandoSources"" WHERE ""Id"" BETWEEN 28 AND 50;");
        }
    }
}
