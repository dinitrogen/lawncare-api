# Arduino Nano ESP32 Weather Display

Build a compact weather station display using an **Arduino Nano ESP32** and a **2.8" ILI9341 TFT LCD** that pulls live data from the Lawncare API.

---

## Table of Contents

1. [Hardware](#hardware)
2. [VSCode & PlatformIO Setup](#vscode--platformio-setup)
3. [Wiring](#wiring)
4. [Library Installation](#library-installation)
5. [Project Structure](#project-structure)
6. [Display Design](#display-design)
7. [API Integration](#api-integration)
8. [Complete Firmware](#complete-firmware)
9. [Build & Upload](#build--upload)
10. [Power & Enclosure Tips](#power--enclosure-tips)

---

## Hardware

| Component | Spec | Notes |
|---|---|---|
| Arduino Nano ESP32 | ABX00083 (Ublox NORA-W106, ESP32-S3) | Built-in Wi-Fi + BLE, USB-C |
| TFT LCD | 2.8" ILI9341, 320×240, SPI, with SD slot | Get an ILI9341 module with 4-wire SPI (not parallel). Common breakout boards: HiLetgo, ELEGOO |
| Jumper wires | Female-to-male, 8–10 pcs | For SPI + power connections |
| Micro-USB or USB-C cable | Data-capable | For programming and power |

> **Alternative displays:** The 2.4" ILI9341 (same driver, 320×240) works identically. For larger displays, a 3.5" ILI9488 (480×320) uses the same SPI wiring but requires the `TFT_eSPI` driver change to `ILI9488`.

---

## VSCode & PlatformIO Setup

### 1. Install PlatformIO Extension

1. Open VS Code → Extensions sidebar (`Ctrl+Shift+X`)
2. Search **PlatformIO IDE** → Install
3. Wait for PlatformIO Core to finish downloading (status bar shows progress)
4. Restart VS Code when prompted

### 2. Create a New Project

1. Click the PlatformIO icon in the sidebar (alien head) → **Home** → **New Project**
2. Configure:
   - **Name:** `lawncare-display`
   - **Board:** `Arduino Nano ESP32`
   - **Framework:** `Arduino`
3. Click **Finish** — PlatformIO scaffolds the project

### 3. Verify Board Detection

1. Connect the Nano ESP32 via USB-C
2. PlatformIO should auto-detect the COM port (check the status bar)
3. If not detected, install the USB-to-Serial driver:
   - **Windows:** The Nano ESP32 uses a native USB CDC. No extra driver needed on Windows 10+.
   - If you see "Unknown device," hold the **BOOT** button while plugging in, then release.

### 4. platformio.ini Configuration

Replace the generated `platformio.ini` with:

```ini
[env:arduino_nano_esp32]
platform = espressif32
board = arduino_nano_esp32
framework = arduino
monitor_speed = 115200
upload_speed = 921600

; Required for TFT_eSPI to find our custom config
build_flags =
    -DUSER_SETUP_LOADED=1
    -DILI9341_DRIVER=1
    -DTFT_MOSI=11
    -DTFT_SCLK=13
    -DTFT_CS=10
    -DTFT_DC=9
    -DTFT_RST=8
    -DSPI_FREQUENCY=40000000
    -DLOAD_GLCD=1
    -DLOAD_FONT2=1
    -DLOAD_FONT4=1
    -DLOAD_FONT6=1
    -DLOAD_FONT7=1
    -DLOAD_FONT8=1
    -DLOAD_GFXFF=1
    -DSMOOTH_FONT=1

lib_deps =
    bodmer/TFT_eSPI@^2.5.43
    bblanchon/ArduinoJson@^7.3.0
```

> **Why build_flags instead of User_Setup.h?** Defining the pins and driver in `build_flags` avoids needing to edit files inside the TFT_eSPI library folder, making the project self-contained and portable.

---

## Wiring

### Arduino Nano ESP32 → ILI9341 TFT (SPI)

| TFT Pin | Nano ESP32 Pin | GPIO | Notes |
|---|---|---|---|
| **VCC** | 3V3 | — | 3.3V supply (do NOT use 5V — the ILI9341 is 3.3V logic) |
| **GND** | GND | — | Common ground |
| **CS** | D10 | GPIO10 | Chip Select |
| **RESET** | D8 | GPIO8 | Hardware reset |
| **DC/RS** | D9 | GPIO9 | Data/Command select |
| **SDI/MOSI** | D11 | GPIO11 | SPI data in |
| **SCK** | D13 | GPIO13 | SPI clock |
| **LED** | 3V3 | — | Backlight (tie to 3.3V for always-on, or use a GPIO + transistor for brightness control) |
| **SDO/MISO** | D12 | GPIO12 | SPI data out (optional — only needed for reading from display) |

```
Arduino Nano ESP32          ILI9341 TFT
 ┌────────────────┐         ┌────────────┐
 │            3V3 ├────────►│ VCC        │
 │            GND ├────────►│ GND        │
 │            D10 ├────────►│ CS         │
 │             D8 ├────────►│ RESET      │
 │             D9 ├────────►│ DC         │
 │            D11 ├────────►│ MOSI       │
 │            D13 ├────────►│ SCK        │
 │            D12 ◄────────│ MISO       │
 │            3V3 ├────────►│ LED        │
 └────────────────┘         └────────────┘
```

> **Tip:** If the display doesn't light up, check the LED/backlight pin. Some modules have a separate backlight enable that needs to be pulled HIGH.

---

## Library Installation

PlatformIO handles this automatically via `lib_deps` in `platformio.ini`. The two required libraries are:

| Library | Purpose |
|---|---|
| **TFT_eSPI** | Hardware-accelerated TFT driver (SPI). Supports ILI9341 natively. |
| **ArduinoJson** | Parse JSON responses from the Lawncare API |

Both are downloaded on first build. No manual install needed.

---

## Project Structure

```
lawncare-display/
├── platformio.ini
├── src/
│   └── main.cpp           # All firmware code
├── include/
│   └── config.h            # Wi-Fi credentials & API URL
├── lib/                    # PlatformIO-managed libraries
└── test/
```

---

## Display Design

The 320×240 screen is divided into two alternating screens, cycled every 10 seconds (or on button press if you add one).

### Screen 1 — Current Conditions + Today's Forecast

```
┌──────────────────────────────────┐ 0
│  72°F         Partly Cloudy  ☁  │ ← Large temp (Font 7) + condition text + icon
│  Feels like 69°F                │ ← Smaller text (Font 2)
├──────────────────────────────────┤ 80
│  💧 53%    🌬 8 km/h   ☂ 0.0mm │ ← Humidity, wind, rain today
├──────────────────────────────────┤ 120
│  TODAY    High 78°  Low 61°     │ ← Forecast for today
│  ☁ Partly cloudy   💧 10%      │ ← Condition + precip probability
├──────────────────────────────────┤ 170
│  TOMORROW  High 81°  Low 63°   │ ← Tomorrow forecast
│  ☀ Mainly clear    💧 5%       │
├──────────────────────────────────┤ 220
│  🌱 Soil: 42%  38%  45%        │ ← Soil moisture channels
└──────────────────────────────────┘ 240
```

### Screen 2 — 5-Day Forecast

```
┌──────────────────────────────────┐ 0
│  5-DAY FORECAST                  │ ← Header (Font 4)
├──────────────────────────────────┤ 30
│  Mon   ☀  82° / 63°   💧  5%   │
│  Tue   ☁  78° / 61°   💧 20%   │
│  Wed   🌧  71° / 58°   💧 65%   │
│  Thu   ☁  74° / 59°   💧 15%   │
│  Fri   ☀  80° / 62°   💧  0%   │
├──────────────────────────────────┤ 200
│  UV: 6        Pressure: 1013hPa │ ← Current UV + barometric
│  Last updated: 2:35 PM          │
└──────────────────────────────────┘ 240
```

### Weather Icon Mapping (WMO → Bitmap)

The API returns Material icon names. Map them to simple TFT-drawn shapes:

| API Icon | WMO Codes | TFT Drawing |
|---|---|---|
| `wb_sunny` | 0 | Yellow filled circle + rays |
| `partly_cloudy_day` | 1, 2 | Half sun + cloud outline |
| `cloud` | 3 | Gray filled cloud |
| `foggy` | 45, 48 | Horizontal dashed lines |
| `grain` | 51–57 | Cloud + small dots |
| `rainy` | 61, 63, 80, 81 | Cloud + vertical lines |
| `thunderstorm` | 65, 82, 95–99 | Cloud + zigzag bolt |
| `ac_unit` | 66, 67 | Snowflake (asterisk) |
| `cloudy_snowing` | 71–86 | Cloud + asterisks |

---

## API Integration

The display consumes two unauthenticated endpoints. No API key or JWT token required.

### Endpoints

| Endpoint | Purpose | Refresh Interval |
|---|---|---|
| `GET /api/weather/display` | Current conditions (Ecowitt sensor data) | Every 60 seconds |
| `GET /api/weather/forecast?lat={LAT}&lon={LON}` | 7-day forecast (Open-Meteo, cached 60 min) | Every 30 minutes |

### `/api/weather/display` Response

```json
{
  "ts": "2025-07-15T14:30:00Z",
  "tempC": 22.3,
  "feelsLikeC": 20.8,
  "humidityPct": 53,
  "windKmh": 8.2,
  "windGustKmh": 14.5,
  "windDir": 225,
  "rainTodayMm": 0.0,
  "rainRateMmh": 0.0,
  "pressureHpa": 1013.2,
  "uvIndex": 6,
  "soilMoisturePct": [42, 38, 45]
}
```

### `/api/weather/forecast` Response

```json
{
  "today": {
    "date": "2025-07-15",
    "tempMaxF": 82.4,
    "tempMinF": 63.1,
    "weatherCode": 2,
    "condition": "Partly cloudy",
    "icon": "partly_cloudy_day",
    "precipitationProbabilityPct": 10,
    "precipitationMm": 0.0,
    "windMaxKmh": 15.3
  },
  "daily": [ /* array of 7 DailyForecast objects */ ],
  "cachedAt": "2025-07-15T14:00:00Z"
}
```

> **Temperature units:** The display endpoint returns Celsius. The forecast endpoint returns Fahrenheit. The firmware converts as needed for display.

---

## Complete Firmware

### `include/config.h`

```cpp
#pragma once

// ── Wi-Fi ───────────────────────────────────────────────────────────────
#define WIFI_SSID     "YourNetworkName"
#define WIFI_PASSWORD "YourPassword"

// ── Lawncare API ────────────────────────────────────────────────────────
#define API_BASE_URL  "https://lawncare-7fa77.web.app"
#define API_DISPLAY   "/api/weather/display"
#define API_FORECAST  "/api/weather/forecast"

// ── Location (for forecast endpoint) ────────────────────────────────────
#define LATITUDE      40.00
#define LONGITUDE    -83.00

// ── Timing (milliseconds) ───────────────────────────────────────────────
#define CURRENT_REFRESH_MS    60000    // Refresh current conditions every 60s
#define FORECAST_REFRESH_MS  1800000   // Refresh forecast every 30 min
#define SCREEN_CYCLE_MS       10000    // Switch screens every 10s
```

### `src/main.cpp`

```cpp
#include <Arduino.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <TFT_eSPI.h>
#include "config.h"

// ── Display ─────────────────────────────────────────────────────────────
TFT_eSPI tft = TFT_eSPI();

// ── Color palette ───────────────────────────────────────────────────────
#define BG_COLOR     TFT_BLACK
#define TEXT_COLOR   TFT_WHITE
#define ACCENT_COLOR 0x2E04  // ~#2e7d32 green in RGB565
#define TEMP_COLOR   0xFD20  // warm orange
#define RAIN_COLOR   0x5D7F  // light blue
#define DIVIDER_CLR  0x4208  // dark gray

// ── State ───────────────────────────────────────────────────────────────
struct CurrentWeather {
  double tempC       = 0;
  double feelsLikeC  = 0;
  int    humidityPct = 0;
  double windKmh     = 0;
  double windGustKmh = 0;
  int    windDir     = 0;
  double rainTodayMm = 0;
  double pressureHpa = 0;
  int    uvIndex     = 0;
  int    soilMoisture[4] = {0};
  int    soilChannels = 0;
  bool   valid       = false;
};

struct ForecastDay {
  char   date[11]     = {0};  // "2025-07-15"
  double tempMaxF     = 0;
  double tempMinF     = 0;
  int    weatherCode  = 0;
  char   condition[24]= {0};
  char   icon[24]     = {0};
  int    precipPct    = 0;
};

struct ForecastData {
  ForecastDay today;
  ForecastDay daily[7];
  int         dayCount = 0;
  bool        valid    = false;
};

CurrentWeather current;
ForecastData   forecast;

unsigned long lastCurrentFetch  = 0;
unsigned long lastForecastFetch = 0;
unsigned long lastScreenSwitch  = 0;
int           currentScreen     = 0;

// ── Helpers ─────────────────────────────────────────────────────────────

double cToF(double c) { return c * 9.0 / 5.0 + 32.0; }

void drawDivider(int y) {
  tft.drawFastHLine(4, y, 312, DIVIDER_CLR);
}

/// Draw a simple weather icon at (x,y) based on the WMO code.
void drawWeatherIcon(int x, int y, int code) {
  if (code == 0) {
    // Sun
    tft.fillCircle(x, y, 10, TFT_YELLOW);
    for (int a = 0; a < 360; a += 45) {
      int x2 = x + 15 * cos(a * DEG_TO_RAD);
      int y2 = y + 15 * sin(a * DEG_TO_RAD);
      tft.drawLine(x + 12*cos(a*DEG_TO_RAD), y + 12*sin(a*DEG_TO_RAD), x2, y2, TFT_YELLOW);
    }
  } else if (code <= 2) {
    // Partly cloudy: small sun + cloud
    tft.fillCircle(x - 5, y - 5, 7, TFT_YELLOW);
    tft.fillRoundRect(x - 8, y - 2, 22, 12, 5, TFT_LIGHTGREY);
  } else if (code == 3) {
    // Overcast
    tft.fillRoundRect(x - 10, y - 5, 22, 12, 5, TFT_LIGHTGREY);
  } else if (code == 45 || code == 48) {
    // Fog: horizontal dashes
    for (int i = 0; i < 4; i++) {
      tft.drawFastHLine(x - 10, y - 4 + i*4, 20, TFT_LIGHTGREY);
    }
  } else if (code >= 51 && code <= 57) {
    // Drizzle
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, TFT_LIGHTGREY);
    for (int i = 0; i < 3; i++) tft.fillCircle(x - 6 + i*6, y + 8, 1, RAIN_COLOR);
  } else if ((code >= 61 && code <= 65) || (code >= 80 && code <= 82)) {
    // Rain
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, TFT_DARKGREY);
    for (int i = 0; i < 3; i++) tft.drawLine(x - 6 + i*6, y + 4, x - 8 + i*6, y + 10, RAIN_COLOR);
  } else if (code == 66 || code == 67) {
    // Freezing rain
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, TFT_DARKGREY);
    for (int i = 0; i < 3; i++) tft.drawLine(x - 6 + i*6, y + 4, x - 8 + i*6, y + 10, TFT_CYAN);
  } else if (code >= 71 && code <= 86) {
    // Snow
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, TFT_LIGHTGREY);
    for (int i = 0; i < 3; i++) {
      int cx = x - 6 + i*6;
      tft.drawLine(cx-3, y+7, cx+3, y+7, TFT_WHITE);
      tft.drawLine(cx, y+4, cx, y+10, TFT_WHITE);
    }
  } else if (code >= 95) {
    // Thunderstorm
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, TFT_DARKGREY);
    // Lightning bolt
    tft.drawLine(x, y + 4, x - 3, y + 9, TFT_YELLOW);
    tft.drawLine(x - 3, y + 9, x + 1, y + 9, TFT_YELLOW);
    tft.drawLine(x + 1, y + 9, x - 2, y + 14, TFT_YELLOW);
  }
}

// ── Wi-Fi ───────────────────────────────────────────────────────────────

void connectWiFi() {
  tft.fillScreen(BG_COLOR);
  tft.setTextColor(TEXT_COLOR, BG_COLOR);
  tft.setTextDatum(MC_DATUM);
  tft.drawString("Connecting to WiFi...", 160, 110, 4);

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(500);
    tft.drawString(".", 100 + attempts * 6, 140, 2);
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    tft.fillScreen(BG_COLOR);
    tft.drawString("Connected!", 160, 110, 4);
    tft.drawString(WiFi.localIP().toString().c_str(), 160, 140, 2);
    delay(1000);
  } else {
    tft.fillScreen(TFT_RED);
    tft.drawString("WiFi Failed!", 160, 120, 4);
    delay(3000);
    ESP.restart();
  }
}

// ── API Fetch ───────────────────────────────────────────────────────────

bool fetchCurrentWeather() {
  if (WiFi.status() != WL_CONNECTED) return false;

  HTTPClient http;
  String url = String(API_BASE_URL) + API_DISPLAY;
  http.begin(url);
  int code = http.GET();

  if (code != 200) {
    http.end();
    return false;
  }

  JsonDocument doc;
  DeserializationError err = deserializeJson(doc, http.getStream());
  http.end();
  if (err) return false;

  current.tempC       = doc["tempC"]       | 0.0;
  current.feelsLikeC  = doc["feelsLikeC"]  | 0.0;
  current.humidityPct = doc["humidityPct"] | 0;
  current.windKmh     = doc["windKmh"]     | 0.0;
  current.windGustKmh = doc["windGustKmh"] | 0.0;
  current.windDir     = doc["windDir"]     | 0;
  current.rainTodayMm = doc["rainTodayMm"] | 0.0;
  current.pressureHpa = doc["pressureHpa"] | 0.0;
  current.uvIndex     = doc["uvIndex"]     | 0;

  JsonArray soil = doc["soilMoisturePct"].as<JsonArray>();
  current.soilChannels = 0;
  for (JsonVariant v : soil) {
    if (current.soilChannels < 4) {
      current.soilMoisture[current.soilChannels++] = v.as<int>();
    }
  }

  current.valid = true;
  return true;
}

bool fetchForecast() {
  if (WiFi.status() != WL_CONNECTED) return false;

  HTTPClient http;
  String url = String(API_BASE_URL) + API_FORECAST
             + "?lat=" + String(LATITUDE, 4)
             + "&lon=" + String(LONGITUDE, 4);
  http.begin(url);
  int code = http.GET();

  if (code != 200) {
    http.end();
    return false;
  }

  JsonDocument doc;
  DeserializationError err = deserializeJson(doc, http.getStream());
  http.end();
  if (err) return false;

  // Today
  JsonObject t = doc["today"];
  strlcpy(forecast.today.date,      t["date"] | "",      sizeof(forecast.today.date));
  forecast.today.tempMaxF    = t["tempMaxF"]    | 0.0;
  forecast.today.tempMinF    = t["tempMinF"]    | 0.0;
  forecast.today.weatherCode = t["weatherCode"] | 0;
  strlcpy(forecast.today.condition,  t["condition"] | "",  sizeof(forecast.today.condition));
  strlcpy(forecast.today.icon,       t["icon"] | "",       sizeof(forecast.today.icon));
  forecast.today.precipPct   = t["precipitationProbabilityPct"] | 0;

  // Daily array
  JsonArray arr = doc["daily"].as<JsonArray>();
  forecast.dayCount = 0;
  for (JsonObject day : arr) {
    if (forecast.dayCount >= 7) break;
    ForecastDay& d = forecast.daily[forecast.dayCount];
    strlcpy(d.date,      day["date"] | "",      sizeof(d.date));
    d.tempMaxF    = day["tempMaxF"]    | 0.0;
    d.tempMinF    = day["tempMinF"]    | 0.0;
    d.weatherCode = day["weatherCode"] | 0;
    strlcpy(d.condition,  day["condition"] | "",  sizeof(d.condition));
    strlcpy(d.icon,       day["icon"] | "",       sizeof(d.icon));
    d.precipPct   = day["precipitationProbabilityPct"] | 0;
    forecast.dayCount++;
  }

  forecast.valid = true;
  return true;
}

// ── Screen Drawing ──────────────────────────────────────────────────────

void drawScreen1() {
  tft.fillScreen(BG_COLOR);

  if (!current.valid) {
    tft.setTextDatum(MC_DATUM);
    tft.drawString("Waiting for data...", 160, 120, 4);
    return;
  }

  // ── Row 1: Large temperature + condition ──────────────────────────────
  int tempF = (int)round(cToF(current.tempC));
  char tempStr[8];
  snprintf(tempStr, sizeof(tempStr), "%d", tempF);

  tft.setTextColor(TEMP_COLOR, BG_COLOR);
  tft.setTextDatum(TL_DATUM);
  tft.drawString(tempStr, 10, 8, 7);  // Large font

  // Degree symbol + F
  int tw = tft.textWidth(tempStr, 7);
  tft.setTextColor(TEXT_COLOR, BG_COLOR);
  tft.drawString("F", 10 + tw + 4, 12, 4);

  // Condition text (right-aligned)
  if (forecast.valid) {
    tft.setTextDatum(TR_DATUM);
    tft.setTextColor(TEXT_COLOR, BG_COLOR);
    tft.drawString(forecast.today.condition, 290, 12, 2);
    drawWeatherIcon(305, 18, forecast.today.weatherCode);
  }

  // Feels like
  int feelsF = (int)round(cToF(current.feelsLikeC));
  char feelsStr[32];
  snprintf(feelsStr, sizeof(feelsStr), "Feels like %dF", feelsF);
  tft.setTextDatum(TL_DATUM);
  tft.setTextColor(TFT_LIGHTGREY, BG_COLOR);
  tft.drawString(feelsStr, 10, 62, 2);

  drawDivider(80);

  // ── Row 2: Humidity, Wind, Rain ───────────────────────────────────────
  tft.setTextColor(TEXT_COLOR, BG_COLOR);
  tft.setTextDatum(TL_DATUM);

  char hum[16]; snprintf(hum, sizeof(hum), "%d%%", current.humidityPct);
  char wind[16]; snprintf(wind, sizeof(wind), "%.0f km/h", current.windKmh);
  char rain[16]; snprintf(rain, sizeof(rain), "%.1fmm", current.rainTodayMm);

  tft.setTextColor(RAIN_COLOR, BG_COLOR);
  tft.drawString(hum, 20, 88, 2);
  tft.setTextColor(TEXT_COLOR, BG_COLOR);
  tft.drawString(wind, 110, 88, 2);
  tft.setTextColor(RAIN_COLOR, BG_COLOR);
  tft.drawString(rain, 220, 88, 2);

  // Labels
  tft.setTextColor(TFT_DARKGREY, BG_COLOR);
  tft.drawString("Humidity", 20, 106, 1);
  tft.drawString("Wind", 110, 106, 1);
  tft.drawString("Rain", 220, 106, 1);

  drawDivider(120);

  // ── Row 3: Today's forecast ───────────────────────────────────────────
  if (forecast.valid) {
    tft.setTextColor(ACCENT_COLOR, BG_COLOR);
    tft.drawString("TODAY", 10, 126, 2);
    tft.setTextColor(TEXT_COLOR, BG_COLOR);

    char todayHL[32];
    snprintf(todayHL, sizeof(todayHL), "Hi %.0fF  Lo %.0fF",
             forecast.today.tempMaxF, forecast.today.tempMinF);
    tft.drawString(todayHL, 75, 126, 2);

    drawWeatherIcon(20, 150, forecast.today.weatherCode);
    char todayCond[40];
    snprintf(todayCond, sizeof(todayCond), "%s  %d%%",
             forecast.today.condition, forecast.today.precipPct);
    tft.drawString(todayCond, 40, 146, 2);
  }

  drawDivider(168);

  // ── Row 4: Tomorrow's forecast ────────────────────────────────────────
  if (forecast.valid && forecast.dayCount >= 2) {
    ForecastDay& tmrw = forecast.daily[1];
    tft.setTextColor(ACCENT_COLOR, BG_COLOR);
    tft.drawString("TOMORROW", 10, 174, 2);
    tft.setTextColor(TEXT_COLOR, BG_COLOR);

    char tmHL[32];
    snprintf(tmHL, sizeof(tmHL), "Hi %.0fF  Lo %.0fF", tmrw.tempMaxF, tmrw.tempMinF);
    tft.drawString(tmHL, 100, 174, 2);

    drawWeatherIcon(20, 198, tmrw.weatherCode);
    char tmCond[40];
    snprintf(tmCond, sizeof(tmCond), "%s  %d%%", tmrw.condition, tmrw.precipPct);
    tft.drawString(tmCond, 40, 194, 2);
  }

  drawDivider(216);

  // ── Row 5: Soil moisture ──────────────────────────────────────────────
  if (current.soilChannels > 0) {
    tft.setTextColor(ACCENT_COLOR, BG_COLOR);
    tft.drawString("SOIL", 10, 224, 2);
    tft.setTextColor(TEXT_COLOR, BG_COLOR);
    char soil[48] = {0};
    int offset = 0;
    for (int i = 0; i < current.soilChannels && i < 4; i++) {
      offset += snprintf(soil + offset, sizeof(soil) - offset,
                         "%s%d%%", i > 0 ? "  " : "", current.soilMoisture[i]);
    }
    tft.drawString(soil, 60, 224, 2);
  }
}

void drawScreen2() {
  tft.fillScreen(BG_COLOR);

  // ── Header ────────────────────────────────────────────────────────────
  tft.setTextColor(ACCENT_COLOR, BG_COLOR);
  tft.setTextDatum(TL_DATUM);
  tft.drawString("5-DAY FORECAST", 10, 6, 4);

  drawDivider(30);

  if (!forecast.valid || forecast.dayCount < 2) {
    tft.setTextColor(TEXT_COLOR, BG_COLOR);
    tft.setTextDatum(MC_DATUM);
    tft.drawString("No forecast data", 160, 120, 2);
    return;
  }

  // ── Forecast rows (skip today = index 0, show 1..5) ──────────────────
  int y = 38;
  int shown = 0;
  for (int i = 1; i < forecast.dayCount && shown < 5; i++, shown++) {
    ForecastDay& d = forecast.daily[i];

    // Day abbreviation from date string (parse "YYYY-MM-DD")
    struct tm tm = {0};
    sscanf(d.date, "%d-%d-%d", &tm.tm_year, &tm.tm_mon, &tm.tm_mday);
    tm.tm_year -= 1900;
    tm.tm_mon  -= 1;
    mktime(&tm);
    const char* days[] = {"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};
    const char* dayName = days[tm.tm_wday];

    tft.setTextColor(TEXT_COLOR, BG_COLOR);
    tft.setTextDatum(TL_DATUM);
    tft.drawString(dayName, 10, y, 2);

    drawWeatherIcon(60, y + 7, d.weatherCode);

    char hl[24];
    snprintf(hl, sizeof(hl), "%.0f / %.0fF", d.tempMaxF, d.tempMinF);
    tft.drawString(hl, 85, y, 2);

    char pr[8];
    snprintf(pr, sizeof(pr), "%d%%", d.precipPct);
    tft.setTextColor(RAIN_COLOR, BG_COLOR);
    tft.drawString(pr, 270, y, 2);

    // Condition on second line
    tft.setTextColor(TFT_DARKGREY, BG_COLOR);
    tft.drawString(d.condition, 85, y + 18, 1);

    y += 34;
  }

  drawDivider(y + 2);

  // ── Bottom bar: UV + Pressure ─────────────────────────────────────────
  if (current.valid) {
    tft.setTextColor(TEXT_COLOR, BG_COLOR);
    char bottom[48];
    snprintf(bottom, sizeof(bottom), "UV: %d    Pressure: %.0f hPa",
             current.uvIndex, current.pressureHpa);
    tft.drawString(bottom, 10, y + 8, 2);
  }
}

// ── Main ────────────────────────────────────────────────────────────────

void setup() {
  Serial.begin(115200);
  delay(100);

  tft.init();
  tft.setRotation(1);  // Landscape
  tft.fillScreen(BG_COLOR);

  connectWiFi();

  // Initial data fetch
  fetchCurrentWeather();
  fetchForecast();
  drawScreen1();

  lastCurrentFetch  = millis();
  lastForecastFetch = millis();
  lastScreenSwitch  = millis();
}

void loop() {
  unsigned long now = millis();

  // Refresh current conditions
  if (now - lastCurrentFetch >= CURRENT_REFRESH_MS) {
    fetchCurrentWeather();
    lastCurrentFetch = now;
    // Redraw current screen with new data
    if (currentScreen == 0) drawScreen1();
    else drawScreen2();
  }

  // Refresh forecast
  if (now - lastForecastFetch >= FORECAST_REFRESH_MS) {
    fetchForecast();
    lastForecastFetch = now;
    if (currentScreen == 0) drawScreen1();
    else drawScreen2();
  }

  // Cycle screens
  if (now - lastScreenSwitch >= SCREEN_CYCLE_MS) {
    currentScreen = 1 - currentScreen;
    if (currentScreen == 0) drawScreen1();
    else drawScreen2();
    lastScreenSwitch = now;
  }

  delay(100);  // Yield
}
```

---

## Build & Upload

1. **Build:** Click the checkmark (✓) in the PlatformIO toolbar, or `Ctrl+Alt+B`
2. **Upload:** Click the arrow (→), or `Ctrl+Alt+U`
3. **Monitor:** Click the plug icon (🔌), or `Ctrl+Alt+S` to open serial monitor at 115200 baud

### Troubleshooting

| Symptom | Fix |
|---|---|
| **White screen** | Check wiring, especially DC and RST pins. Verify `TFT_DC` and `TFT_RST` in build_flags match your wiring. |
| **Garbled display** | SPI frequency too high. Lower `SPI_FREQUENCY` to `27000000`. |
| **WiFi won't connect** | Check SSID/password in `config.h`. Ensure 2.4 GHz network (ESP32-S3 doesn't support 5 GHz). |
| **API returns 404** | Verify the Ecowitt relay is running and has posted at least one reading. |
| **JSON parse error** | Increase ArduinoJson capacity or check API response with `Serial.println(http.getString())`. |
| **Upload fails** | Hold **BOOT** button, click **Upload**, release BOOT after "Connecting..." appears. |
| **Port not found** | Windows: Check Device Manager for COM port. Try a different USB-C cable (must be data-capable). |

---

## Power & Enclosure Tips

- **USB power:** 5V/500mA from any USB adapter is sufficient.
- **Low-power mode:** Between refresh intervals you can put the ESP32-S3 into light sleep. Replace `delay(100)` in `loop()` with `esp_sleep_enable_timer_wakeup()` for significant power savings.
- **Enclosure:** A 3D-printed case works well. The display typically mounts with M2.5 screws. Leave ventilation holes near the ESP32.
- **Backlight dimming:** Connect the TFT LED pin to a GPIO (e.g., D6) and use `analogWrite()` / `ledcWrite()` to dim at night.
- **OTA updates:** Add `ArduinoOTA` to `setup()` for wireless firmware updates without re-plugging USB.
