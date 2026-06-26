---
title: InvoiceHub API
emoji: 📄
colorFrom: blue
colorTo: indigo
sdk: docker
pinned: false
---

# InvoiceHub API

GST-compliant invoicing and business management backend API built with .NET 10.

## Tech Stack

- **Runtime:** .NET 10
- **ORM:** Entity Framework Core 10 with Npgsql
- **Database:** PostgreSQL (Neon)
- **Auth:** JWT Bearer (access + refresh tokens)
- **PDF:** QuestPDF
- **Email:** MailKit + SMTP (Gmail)
- **AI:** Groq API (LLaMA)
- **Logging:** Serilog (console + rolling file + JSON)

## Project Structure

```
├── API/                  # ASP.NET Core Web API (entry point)
│   ├── Controllers/      # API endpoints
│   ├── Middleware/        # Exception handling, tenant resolution
│   ├── Filters/           # Validation, tenant context
│   └── Converters/        # UTC DateTime JSON converters
├── Application/          # Business logic, DTOs, validators
│   ├── DTOs/             # Request/Response records
│   ├── Services/         # Application services
│   └── Validators/       # FluentValidation rules
├── Core/                 # Domain layer (entities, interfaces, enums)
│   ├── Entities/         # Domain models
│   ├── Enums/            # Enum types
│   └── Interfaces/       # Repository contracts
└── Infrastructure/       # EF Core, migrations, repositories
    ├── Data/             # DbContext, configurations, seeder
    ├── Migrations/       # EF Core migrations
    └── Repositories/     # Repository implementations
```

## Features

- Multi-tenant SaaS architecture
- Invoice management with GST (CGST/SGST/IGST)
- Purchase orders, expenses, payments
- Customer & supplier management
- Product catalog with inventory tracking
- Credit notes, e-Way bill, e-Invoice fields
- AI-powered business insights
- Role-based access (SuperAdmin → Admin → Manager → Accountant → SalesAgent → Viewer)
- Dashboard with KPIs, revenue, overdue tracking
- Email notifications (verification, invoices, reminders)
- PDF generation for invoices
- File upload for logos & receipts
- Rate limiting, health checks, structured logging

## Prerequisites

- .NET 10 SDK
- PostgreSQL database (Neon, Supabase, or local)
- SMTP credentials (optional, for email features)
- Groq API key (optional, for AI features)

## Setup

### 1. Clone & Restore

```bash
git clone https://github.com/ManthanThakor/invoicehub-api.git
cd invoicehub-api
dotnet restore
```

### 2. Configure Environment

Copy `.env.example` to `.env` and fill in your values:

```bash
cp .env.example .env
```

Or set environment variables directly in your hosting platform:

| Variable | Description | Required |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes |
| `Jwt__Secret` | JWT signing key (min 32 chars) | Yes |
| `Email__Host` | SMTP host | No |
| `Email__Username` | SMTP username | No |
| `Email__Password` | SMTP password | No |
| `AI__ApiKey` | Groq API key | No |
| `Cors__AllowedOrigins__0` | Frontend origin | Yes |

### 3. Database

Migrations run automatically on startup. To manually apply:

```bash
dotnet ef database update --project Infrastructure --startup-project API
```

### 4. Run

```bash
dotnet run --project API
```

API available at `http://localhost:5001` | Swagger at `/swagger`.

## Deployment

### Render / Railway / Fly.io

1. Set environment variables in the dashboard
2. Set build command: `dotnet publish API/API.csproj -c Release -o /app/publish`
3. Set start command: `dotnet /app/publish/API.dll`

### Docker

```bash
docker build -t invoicehub-api .
docker run -p 5000:80 invoicehub-api
```

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | No | Login |
| POST | `/api/auth/register` | No | Register |
| POST | `/api/auth/refresh` | No | Refresh token |
| GET | `/api/invoices` | AllRoles | List invoices |
| POST | `/api/invoices` | SalesUp | Create invoice |
| GET | `/api/invoices/{id}` | AllRoles | Get invoice |
| PUT | `/api/invoices/{id}` | SalesUp | Update invoice |
| POST | `/api/invoices/{id}/send` | SalesUp | Send invoice email |
| GET | `/api/invoices/{id}/pdf` | AllRoles | Download PDF |
| GET | `/api/tenant` | AllRoles | Get business profile |
| PUT | `/api/tenant` | AdminOnly | Update business profile |
| GET | `/api/tenant/dashboard` | AllRoles | Dashboard KPIs |
| GET | `/health` | No | Health check |

## License

MIT
