# lawncare-api

A minimal **ASP.NET Core 8** REST API that:
- **Ingests real-time sensor data** pushed by an Ecowitt **GW1100** gateway (with **WH51** soil-moisture meters and **WN32** outdoor temperature/humidity sensor)
- **Serves weather data** to an ESP-controlled display (compact `/api/weather/display` endpoint) and an Angular PWA
- **Manages lawn care tasks** (full CRUD) for the Angular PWA
- **Persists all data** in Google Cloud Firestore (Firebase)

---

## Project structure

```
LawncareApi/
├── Controllers/
│   ├── WeatherController.cs   # Ecowitt ingestion + weather read endpoints
│   └── TasksController.cs     # Lawn care task CRUD
├── Models/
│   ├── EcowittReading.cs      # Raw Ecowitt push payload (form-encoded)
│   ├── WeatherReading.cs      # Normalised metric reading (stored in Firestore)
│   └── LawnCareTask.cs        # Lawn care task model + request DTO
├── Services/
│   ├── IWeatherService.cs / WeatherService.cs
│   ├── ILawnCareTaskService.cs / LawnCareTaskService.cs
│   └── EcowittMapper.cs       # Converts Ecowitt imperial → metric
├── Program.cs
└── appsettings.json
LawncareApi.Tests/             # xUnit unit tests (in-memory stubs, no Firestore needed)
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0 |
| Firebase project | any – Firestore must be enabled |
| Google Application Default Credentials | set before running |

---

## Configuration

Edit `LawncareApi/appsettings.json` (or set environment variables):

```json
{
  "Firestore": {
    "ProjectId": "your-firebase-project-id"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://your-app.web.app"
    ]
  }
}
```

**Authentication:** The API uses [Application Default Credentials](https://cloud.google.com/docs/authentication/application-default-credentials).  
For local development: `gcloud auth application-default login`  
For Cloud Run / Firebase Hosting rewrites: attach a service account with `roles/datastore.user`.

---

## Running locally

```bash
cd LawncareApi
dotnet run
# Swagger UI → https://localhost:{port}/swagger
```

---

## API endpoints

### Weather

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/weather/ecowitt` | Receive Ecowitt GW1100 Custom Server push (form-encoded) |
| `GET`  | `/api/weather/current` | Latest full weather reading |
| `GET`  | `/api/weather/display` | Condensed snapshot for ESP display |
| `GET`  | `/api/weather/history?from=&to=&limit=` | Historical readings (UTC, newest first) |

#### Ecowitt gateway setup

In the Ecowitt **WS View / WS Tool** app → *Others* → *Customized*:
- Protocol: **Ecowitt** or **Wunderground** (Ecowitt form-encoded is the default)
- Server IP / hostname: your API host
- Path: `/api/weather/ecowitt`
- Port & interval: as desired

### Lawn care tasks

| Method | Path | Description |
|--------|------|-------------|
| `GET`    | `/api/tasks`      | List all tasks |
| `POST`   | `/api/tasks`      | Create a task |
| `GET`    | `/api/tasks/{id}` | Get task by ID |
| `PUT`    | `/api/tasks/{id}` | Update task |
| `DELETE` | `/api/tasks/{id}` | Delete task |

---

## Running tests

```bash
dotnet test LawncareApi.slnx
```

Tests use in-memory service stubs — no Firestore or network access required.