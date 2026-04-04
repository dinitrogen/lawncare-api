# Notifications When the App Is Inactive

## Background

The `ReminderNotificationWorker` is an in-process `BackgroundService` that polls Firestore
every five minutes and dispatches Discord webhooks for reminders whose date/time has arrived.
This works well while the API is running, but **Cloud Run scales to zero** when there is no
incoming traffic, which means the background worker is not running and reminders can be missed.

---

## Problem Statement

Cloud Run instances are torn down after a period of inactivity (default: 15 minutes of idle).
Any reminders that fall due during a quiet period will not be notified until the next HTTP
request wakes the instance and the worker runs again.

---

## Options

### Option 1 – Cloud Scheduler pings the API (recommended for quick adoption)

Create a Cloud Scheduler job that sends a lightweight HTTP `GET` to the API every 5–15 minutes:

```
GET https://<api-host>/api/health   (or any authenticated endpoint)
```

This keeps the Cloud Run instance warm, so the `ReminderNotificationWorker` never stops.

**Pros:** Zero new infrastructure; easy to enable today.  
**Cons:** Costs a small amount to keep the instance warm 24/7; doesn't scale well if the
         number of users grows significantly.

---

### Option 2 – Dedicated Cloud Scheduler → Cloud Run Job (recommended long-term)

Extract the notification-dispatch logic into a standalone Cloud Run Job and trigger it on a
fixed schedule via Cloud Scheduler.

```
Cloud Scheduler (every 5 min)
  └─▶ Cloud Run Job: lawncare-reminder-dispatcher
        ├─ Query Firestore collection group "reminders"
        ├─ Send due Discord webhooks
        └─ Set NotificationSent = true
```

The main API no longer needs the `ReminderNotificationWorker` background thread.

**Pros:** Guaranteed execution even with zero API traffic; cost-efficient (pay per invocation).  
**Cons:** Requires a second container image and additional IAM/deployment configuration.

---

### Option 3 – Firebase Scheduled Cloud Function

Write a Firebase Cloud Function triggered by Firebase's built-in cron scheduler (backed by
Cloud Scheduler):

```typescript
export const sendDueReminders = functions.pubsub
  .schedule("every 5 minutes")
  .onRun(async () => {
    // Query Firestore, send webhooks, mark sent
  });
```

**Pros:** Serverless; no container management; tight Firestore integration.  
**Cons:** Reintroduces a Firebase Functions dependency the project previously moved away from;
         requires Node.js/TypeScript toolchain separate from the .NET codebase.

---

### Option 4 – Firestore Triggers (event-driven, zero polling)

Use a Firestore-triggered Cloud Function (or Eventarc trigger) that fires whenever a
`reminders` document is written. Schedule the actual notification using Cloud Tasks with a
delivery timestamp equal to the reminder's date/time.

```
POST /api/reminders
  └─▶ Firestore write
        └─▶ Firestore trigger
              └─▶ Cloud Tasks: enqueue task with ETA = reminder date/time
                    └─▶ Cloud Run endpoint: send Discord webhook
```

**Pros:** Exact delivery time; no polling overhead; scales to zero cost when idle.  
**Cons:** Highest operational complexity; requires Cloud Tasks queue, Firestore trigger, and
         an additional HTTP handler endpoint.

---

## Recommended Path

| Phase | Action |
|-------|--------|
| **Now** | Enable Option 1 (Cloud Scheduler warm-up ping) to close the gap immediately. |
| **Next** | Migrate to Option 2 (dedicated Cloud Run Job) for a production-grade, cost-efficient solution. |
| **Future** | Evaluate Option 4 (Cloud Tasks) if exact delivery time becomes a strict requirement. |

---

## Firestore Index Required

The `ReminderNotificationWorker` (and any Cloud Run Job equivalent) uses a Firestore
collection-group query with multiple filter fields. A composite index must be created in the
Firebase console for the `reminders` collection group:

| Field | Order |
|-------|-------|
| `SendDiscordReminder` | Ascending |
| `NotificationSent` | Ascending |
| `Date` | Ascending |

Without this index the query will return an error with a link to create it automatically.
