# Deployment plan (GoDaddy) — DecisionOS.Distribution.V1

This document is a **practical, end-to-end deployment plan** for hosting **`DecisionOS.Distribution.Web`** (ASP.NET Core / .NET 8) with a **PostgreSQL** database on **GoDaddy**.

It covers two realistic GoDaddy hosting models:

- **Option A (recommended)**: GoDaddy **VPS/Dedicated** (Windows or Linux) where you control the OS.
- **Option B (only if available)**: GoDaddy managed **Windows hosting + IIS** with ASP.NET Core hosting bundle support.

> If you can choose, pick **Option A**. This app needs a PostgreSQL database and reliable background OS-level access for migrations, backups, and troubleshooting.

---

## 0) What we are deploying

- **Web app**: `src/DecisionOS.Distribution.Web` (Razor Pages + minimal API under `/api`)
- **Database**: PostgreSQL (schema managed by EF Core migrations)
- **Auth**: ASP.NET Identity (cookie auth); admin user seeded from config key `SeedAdmin:*`
- **Uploads**: files stored under Web content root: `App_Data/uploads/...`

**Configuration keys (prod)**

- `ConnectionStrings__DecisionOs`
- `SeedAdmin__Email`
- `SeedAdmin__Password`
- `ASPNETCORE_URLS` (when using Kestrel directly)
- `ASPNETCORE_ENVIRONMENT=Production`

---

## 1) Prerequisites & decisions (do these first)

### 1.1 Hosting decision

Choose one:

- **A1: Linux VPS** (Ubuntu) + Nginx + systemd + PostgreSQL
- **A2: Windows VPS** + IIS (reverse proxy) + ASP.NET Core Hosting Bundle + PostgreSQL
- **B: Managed Windows hosting** (only if it supports ASP.NET Core 8 + allows PostgreSQL connectivity and persistent storage)

### 1.2 DNS / domain decision

- **Domain**: `app.yourdomain.com` (recommended subdomain)
- DNS record:
  - VPS: `A` record → your server public IP
  - Managed hosting: follow GoDaddy panel instructions

### 1.3 Data retention decision

Decide retention for:
- DB backups (e.g., 30 days)
- Uploaded files under `App_Data/uploads` (backed up daily)

---

## 2) Build artifact strategy (recommended)

On a build machine (CI or your dev box), publish the web app:

```powershell
dotnet publish .\src\DecisionOS.Distribution.Web -c Release -o .\out\web
```

The deployment artifact is the contents of `out/web/`.

> You do **not** deploy the whole repo; deploy the published output.

---

## 3) Database provisioning (PostgreSQL)

### 3.1 Create DB + user

Create a database and user (example):

```sql
CREATE USER decisionos_app WITH PASSWORD 'REPLACE_WITH_STRONG_PASSWORD';
CREATE DATABASE decisionos OWNER decisionos_app;
```

### 3.2 Connection string

Set **exactly** this key on the server (env var preferred):

- `ConnectionStrings__DecisionOs` =
  - `Host=<db-host>;Port=5432;Database=decisionos;Username=decisionos_app;Password=<password>;SSL Mode=Require;Trust Server Certificate=true`

> For production, enable TLS to the DB if your PostgreSQL provider supports it.

---

## 4) Applying migrations (production)

The repo already uses EF Core migrations.

### 4.1 Preferred migration approach

Run migrations **from the deployed web host** (same config/connection string) before starting traffic.

On the server, run:

```powershell
dotnet ef database update --project .\src\DecisionOS.Distribution.Infrastructure --startup-project .\src\DecisionOS.Distribution.Web
```

If you deploy only published output (recommended), then instead run migrations from a checked-out repo on the server (or from CI) using the same `ConnectionStrings__DecisionOs`.

### 4.2 “First boot” seed user

When the web app starts, it runs:

- role creation
- SeedAdmin user creation (if `SeedAdmin:Password` is set)

So for production, ensure these are set **before first start**:

- `SeedAdmin__Email=admin@yourcompany.com`
- `SeedAdmin__Password=<strong password (>= 8, with upper/lower/digit/symbol)>`

---

## 5) Option A1 — Linux VPS (Ubuntu) + Nginx + systemd (recommended)

