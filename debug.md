# Debug — Guida completa

Tutti i comandi da lanciare in console per avviare, ispezionare e fermare lo stack di debug.

---

## Avvio stack di debug

```bash
# Prima volta: costruisce le immagini e installa VSDBG
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build

# Avvii successivi (senza rebuild)
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d
```

---

## Porte esposte in debug

| Servizio | Porta locale | Descrizione |
|----------|-------------|-------------|
| API | `5050` | HTTP endpoint (`http://localhost:5050`) |
| API | `2222` | VSDBG remote debugger attach |
| Worker | `2223` | VSDBG remote debugger attach |
| PostgreSQL | `5432` | Accesso diretto da DB client |
| Adminer | `8090` | GUI database (`http://localhost:8090`) |

---

## Log

```bash
# Tutti i servizi, in tempo reale
docker compose -f docker-compose.yml -f docker-compose.debug.yml logs -f

# Solo API
docker compose -f docker-compose.yml -f docker-compose.debug.yml logs -f api

# Solo Worker
docker compose -f docker-compose.yml -f docker-compose.debug.yml logs -f worker

# Ultime 100 righe del DB
docker compose -f docker-compose.yml -f docker-compose.debug.yml logs --tail=100 db
```

---

## Shell interattiva nei container

```bash
# Entra nell'API container
docker compose -f docker-compose.yml -f docker-compose.debug.yml exec api bash

# Entra nel Worker container
docker compose -f docker-compose.yml -f docker-compose.debug.yml exec worker bash

# Entra nel DB container (psql)
docker compose -f docker-compose.yml -f docker-compose.debug.yml exec db \
  psql -U castingradar -d castingradar
```

---

## Database (psql)

```bash
# Connessione diretta dall'host (richiede psql installato localmente)
psql -h localhost -p 5432 -U castingradar -d castingradar

# Oppure via container
docker compose -f docker-compose.yml -f docker-compose.debug.yml exec db \
  psql -U castingradar -d castingradar

# Query rapide utili
# -- conta i casting
SELECT count(*) FROM "CastingCalls";

# -- ultime 10 entry scrappate
SELECT "Title", "SourceName", "ScrapedAt"
FROM "CastingCalls"
ORDER BY "ScrapedAt" DESC
LIMIT 10;

# -- stato delle fonti
SELECT "Name", "IsEnabled", "LastScrapedAt", "ErrorCount" FROM "Sources";
```

---

## EF Core Migrations (in debug)

```bash
# Applica migration al DB di debug (PostgreSQL)
dotnet ef database update \
  --project src/CastingRadar.Infrastructure \
  --startup-project src/CastingRadar.Api \
  -- --UsePostgres=true \
     --ConnectionStrings:DefaultConnection="Host=localhost;Port=5432;Database=castingradar;Username=castingradar;Password=castingradar_secret"

# Crea una nuova migration
dotnet ef migrations add NomeMigration \
  --project src/CastingRadar.Infrastructure \
  --startup-project src/CastingRadar.Api \
  --output-dir Persistence/Migrations
```

---

## Trigger manuale dello scraping

```bash
# Scraping di tutte le fonti
curl -X POST http://localhost:5050/api/sources/scrape-all

# Scraping di una singola fonte
curl -X POST http://localhost:5050/api/sources/Ticonsiglio/scrape
curl -X POST http://localhost:5050/api/sources/iMoviez/scrape
curl -X POST http://localhost:5050/api/sources/AttoriCasting/scrape
curl -X POST http://localhost:5050/api/sources/Mandy/scrape
curl -X POST http://localhost:5050/api/sources/Backstage/scrape

# Leggi la lista casting (JSON)
curl http://localhost:5050/api/castings | jq .

# Leggi statistiche
curl http://localhost:5050/api/stats | jq .

# Health check
curl http://localhost:5050/health
```

---

## Remote Debugger (VSDBG)

### Visual Studio Code — `launch.json`

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach API (Docker)",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickRemoteProcess}",
      "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": [
          "compose", "-f", "docker-compose.yml", "-f", "docker-compose.debug.yml",
          "exec", "-i", "api"
        ],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceFolder}"
      },
      "sourceFileMap": {
        "/src": "${workspaceFolder}"
      }
    },
    {
      "name": "Attach Worker (Docker)",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickRemoteProcess}",
      "pipeTransport": {
        "pipeProgram": "docker",
        "pipeArgs": [
          "compose", "-f", "docker-compose.yml", "-f", "docker-compose.debug.yml",
          "exec", "-i", "worker"
        ],
        "debuggerPath": "/vsdbg/vsdbg",
        "pipeCwd": "${workspaceFolder}"
      },
      "sourceFileMap": {
        "/src": "${workspaceFolder}"
      }
    }
  ]
}
```

### Rider / Visual Studio — SSH Remote Debug

Host: `localhost` — Porta: `2222` (API) o `2223` (Worker)  
Debugger path nel container: `/vsdbg/vsdbg`

---

## Rebuild selettivo

```bash
# Rebuilda solo l'API (es. dopo modifica a un Dockerfile)
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build api

# Rebuilda solo il Worker
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build worker
```

---

## Stop e pulizia

```bash
# Ferma i container (mantiene volumi e immagini)
docker compose -f docker-compose.yml -f docker-compose.debug.yml down

# Ferma e rimuove i volumi (ATTENZIONE: cancella il DB e il vsdbg installato)
docker compose -f docker-compose.yml -f docker-compose.debug.yml down -v

# Rimuove anche le immagini buildate localmente
docker compose -f docker-compose.yml -f docker-compose.debug.yml down --rmi local

# Pulizia totale Docker (tutti i container/volumi/immagini non usati)
docker system prune -af --volumes
```

---

## Test

```bash
# Tutti i test
dotnet test CastingRadar.slnx

# Solo Domain
dotnet test tests/CastingRadar.Domain.Tests

# Solo Application (con output verboso)
dotnet test tests/CastingRadar.Application.Tests --logger "console;verbosity=detailed"

# Con coverage (richiede coverlet)
dotnet test CastingRadar.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```
