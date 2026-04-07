# CastingRadar

Aggregatore automatico di bandi di casting per attrici italiane. Aggrega da 7 fonti italiane e internazionali, deduplica, filtra per profilo e notifica via Telegram.

## Stack

- **Backend**: .NET 9, ASP.NET Core Minimal API
- **Worker**: BackgroundService con PeriodicTimer (ogni 6 ore)
- **Scraping**: AngleSharp + HttpClient con Polly (retry + circuit breaker)
- **Database**: SQLite (dev) / PostgreSQL (prod) tramite Entity Framework Core 9
- **Notifiche**: Telegram Bot API
- **Frontend**: React 18 + TypeScript + TailwindCSS (Vite)

## Prerequisiti

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (per il frontend)
- Docker + Docker Compose (opzionale, per produzione e debug containerizzato)

---

## Avvio locale (sviluppo senza Docker)

### 1. Ripristina dipendenze

```bash
dotnet restore CastingRadar.slnx
cd frontend && npm install && cd ..
```

### 2. Avvia il backend

```bash
dotnet run --project src/CastingRadar.Api
# API disponibile su http://localhost:5000
# Il database SQLite (castingradar.db) viene creato automaticamente
```

### 3. Avvia il frontend (terminale separato)

```bash
cd frontend
npm run dev
# App disponibile su http://localhost:5173
# Le chiamate /api/* sono proxate automaticamente verso http://localhost:5000
```

### 4. Build frontend per produzione embedded

```bash
cd frontend
npm run build
# Output in src/CastingRadar.Api/wwwroot/
# Dopo la build, dotnet run serve anche la SPA su http://localhost:5000
```

---

## Docker Compose — Produzione

Esegui **solo** con `docker-compose.yml`. Usa immagini Release ottimizzate, nessuna porta di debug esposta.

### 1. Crea il file `.env`

```bash
cp .env.example .env   # se esiste, altrimenti crea manualmente
```

```env
TELEGRAM_BOT_TOKEN=123456:ABC-DEF...
TELEGRAM_CHAT_ID=123456789
DB_PASSWORD=password_sicura
```

### 2. Avvia lo stack

```bash
docker compose up -d
```

```
Servizi avviati:
  db       → PostgreSQL 16 (interno, non esposto)
  api      → http://localhost:5000
  worker   → background scraper (nessuna porta)
```

### 3. Comandi utili

```bash
# Visualizza i log in tempo reale
docker compose logs -f

# Log solo dell'API
docker compose logs -f api

# Ferma tutto
docker compose down

# Ferma e rimuove anche i volumi (attenzione: cancella il DB)
docker compose down -v
```

---

## Docker Compose — Debug

Esegui con **entrambi** i file sovrapposti: `docker-compose.yml` + `docker-compose.debug.yml`.
Il secondo file fa override del primo aggiungendo strumenti di debug senza modificare il compose di produzione.

> Per i dettagli completi (porte, comandi attach, variabili) vedi [debug.md](debug.md).

### 1. Avvia lo stack di debug

```bash
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d
```

```
Servizi avviati:
  db          → PostgreSQL 16 (porta 5432 esposta per DBeaver/psql)
  api         → http://localhost:5000  (dotnet watch, sorgenti montati)
  api         → porta 2222             (VSDBG remote attach)
  worker      → porta 2223             (VSDBG remote attach)
  adminer     → http://localhost:8090  (GUI database, tema dark)
  vsdbg-installer → sidecar, installa il debugger e si chiude
```

### 2. Ferma lo stack di debug

```bash
docker compose -f docker-compose.yml -f docker-compose.debug.yml down
```

### 3. Rebuild dopo modifiche ai `.csproj` o ai Dockerfile

```bash
docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d --build
```

---

## Configurazione Telegram

1. Crea un bot con [@BotFather](https://t.me/BotFather) → ottieni il **Bot Token**
2. Invia un messaggio al bot, poi usa [@userinfobot](https://t.me/userinfobot) → ottieni il **Chat ID**
3. Imposta i valori nel file `.env` oppure dalla pagina **Impostazioni** nell'app

---

## Test

```bash
dotnet test CastingRadar.slnx
```

---

## API endpoints

| Metodo | Path | Descrizione |
|--------|------|-------------|
| GET | `/api/castings` | Lista casting (filtri: keywords, types, regions, onlyPaid, gender) |
| GET | `/api/castings/{id}` | Dettaglio casting |
| POST | `/api/castings/{id}/favorite` | Toggle preferito |
| POST | `/api/castings/{id}/applied` | Segna come candidata |
| GET | `/api/sources` | Stato delle fonti |
| POST | `/api/sources/scrape-all` | Trigger scraping manuale (rate limit: 3/10min) |
| POST | `/api/sources/{name}/scrape` | Scraping di una singola fonte |
| GET | `/api/profile` | Profilo utente |
| PUT | `/api/profile` | Aggiorna preferenze |
| GET | `/api/stats` | Statistiche |
| GET | `/health` | Health check |

---

## Fonti supportate

| Fonte | Regione |
|-------|---------|
| Ticonsiglio | Italia |
| AttoriCasting | Italia |
| iMoviez | Italia |
| iCasting | Italia (tier gratuito) |
| CastingEProvini | Italia |
| Mandy | Europa |
| Backstage | Internazionale |