### 5.1 Install dependencies

- Install .NET 8 runtime
- Install Nginx
- Install PostgreSQL **or** use managed Postgres (recommended)

### 5.2 Create app folder & user

- `/var/www/decisionos/`
- create `decisionos` linux user
- ensure writable folder:
  - `/var/www/decisionos/App_Data/uploads`

### 5.3 Deploy published output

Upload `out/web/*` to `/var/www/decisionos/`.

### 5.4 systemd service

Create `/etc/systemd/system/decisionos.service`:

- ExecStart: `dotnet /var/www/decisionos/DecisionOS.Distribution.Web.dll`
- WorkingDirectory: `/var/www/decisionos`
- Environment:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ASPNETCORE_URLS=http://127.0.0.1:5276`
  - `ConnectionStrings__DecisionOs=...`
  - `SeedAdmin__Email=...`
  - `SeedAdmin__Password=...`

Enable and start:

- `systemctl enable decisionos`
- `systemctl start decisionos`

### 5.5 Nginx reverse proxy + HTTPS

- Nginx upstream to `http://127.0.0.1:5276`
- Use Let’s Encrypt (certbot) for TLS
- Ensure large upload sizes if needed:
  - `client_max_body_size 50m;`

### 5.6 Firewall

- allow `80/tcp`, `443/tcp`
- block direct 5276 externally

---

## 6) Option A2 — Windows VPS + IIS

### 6.1 Install on server

- .NET 8 **ASP.NET Core Hosting Bundle**
- IIS (Web Server role)
- (Optional) URL Rewrite + ARR if needed

### 6.2 Deploy published output

Example folder:
- `C:\inetpub\DecisionOS\`
- Ensure write permissions for IIS App Pool identity on:
  - `C:\inetpub\DecisionOS\App_Data\uploads`

### 6.3 Configure IIS site

- Site binding: `http/https` for your domain
- App pool: **No Managed Code** (ASP.NET Core runs out-of-process)

### 6.4 Configure environment variables for the app pool

Set (via IIS configuration or machine env vars):

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DecisionOs=...`
- `SeedAdmin__Email=...`
- `SeedAdmin__Password=...`

### 6.5 HTTPS

- Use GoDaddy cert or Let’s Encrypt for Windows
- Bind HTTPS in IIS

---

## 7) Option B — Managed GoDaddy Windows hosting

Only proceed if ALL are true:

- Supports **ASP.NET Core 8**
- Allows setting environment variables (or secure app settings)
- Has persistent storage for `App_Data/uploads`
- Can reach PostgreSQL (hosted elsewhere or as an add-on)

If any of these are missing, use **Option A**.

---

## 8) Release checklist (what to do on deploy day)

- **Config**
  - Set `ConnectionStrings__DecisionOs`
  - Set `SeedAdmin__Email` + `SeedAdmin__Password`
  - Confirm `ASPNETCORE_ENVIRONMENT=Production`
- **DB**
  - Take a snapshot/backup
  - Apply migrations
- **Files**
  - Ensure `App_Data/uploads` exists and is writable
- **Start**
  - Start service / recycle IIS app pool
- **Smoke tests**
  - `GET /health` returns OK
  - Login works
  - Operations → Uploads page loads
  - Upload a small CSV and validate/import

---

## 9) Monitoring & logs

### Web app logs

- Linux: `journalctl -u decisionos -f`
- Windows: Event Viewer + stdout logs (if enabled) + IIS logs

### Database monitoring

- Monitor disk usage, connections, slow queries

---

## 10) Backups & rollback

### 10.1 Backups

- **DB**: daily `pg_dump` (or managed provider automated backups)
- **Uploads folder**: daily file backup/snapshot

### 10.2 Rollback

- Re-deploy previous published output artifact
- Restore DB snapshot (only if schema/data is incompatible)
  - Prefer forward-fix migrations when possible

---

## 11) Security hardening (minimum)

- Use strong passwords; do not keep `SeedAdmin__Password` empty in production.
- Restrict DB to private network / allowlist server IP only.
- Enforce HTTPS.
- Consider adding IP allowlist for `/Operations` during pilot if required.

