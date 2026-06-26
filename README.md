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
| `CORS_ORIGIN` | Frontend origin (comma-separated for multiple) | Yes |

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

**Production:** Health check at [https://manthurocks-invoicehub-api.hf.space/health](https://manthurocks-invoicehub-api.hf.space/health).

## Deployment

**Live API:** [https://manthurocks-invoicehub-api.hf.space](https://manthurocks-invoicehub-api.hf.space)

### Hugging Face Spaces (Docker)

1. Create a Space at [huggingface.co](https://huggingface.co) with **Docker** SDK
2. Set environment variables in **Settings → Repository secrets**:

   | Variable | Description | Required |
   |---|---|---|
   | `CONNECTIONSTRINGS__DEFAULTCONNECTION` | PostgreSQL connection string | Yes |
   | `JWT__SECRET` | JWT signing key (min 32 chars) | Yes |
   | `JWT__ISSUER` | `InvoiceHub` | Yes |
   | `JWT__AUDIENCE` | `InvoiceHub` | Yes |
   | `SUPERADMIN__EMAIL` | `superadmin@invoicehub.in` | Yes |
   | `SUPERADMIN__PASSWORD` | SuperAdmin password | Yes |
   | `ASPNETCORE_ENVIRONMENT` | `Production` | Yes |
   | `CORS_ORIGIN` | Frontend URL | Yes |

3. Push to the Space: `git push hf main --force`

### Docker

```bash
docker build -t invoicehub-api .
docker run -p 7860:7860 invoicehub-api
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
