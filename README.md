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
- Docker + Docker Compose (opzionale, per la produzione)

## Avvio rapido (sviluppo)

### 1. Clona e ripristina dipendenze

```bash
git clone <repo>
cd actoring-app
dotnet restore CastingRadar.slnx
```

### 2. Avvia il backend

```bash
cd src/CastingRadar.Api
dotnet run
# API disponibile su http://localhost:5000
# Il database SQLite viene creato automaticamente
```

### 3. Avvia il frontend (in un altro terminale)

```bash
cd frontend
npm install
npm run dev
# App disponibile su http://localhost:5173
```

Il frontend proxya le chiamate `/api/*` verso `http://localhost:5000`.

## Configurazione Telegram

1. Crea un bot con [@BotFather](https://t.me/BotFather) su Telegram → ottieni il **Bot Token**
2. Invia un messaggio al bot, poi usa [@userinfobot](https://t.me/userinfobot) per ottenere il tuo **Chat ID**
3. Aggiorna `src/CastingRadar.Api/appsettings.json`:

```json
{
  "CastingRadar": {
    "Telegram": {
      "BotToken": "123456:ABC-DEF...",
      "ChatId": "123456789"
    }
  }
}
```

Oppure aggiorna il profilo dalla pagina **Impostazioni** nell'app.

## Build frontend per produzione

```bash
cd frontend
npm run build
# Output in src/CastingRadar.Api/wwwroot/
```

## Produzione con Docker Compose

```bash
# Crea un file .env
echo "TELEGRAM_BOT_TOKEN=tuo_token" > .env
echo "TELEGRAM_CHAT_ID=tuo_chat_id" >> .env
echo "DB_PASSWORD=password_sicura" >> .env

docker compose up -d
# API su http://localhost:5000
```

## API endpoints

| Metodo | Path | Descrizione |
|--------|------|-------------|
| GET | `/api/castings` | Lista casting (filtri: keywords, types, regions, onlyPaid, gender) |
| GET | `/api/castings/{id}` | Dettaglio casting |
| POST | `/api/castings/{id}/favorite` | Toggle preferito |
| POST | `/api/castings/{id}/applied` | Segna come candidata |
| GET | `/api/sources` | Stato delle fonti |
| POST | `/api/sources/scrape-all` | Trigger scraping manuale |
| POST | `/api/sources/{name}/scrape` | Scraping di una singola fonte |
| GET | `/api/profile` | Profilo utente |
| PUT | `/api/profile` | Aggiorna preferenze |
| GET | `/api/stats` | Statistiche |
| GET | `/health` | Health check |

## Test

```bash
dotnet test CastingRadar.slnx
```

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
