# TRACELOCK — Digital Evidence & Forensics Management System

A professional .NET 9 evidence-management application for investigation teams. The current web project uses Razor Pages with the modern ASP.NET Core hosting model, Entity Framework Core, cookie authentication and a database-provider switch that supports the existing SQLite development database or SQL Server 2022 in Docker.

## Run locally

```bash
dotnet restore
dotnet run --project DigitalEvidenceSystem.Web
```

Open the address printed by the terminal.

### Demo account
- Username: `admin`
- Password: `Admin@123!`

The password is stored as a PBKDF2 password hash and is never stored as plaintext.

## SQL Server 2022

Start SQL Server with Docker Compose:

```bash
cp .env.example .env
docker compose up -d
```

Set `SQLSERVER_CONNECTION` in the environment when you want to use SQL Server. When it is empty, the existing `digital-evidence.db` SQLite database remains the default development store, so existing local data is preserved.

Example:

```text
SQLSERVER_CONNECTION=Server=localhost,1433;Database=DigitalEvidenceSystem;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=False
```

## Included workflow

Login → Dashboard → Create Case → Add Evidence → Record Collection / Custody → Assign Analysis → Attach Evidence Files → Generate SHA-256 → Verify Integrity → Record Findings → Generate Reports → Review Audit Log → Close Case.

## Functional modules

- Role-aware login and logout
- Dashboard with active/closed cases, evidence, analysis workload and integrity attention
- Case CRUD, case details, notes and relevant persons
- Evidence register, advanced filtering, custodians, collection metadata and integrity state
- Append-only chain-of-custody UI with transfer condition, location and authorization
- Forensic analysis workflow with status progression, tools, findings and report references
- Secure evidence-file storage outside SQL with metadata and SHA-256 verification
- Notifications
- Administrator user creation, activation/deactivation and role management
- Audit history with actor, entity, timestamp, details and IP address
- Printable reports plus evidence, custody and audit CSV exports
- Responsive desktop-first UI

## Storage

Evidence files are stored under `DigitalEvidenceSystem.Web/App_Data/EvidenceStorage` and are not served as static files. Only metadata and file references are kept in the database.

## Existing SQLite data

The application performs a small startup schema upgrade for the existing SQLite database. It adds the new columns/tables without deleting existing cases, evidence, users or custody records.
