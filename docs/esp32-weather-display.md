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

> **Alternative displays:** The 2.4" ILI9341 (same driver, 320×240) works identically.

> **⚠ Important: Do NOT use TFT_eSPI on this board.** The Arduino Nano ESP32 uses a pin remap layer (`BOARD_HAS_PIN_REMAP`) that translates Arduino D-pin numbers to raw ESP32-S3 GPIOs. TFT_eSPI mixes Arduino SPI calls (which go through the remap) with raw ESP-IDF GPIO calls (which bypass it), causing pin mismatches that crash the firmware and can brick USB. Use **Adafruit_ILI9341 + Adafruit_GFX** instead — they use only standard Arduino SPI, so the remap works correctly.

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
upload_protocol = esptool

; Pin definitions (Arduino D-pin numbers, remap layer handles GPIO translation)
build_flags =
    -DTFT_CS=10
    -DTFT_DC=9
    -DTFT_RST=8

lib_deps =
    adafruit/Adafruit ILI9341@^1.6.1
    adafruit/Adafruit GFX Library@^1.11.11
    bblanchon/ArduinoJson@^7.3.0
```

> **Why Adafruit_ILI9341 instead of TFT_eSPI?** The Arduino Nano ESP32 has a pin remap layer that translates D-pin numbers to raw GPIOs. TFT_eSPI bypasses this with raw ESP-IDF calls, causing pin mismatches and firmware crashes. Adafruit_ILI9341 uses only standard Arduino SPI and works correctly.

> **Why `upload_protocol = esptool`?** The default DFU upload protocol requires a WinUSB driver installed via Zadig. The `esptool` protocol works out of the box on Windows.

---

## Wiring

### Arduino Nano ESP32 → ILI9341 TFT (SPI)

| TFT Pin | Nano ESP32 Pin | GPIO | Notes |
|---|---|---|---|
| **VCC** | 3V3 | — | 3.3V supply (do NOT use 5V — the ILI9341 is 3.3V logic) |
| **GND** | GND | — | Common ground |
| **CS** | D10 | GPIO21 | Chip Select |
| **RESET** | D8 | GPIO17 | Hardware reset |
| **DC/RS** | D9 | GPIO18 | Data/Command select |
| **SDI/MOSI** | D11 | GPIO38 | SPI data in |
| **SCK** | D13 | GPIO48 | SPI clock |
| **LED** | 3V3 | — | Backlight (tie to 3.3V for always-on, or use a GPIO + transistor for brightness control) |
| **SDO/MISO** | D12 | GPIO47 | SPI data out (optional — only needed for reading from display) |

> **Pin remap note:** The Arduino Nano ESP32 remaps D-pin numbers to different raw GPIOs. In firmware, always use D-pin numbers (D8, D9, D10, etc.) — the Arduino core translates them automatically. The GPIO column above is for reference only.

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
| **Adafruit ILI9341** | ILI9341 TFT driver using standard Arduino SPI. Compatible with Nano ESP32 pin remap. |
| **Adafruit GFX** | Graphics primitives (text, shapes, colors). Required by Adafruit ILI9341. |
| **ArduinoJson** | Parse JSON responses from the Lawncare API |

All are downloaded automatically on first build via `lib_deps`. No manual install needed.

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
| `GET /api/weather/forecast?lat={LAT}&lon={LON}` | 7-day forecast (NWS, cached 60 min) | Every 30 minutes |

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

> **Temperature units:** The display endpoint returns Celsius. The forecast endpoint returns Fahrenheit (sourced from the National Weather Service API). The firmware converts as needed for display.

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
#include <SPI.h>
#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Adafruit_GFX.h>
#include <Adafruit_ILI9341.h>
#include "config.h"

// ── Display ─────────────────────────────────────────────────────────────
Adafruit_ILI9341 tft(TFT_CS, TFT_DC, TFT_RST);

// ── Color palette (RGB565) ──────────────────────────────────────────────────
#define BG_COLOR     ILI9341_BLACK
#define TEXT_COLOR   ILI9341_WHITE
#define ACCENT_COLOR 0x2E04  // ~#2e7d32 green
#define TEMP_COLOR   0xFD20  // warm orange
#define RAIN_COLOR   0x5D7F  // light blue
#define DIVIDER_CLR  0x4208  // dark gray
#define GREY_LIGHT   0xC618  // light grey
#define GREY_DARK    0x7BEF  // dark grey

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

// Print text at (x,y) with given size and color
void drawText(const char* text, int x, int y, uint8_t sz, uint16_t color) {
  tft.setTextSize(sz);
  tft.setTextColor(color, BG_COLOR);
  tft.setCursor(x, y);
  tft.print(text);
}

// Print right-aligned text
void drawTextRight(const char* text, int rightX, int y, uint8_t sz, uint16_t color) {
  tft.setTextSize(sz);
  int16_t x1, y1; uint16_t w, h;
  tft.getTextBounds(text, 0, 0, &x1, &y1, &w, &h);
  tft.setTextColor(color, BG_COLOR);
  tft.setCursor(rightX - w, y);
  tft.print(text);
}

// Get pixel width of text at given size
int textWidth(const char* text, uint8_t sz) {
  tft.setTextSize(sz);
  int16_t x1, y1; uint16_t w, h;
  tft.getTextBounds(text, 0, 0, &x1, &y1, &w, &h);
  return w;
}

/// Draw a simple weather icon at (x,y) based on the WMO code.
void drawWeatherIcon(int x, int y, int code) {
  if (code == 0) {
    tft.fillCircle(x, y, 10, ILI9341_YELLOW);
    for (int a = 0; a < 360; a += 45) {
      float rad = a * DEG_TO_RAD;
      int x2 = x + 15 * cos(rad);
      int y2 = y + 15 * sin(rad);
      tft.drawLine(x + 12*cos(rad), y + 12*sin(rad), x2, y2, ILI9341_YELLOW);
    }
  } else if (code <= 2) {
    tft.fillCircle(x - 5, y - 5, 7, ILI9341_YELLOW);
    tft.fillRoundRect(x - 8, y - 2, 22, 12, 5, GREY_LIGHT);
  } else if (code == 3) {
    tft.fillRoundRect(x - 10, y - 5, 22, 12, 5, GREY_LIGHT);
  } else if (code == 45 || code == 48) {
    for (int i = 0; i < 4; i++)
      tft.drawFastHLine(x - 10, y - 4 + i*4, 20, GREY_LIGHT);
  } else if (code >= 51 && code <= 57) {
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, GREY_LIGHT);
    for (int i = 0; i < 3; i++) tft.fillCircle(x - 6 + i*6, y + 8, 1, RAIN_COLOR);
  } else if ((code >= 61 && code <= 65) || (code >= 80 && code <= 82)) {
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, GREY_DARK);
    for (int i = 0; i < 3; i++) tft.drawLine(x - 6 + i*6, y + 4, x - 8 + i*6, y + 10, RAIN_COLOR);
  } else if (code == 66 || code == 67) {
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, GREY_DARK);
    for (int i = 0; i < 3; i++) tft.drawLine(x - 6 + i*6, y + 4, x - 8 + i*6, y + 10, ILI9341_CYAN);
  } else if (code >= 71 && code <= 86) {
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, GREY_LIGHT);
    for (int i = 0; i < 3; i++) {
      int cx = x - 6 + i*6;
      tft.drawLine(cx-3, y+7, cx+3, y+7, ILI9341_WHITE);
      tft.drawLine(cx, y+4, cx, y+10, ILI9341_WHITE);
    }
  } else if (code >= 95) {
    tft.fillRoundRect(x - 10, y - 6, 22, 10, 5, GREY_DARK);
    tft.drawLine(x, y + 4, x - 3, y + 9, ILI9341_YELLOW);
    tft.drawLine(x - 3, y + 9, x + 1, y + 9, ILI9341_YELLOW);
    tft.drawLine(x + 1, y + 9, x - 2, y + 14, ILI9341_YELLOW);
  }
}

// ── Wi-Fi ───────────────────────────────────────────────────────────────

void connectWiFi() {
  tft.fillScreen(BG_COLOR);
  drawText("Connecting to WiFi...", 20, 100, 2, TEXT_COLOR);

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(500);
    tft.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    tft.fillScreen(BG_COLOR);
    drawText("Connected!", 60, 100, 2, TEXT_COLOR);
    drawText(WiFi.localIP().toString().c_str(), 60, 125, 1, GREY_LIGHT);
    delay(1000);
  } else {
    tft.fillScreen(ILI9341_RED);
    drawText("WiFi Failed!", 60, 110, 2, TEXT_COLOR);
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
    drawText("Waiting for data...", 30, 110, 2, TEXT_COLOR);
    return;
  }

  // ── Row 1: Large temperature ──────────────────────────────────────────
  int tempF = (int)round(cToF(current.tempC));
  char tempStr[8];
  snprintf(tempStr, sizeof(tempStr), "%d", tempF);

  drawText(tempStr, 10, 6, 5, TEMP_COLOR);
  int tw = textWidth(tempStr, 5);
  drawText("F", 14 + tw, 10, 2, TEXT_COLOR);

  if (forecast.valid) {
    drawTextRight(forecast.today.condition, 290, 10, 1, TEXT_COLOR);
    drawWeatherIcon(305, 16, forecast.today.weatherCode);
  }

  int feelsF = (int)round(cToF(current.feelsLikeC));
  char feelsStr[32];
  snprintf(feelsStr, sizeof(feelsStr), "Feels like %dF", feelsF);
  drawText(feelsStr, 10, 50, 1, GREY_LIGHT);

  drawDivider(62);

  // ── Row 2: Humidity, Wind, Rain ───────────────────────────────────────
  char hum[16]; snprintf(hum, sizeof(hum), "%d%%", current.humidityPct);
  char wind[16]; snprintf(wind, sizeof(wind), "%.0f km/h", current.windKmh);
  char rain[16]; snprintf(rain, sizeof(rain), "%.1fmm", current.rainTodayMm);

  drawText(hum,  20,  68, 2, RAIN_COLOR);
  drawText(wind, 110, 68, 2, TEXT_COLOR);
  drawText(rain, 220, 68, 2, RAIN_COLOR);

  drawText("Humidity", 20,  86, 1, GREY_DARK);
  drawText("Wind",     110, 86, 1, GREY_DARK);
  drawText("Rain",     220, 86, 1, GREY_DARK);

  drawDivider(98);

  // ── Row 3: Today's forecast ───────────────────────────────────────────
  if (forecast.valid) {
    drawText("TODAY", 10, 104, 2, ACCENT_COLOR);

    char todayHL[32];
    snprintf(todayHL, sizeof(todayHL), "Hi %.0fF  Lo %.0fF",
             forecast.today.tempMaxF, forecast.today.tempMinF);
    drawText(todayHL, 80, 104, 2, TEXT_COLOR);

    drawWeatherIcon(20, 130, forecast.today.weatherCode);
    char todayCond[40];
    snprintf(todayCond, sizeof(todayCond), "%s  %d%%",
             forecast.today.condition, forecast.today.precipPct);
    drawText(todayCond, 40, 126, 1, TEXT_COLOR);
  }

  drawDivider(142);

  // ── Row 4: Tomorrow's forecast ────────────────────────────────────────
  if (forecast.valid && forecast.dayCount >= 2) {
    ForecastDay& tmrw = forecast.daily[1];
    drawText("TOMORROW", 10, 148, 2, ACCENT_COLOR);

    char tmHL[32];
    snprintf(tmHL, sizeof(tmHL), "Hi %.0fF  Lo %.0fF", tmrw.tempMaxF, tmrw.tempMinF);
    drawText(tmHL, 110, 148, 2, TEXT_COLOR);

    drawWeatherIcon(20, 174, tmrw.weatherCode);
    char tmCond[40];
    snprintf(tmCond, sizeof(tmCond), "%s  %d%%", tmrw.condition, tmrw.precipPct);
    drawText(tmCond, 40, 170, 1, TEXT_COLOR);
  }

  drawDivider(186);

  // ── Row 5: Soil moisture ──────────────────────────────────────────────
  if (current.soilChannels > 0) {
    drawText("SOIL", 10, 192, 2, ACCENT_COLOR);
    char soil[48] = {0};
    int offset = 0;
    for (int i = 0; i < current.soilChannels && i < 4; i++) {
      offset += snprintf(soil + offset, sizeof(soil) - offset,
                         "%s%d%%", i > 0 ? "  " : "", current.soilMoisture[i]);
    }
    drawText(soil, 60, 192, 2, TEXT_COLOR);
  }

  // ── Row 6: UV + Pressure ────────────────────────────────────────────
  drawDivider(210);
  char bottom[48];
  snprintf(bottom, sizeof(bottom), "UV: %d   %.0f hPa",
           current.uvIndex, current.pressureHpa);
  drawText(bottom, 10, 216, 1, GREY_LIGHT);
}

void drawScreen2() {
  tft.fillScreen(BG_COLOR);

  // ── Header ────────────────────────────────────────────────────────────
  drawText("5-DAY FORECAST", 10, 4, 2, ACCENT_COLOR);
  drawDivider(22);

  if (!forecast.valid || forecast.dayCount < 2) {
    drawText("No forecast data", 60, 110, 2, TEXT_COLOR);
    return;
  }

  // ── Forecast rows (skip today = index 0, show 1..5) ──────────────────
  int y = 28;
  int shown = 0;
  for (int i = 1; i < forecast.dayCount && shown < 5; i++, shown++) {
    ForecastDay& d = forecast.daily[i];

    // Day abbreviation
    struct tm tm = {0};
    sscanf(d.date, "%d-%d-%d", &tm.tm_year, &tm.tm_mon, &tm.tm_mday);
    tm.tm_year -= 1900;
    tm.tm_mon  -= 1;
    mktime(&tm);
    const char* days[] = {"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};

    drawText(days[tm.tm_wday], 10, y, 2, TEXT_COLOR);
    drawWeatherIcon(60, y + 7, d.weatherCode);

    char hl[24];
    snprintf(hl, sizeof(hl), "%.0f / %.0fF", d.tempMaxF, d.tempMinF);
    drawText(hl, 85, y, 2, TEXT_COLOR);

    char pr[8];
    snprintf(pr, sizeof(pr), "%d%%", d.precipPct);
    drawText(pr, 270, y, 2, RAIN_COLOR);

    drawText(d.condition, 85, y + 18, 1, GREY_DARK);

    y += 36;
  }

  drawDivider(y + 2);

  // ── Bottom bar: UV + Pressure ─────────────────────────────────────────
  if (current.valid) {
    char bottom[48];
    snprintf(bottom, sizeof(bottom), "UV: %d    Pressure: %.0f hPa",
             current.uvIndex, current.pressureHpa);
    drawText(bottom, 10, y + 8, 1, TEXT_COLOR);
  }
}

// ── Main ────────────────────────────────────────────────────────────────

void setup() {
  Serial.begin(115200);
  delay(100);

  tft.begin();
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

### Building

Click the checkmark (✓) in the PlatformIO toolbar, or `Ctrl+Alt+B`.

### Uploading

The Arduino Nano ESP32 requires a specific upload flow on Windows because its native USB CDC port doesn't work with PlatformIO's built-in upload reliably. Use the included `flash_rescue.py` script:

```bash
python flash_rescue.py
```

The script automatically:
1. Detects the board on its CDC port (COM4, VID `2341:0070`)
2. Sends a 1200-baud DTR touch via the Win32 API to trigger bootloader mode
3. Waits for the bootloader port to appear (COM3, VID `303A:1001`)
4. Flashes using `esptool` from PlatformIO's packages

If the board doesn't appear automatically, **double-tap the reset button** to force bootloader mode.

> **Important:** Close any serial monitor before flashing — it locks the COM port.

### Serial Monitor

```bash
pio device monitor --port COM4 --baud 115200
```

Or click the plug icon in the PlatformIO toolbar.

### Troubleshooting

| Symptom | Fix |
|---|---|
| **White screen** | Check wiring (especially DC, RST, CS). Verify `TFT_DC`, `TFT_RST`, `TFT_CS` in build_flags match your wiring. |
| **Firmware crashes / USB disappears** | **Do NOT use TFT_eSPI** on this board — it bypasses the pin remap layer and crashes. Use Adafruit_ILI9341. |
| **Upload fails — "Cannot configure port"** | Close the serial monitor first. The COM port can only be opened by one process. |
| **Upload fails — port not found** | Double-tap the reset button to enter bootloader mode, then run `flash_rescue.py`. |
| **Board bricked — no USB at all** | Bad firmware crashed the USB stack. Double-tap reset rapidly — you have a ~300ms bootloader window. The `flash_rescue.py` script polls every 100ms to catch it. |
| **WiFi won't connect** | Check SSID/password in `config.h`. Ensure 2.4 GHz network (ESP32-S3 doesn't support 5 GHz). |
| **API returns 404** | Verify the Ecowitt relay is running and has posted at least one reading. |
| **JSON parse error** | Check API response with `Serial.println(http.getString())`. |
| **DFU upload protocol fails** | Use `upload_protocol = esptool` in platformio.ini. DFU requires WinUSB driver via Zadig. |

---

## Power & Enclosure Tips

- **USB power:** 5V/500mA from any USB adapter is sufficient.
- **Low-power mode:** Between refresh intervals you can put the ESP32-S3 into light sleep. Replace `delay(100)` in `loop()` with `esp_sleep_enable_timer_wakeup()` for significant power savings.
- **Enclosure:** A 3D-printed case works well. The display typically mounts with M2.5 screws. Leave ventilation holes near the ESP32.
- **Backlight dimming:** Connect the TFT LED pin to a GPIO (e.g., D6) and use `analogWrite()` / `ledcWrite()` to dim at night.
- **OTA updates:** Add `ArduinoOTA` to `setup()` for wireless firmware updates without re-plugging USB.

---

## Known Issues & Lessons Learned

### TFT_eSPI is incompatible with Arduino Nano ESP32

The Arduino Nano ESP32 variant (`arduino_nano_nora`) uses `BOARD_HAS_PIN_REMAP` to translate Arduino D-pin numbers (D8, D9, D10, etc.) to raw ESP32-S3 GPIOs (GPIO17, GPIO18, GPIO21, etc.).

TFT_eSPI uses **both** Arduino SPI calls (which go through the remap layer) **and** raw ESP-IDF calls (`spi_bus_config_t`, `gpio_set_direction`) which bypass it. This means:
- SPI.begin() configures the remapped GPIOs correctly
- But TFT_eSPI's direct GPIO access uses the un-remapped numbers
- The DC and RST pins end up pointing at wrong hardware pins
- This can crash the firmware and stop USB from enumerating

Even setting `BOARD_USES_HW_GPIO_NUMBERS=1` (which disables the remap layer entirely) doesn't help because then Arduino SPI uses the wrong pins.

**Solution:** Use Adafruit_ILI9341 + Adafruit_GFX which only use standard Arduino APIs.

### Windows upload quirks

The Nano ESP32 has two USB identities:
- **Normal mode:** `VID:PID = 2341:0070` — Arduino USB CDC (serial port)
- **Bootloader mode:** `VID:PID = 303A:1001` — ESP32-S3 ROM bootloader

PlatformIO's esptool can only flash in bootloader mode. To enter bootloader:
1. Send a 1200-baud DTR touch to the CDC port (what `flash_rescue.py` does)
2. Or double-tap the physical reset button

The CDC port cannot be opened by pyserial's standard methods on Windows (you get `OSError(22)`). The `flash_rescue.py` script uses the Win32 `CreateFileW` API directly to work around this.
