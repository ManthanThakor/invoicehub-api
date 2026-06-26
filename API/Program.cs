using Application.Services.Catalog;
using Application.Services.Finance;
using Application.Services.Identity;
using Application.Services.Purchases;
using Application.Services.Sales;
using Application.Services.System;
using Application.Services.Tenancy;
using Application.Services.Utilities;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Infrastructure.Repositories;
using InvoiceHub.API.Filters;
using InvoiceHub.API.Middleware;
using InvoiceHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;


// ─────────────────────────────────────────────────────────────────────
//  BOOTSTRAP LOGGER
//  Captures any crash that happens before DI is ready.
// ─────────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting InvoiceHub API...");

    // Load .env if present (optional — not needed in production where env vars are used)
    try { DotNetEnv.Env.Load(); }
    catch (FileNotFoundException) { Log.Information("No .env file — using environment variables."); }

    var builder = WebApplication.CreateBuilder(args);

    // Railway sets PORT env var — only apply in non-Development
    // In Development, launch profile (launchSettings.json) controls the URL with HTTPS
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    builder.Configuration.AddEnvironmentVariables();


    // ══════════════════════════════════════════════════════════════════
    //  1. SERILOG
    // ══════════════════════════════════════════════════════════════════
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        var logFile = ctx.Configuration["Serilog:FilePath"] ?? "logs/invoicehub-.log";

        cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "InvoiceHub")

            // Console: verbose in Dev, compact in Prod
            .WriteTo.Console(
                outputTemplate: ctx.HostingEnvironment.IsDevelopment()
                    ? "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    : "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

            // Rolling text log — 30-day retention
            .WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {TenantId} " +
                    "{SourceContext}: {Message:lj}{NewLine}{Exception}")

            // Structured JSON log — for ELK / Seq / Grafana
            .WriteTo.File(
                new Serilog.Formatting.Compact.CompactJsonFormatter(),
                "logs/invoicehub-json-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)

            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
                ctx.HostingEnvironment.IsDevelopment()
                    ? LogEventLevel.Information : LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
    });

    // ══════════════════════════════════════════════════════════════════
    //  2. DATABASE
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var conn = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Check your .env or environment variables.");

        options.UseNpgsql(conn, npgsql =>
        {
            npgsql.CommandTimeout(60);
            npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
        });

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // ══════════════════════════════════════════════════════════════════
    //  3. REPOSITORIES
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ITenantRepository, TenantRepository>();
    builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
    builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
    builder.Services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
    builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
    builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
    builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    builder.Services.AddScoped<IAIInsightRepository, AIInsightRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

    // ══════════════════════════════════════════════════════════════════
    //  4. APPLICATION SERVICES
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddScoped<IGSTCalculationService, GSTCalculationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IPurchaseService, PurchaseService>();
    builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IInventoryService, InventoryService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<ISupplierService, SupplierService>();
    builder.Services.AddScoped<IExpenseService, ExpenseService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IInsightService, InsightService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    builder.Services.AddScoped<IPdfService, PdfService>();

    // ══════════════════════════════════════════════════════════════════
    //  5. HTTP CLIENTS
    // ══════════════════════════════════════════════════════════════════
    // Named client for AI provider (Groq / OpenRouter / Ollama)
    builder.Services.AddHttpClient("AI", client =>
    {
        var baseUrl = (builder.Configuration["AI:BaseUrl"]
                      ?? "https://api.groq.com/openai/v1").TrimEnd('/');
        client.BaseAddress = new Uri(baseUrl + "/"); // trailing slash is REQUIRED
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Named client for Google OAuth token validation
    builder.Services.AddHttpClient("Google", client =>
    {
        client.BaseAddress = new Uri("https://oauth2.googleapis.com/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddMemoryCache();

    // ══════════════════════════════════════════════════════════════════
    //  6. AUTHENTICATION — JWT Bearer
    // ══════════════════════════════════════════════════════════════════
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException(
            "Jwt:Secret is not configured. " +
            "Add it to your .env file: Jwt__Secret=your-secret-key");

    if (jwtSecret.Length < 32)
        throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");

    builder.Services
        .AddAuthentication(opts =>
        {
            opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "InvoiceHub",
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "InvoiceHub",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret))
            };

            // Support JWT from cookie (for browser clients that store token in cookie)
            opts.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    // Cookie fallback — front-end can store token in HttpOnly cookie
                    if (string.IsNullOrEmpty(ctx.Token))
                        ctx.Token = ctx.Request.Cookies["access_token"];
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = ctx =>
                {
                    if (ctx.Exception is SecurityTokenExpiredException)
                        ctx.Response.Headers["Token-Expired"] = "true";

                    Log.Warning("JWT authentication failed: {Error}",
                        ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    // ══════════════════════════════════════════════════════════════════
    //  7. AUTHORIZATION POLICIES
    //
    //  Role hierarchy (highest to lowest):
    //  SuperAdmin > Admin > Manager > Accountant > SalesAgent > Viewer
    //
    //  Policy naming convention: "XxxUp" = that role AND everything above it.
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddAuthorization(opts =>
    {
        // Only the platform owner (YOU)
        opts.AddPolicy("SuperAdminOnly",
            p => p.RequireRole("SuperAdmin"));

        // Business owner / first user who created the company
        opts.AddPolicy("AdminOnly",
            p => p.RequireRole("SuperAdmin", "Admin"));

        // Admin + Manager
        opts.AddPolicy("ManagerUp",
            p => p.RequireRole("SuperAdmin", "Admin", "Manager"));

        // Admin + Manager + Accountant
        opts.AddPolicy("AccountantUp",
            p => p.RequireRole("SuperAdmin", "Admin", "Manager", "Accountant"));

        // Admin + Manager + Accountant + SalesAgent
        opts.AddPolicy("SalesUp",
            p => p.RequireRole("SuperAdmin", "Admin", "Manager", "Accountant", "SalesAgent"));

        // Everyone who is authenticated (including Viewer)
        opts.AddPolicy("AllRoles",
            p => p.RequireRole(
                "SuperAdmin", "Admin", "Manager", "Accountant", "SalesAgent", "Viewer"));
    });

    // ══════════════════════════════════════════════════════════════════
    //  8. CORS
    // ══════════════════════════════════════════════════════════════════
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:3001" };

    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("DefaultCors", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                // Expose Content-Disposition so the browser can read the filename
                .WithExposedHeaders("Content-Disposition", "Token-Expired");
        });
    });

    // ══════════════════════════════════════════════════════════════════
    //  9. RATE LIMITING
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddRateLimiter(opts =>
    {
        // General API: 100 requests per minute per IP
        opts.AddSlidingWindowLimiter("GeneralApi", cfg =>
        {
            cfg.Window = TimeSpan.FromMinutes(1);
            cfg.SegmentsPerWindow = 6;
            cfg.PermitLimit = 100;
            cfg.QueueLimit = 0;
            cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        // Auth endpoints: 10 requests per minute per IP (brute-force protection)
        opts.AddSlidingWindowLimiter("AuthApi", cfg =>
        {
            cfg.Window = TimeSpan.FromMinutes(1);
            cfg.SegmentsPerWindow = 6;
            cfg.PermitLimit = 10;
            cfg.QueueLimit = 0;
            cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        opts.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter = "60";
            await ctx.HttpContext.Response.WriteAsJsonAsync(new
            {
                Success = false,
                Message = "Too many requests. Please wait 60 seconds and try again."
            }, ct);
        };
    });

    // ══════════════════════════════════════════════════════════════════
    //  10. CONTROLLERS + FILTERS + JSON OPTIONS
    // ══════════════════════════════════════════════════════════════════
    builder.Services
        .AddControllers(opts =>
        {
            opts.Filters.Add<ValidationFilter>();    // ModelState → 400 ApiResponse
            opts.Filters.Add<TenantContextFilter>(); // Enriches Serilog with TenantId
        })
        .AddJsonOptions(opts =>
        {
            // PascalCase to match ApiResponse<T> record properties
            opts.JsonSerializerOptions.PropertyNamingPolicy = null;
            // Omit null values to keep responses clean
            opts.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            // Serialize enums as strings (e.g. "Paid" instead of 3)
            opts.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
            // Normalize all DateTime values to UTC (required by Npgsql timestamptz)
            opts.JsonSerializerOptions.Converters.Add(
                new InvoiceHub.API.Converters.UtcDateTimeConverter());
            opts.JsonSerializerOptions.Converters.Add(
                new InvoiceHub.API.Converters.NullableUtcDateTimeConverter());
        });

    // ══════════════════════════════════════════════════════════════════
    //  11. SWAGGER / OPENAPI
    // ══════════════════════════════════════════════════════════════════
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "InvoiceHub API",
            Version = "v1",
            Description =
                "GST-compliant invoicing and business management API.\n\n" +
                "**Role hierarchy:** SuperAdmin → Admin → Manager → Accountant → SalesAgent → Viewer\n\n" +
                "Authenticate via POST /api/auth/login, then click 'Authorize' and paste your JWT.",
            Contact = new OpenApiContact
            {
                Name = "InvoiceHub Support",
                Email = "support@invoicehub.in"
            }
        });

        // JWT Bearer auth scheme for Swagger UI
        opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Paste your JWT access token here.\n" +
                "Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        });

        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Group endpoints by Tag (matches [Tags("...")] attributes on controllers)
        opts.TagActionsBy(api => api.GroupName is not null
            ? new[] { api.GroupName }
            : new[] { api.ActionDescriptor.RouteValues["controller"] ?? "Default" });

        opts.DocInclusionPredicate((_, _) => true);
    });

    // ══════════════════════════════════════════════════════════════════
    //  12. QUESTPDF LICENSE
    // ══════════════════════════════════════════════════════════════════
    QuestPDF.Settings.License = LicenseType.Community;

    // ══════════════════════════════════════════════════════════════════
    //  13. HEALTH CHECKS
    // ══════════════════════════════════════════════════════════════════
    builder.Services
        .AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    // ══════════════════════════════════════════════════════════════════
    //  BUILD
    // ══════════════════════════════════════════════════════════════════
    var app = builder.Build();

    // ─────────────────────────────────────────────────────────────────
    //  AUTO MIGRATE + SEED  (runs once on startup)
    // ─────────────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                Log.Information("Applying {Count} pending EF Core migrations...", pending.Count());
                await db.Database.MigrateAsync();
                Log.Information("Migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed. Cannot start.");
            throw;
        }
    }

    // Seed SuperAdmin AFTER migrations (table must exist)
    await SuperAdminSeeder.SeedAsync(app.Services);

    // ─────────────────────────────────────────────────────────────────
    //  MIDDLEWARE PIPELINE
    //  ORDER IS CRITICAL — do not rearrange.
    // ─────────────────────────────────────────────────────────────────

    // 1. Serilog request logging (skip health + static files)
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0}ms";

        // Log level based on status code
        opts.GetLevel = (httpCtx, elapsed, ex) =>
            ex is not null || httpCtx.Response.StatusCode >= 500 ? LogEventLevel.Error
            : httpCtx.Response.StatusCode >= 400 ? LogEventLevel.Warning
            : elapsed > 2000 ? LogEventLevel.Warning  // slow request
                                                                  : LogEventLevel.Information;

        opts.EnrichDiagnosticContext = (diag, httpCtx) =>
        {
            diag.Set("ClientIp", httpCtx.Connection.RemoteIpAddress?.ToString());
            diag.Set("UserAgent", httpCtx.Request.Headers.UserAgent.ToString());
            diag.Set("RequestHost", httpCtx.Request.Host.Value);
        };
    });

    // 2. Global exception handler — always first after logging
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 3. HTTPS redirect (skip in dev to avoid certificate warnings)
    if (!app.Environment.IsDevelopment())
        app.UseHsts();
    app.UseHttpsRedirection();

    // 4. Static files (serves wwwroot/uploads/*)
    app.UseStaticFiles();

    // 5. Swagger UI (dev + staging only — NEVER in production)
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint("/swagger/v1/swagger.json", "InvoiceHub API v1");
            opts.RoutePrefix = "swagger";
            opts.DocumentTitle = "InvoiceHub API";
            opts.DisplayRequestDuration();
            opts.EnableFilter();
            opts.EnableDeepLinking();
            opts.DefaultModelsExpandDepth(-1); // hide schema models by default
        });
    }

    // 6. CORS — before auth so preflight requests work
    app.UseCors("DefaultCors");

    // 7. Rate limiting
    app.UseRateLimiter();

    // 8. Auth — MUST be before TenantMiddleware
    app.UseAuthentication();
    app.UseAuthorization();

    // 9. Tenant resolver — reads tenantId from JWT, validates active tenant
    app.UseMiddleware<TenantMiddleware>();

    // 10. Controllers
    app.MapControllers();

    // 11. Health check endpoint
    app.MapHealthChecks("/health").AllowAnonymous();

    // 12. Root path — redirect to Swagger in dev, health check in prod
    app.MapGet("/", () =>
        Results.Redirect(app.Environment.IsDevelopment() ? "/swagger" : "/health"))
        .ExcludeFromDescription();

    Log.Information(
        "InvoiceHub API running | Environment: {Env} | URLs: {Urls}",
        app.Environment.EnvironmentName,
        string.Join(", ", app.Urls));

    await app.RunAsync();
}
catch (HostAbortedException)
{
    // Normal shutdown — not an error
}
catch (Exception ex)
{
    Log.Fatal(ex, "InvoiceHub API terminated unexpectedly.");
    Environment.Exit(1);    
}
finally
{
    await Log.CloseAndFlushAsync();
}