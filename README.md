# Lawncare API

ASP.NET Core 8 REST API for the Lawncare PWA. Manages lawn care data (zones, products, treatments, equipment, soil tests, GDD tracking, seasonal plans) and ingests weather data from an Ecowitt GW1100 gateway for an ESP32 display.

All data is persisted in Google Cloud Firestore. Authenticated endpoints use Firebase JWT tokens.

## Architecture

```
Angular PWA ──► Firebase Hosting ──► /api/** rewrite ──► Cloud Run (.NET API) ──► Firestore
Ecowitt GW1100 ──► POST /api/weather/ecowitt ──────────► Cloud Run (.NET API) ──► Firestore
ESP32 Display  ──► GET  /api/weather/display  ──────────► Cloud Run (.NET API) ──► Firestore
```

## Project Structure

```
LawncareApi/
├── Controllers/         # 9 controllers, 35 endpoints
├── Models/              # Firestore models + request DTOs
├── Services/            # Business logic + Firestore access
├── Extensions/          # ClaimsPrincipal helpers
├── Program.cs           # DI, auth, middleware
├── appsettings.json     # Config (project ID, CORS origins)
└── firebase-service-account.json  # Local dev only (gitignored)
LawncareApi.Tests/       # xUnit tests (in-memory, no Firestore needed)
Dockerfile               # Multi-stage build for Cloud Run
```

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Firebase project | Firestore enabled |
| Google Cloud CLI | For deployment |

## Local Development

1. **Get a Firebase service account key:**
   - Firebase Console → Project Settings → Service accounts → Generate new private key
   - Save as `LawncareApi/firebase-service-account.json`

2. **Run the API:**
   ```bash
   cd LawncareApi
   dotnet run
   ```
   Swagger UI: http://localhost:5021/swagger

## API Endpoints

### Weather (no auth — Ecowitt/ESP32 compatible)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/weather/ecowitt` | Receive Ecowitt GW1100 sensor push |
| GET | `/api/weather/current` | Latest weather reading |
| GET | `/api/weather/display` | Condensed snapshot for ESP32 |
| GET | `/api/weather/forecast?lat=&lon=` | 7-day forecast (NWS, cached 60 min) |
| GET | `/api/weather/history?from=&to=&limit=` | Historical readings |

### User (auth required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/user` | Get profile |
| POST | `/api/user` | Create profile |
| PUT | `/api/user` | Update profile |

### Zones, Products, Treatments, Equipment, Soil Tests (auth required)

Standard CRUD on `/api/zones`, `/api/products`, `/api/treatments`, `/api/equipment`, `/api/soiltests`.

Equipment also has `/api/equipment/logs` for maintenance logs.

### GDD and Seasonal (auth required)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/gdd` | Calculate GDD from user profile settings |
| GET | `/api/seasonal/{year}` | Get seasonal task statuses |
| PUT | `/api/seasonal` | Save seasonal task statuses |

### Ecowitt Gateway Setup

In the WS View / WS Tool app → Others → Customized:
- Protocol: Ecowitt
- Server: your Cloud Run URL or Firebase Hosting domain
- Path: `/api/weather/ecowitt`

## Running Tests

```bash
dotnet test LawncareApi.Tests/LawncareApi.Tests.csproj
```

## Deploying to Cloud Run

```bash
gcloud config set project lawncare-7fa77
gcloud run deploy lawncare-api --source . --region us-central1 --allow-unauthenticated --set-env-vars "Firestore__ProjectId=lawncare-7fa77"
```

On Cloud Run, Firestore credentials are automatic via the service account — no key file needed.
