using Microsoft.EntityFrameworkCore;
using MassTransit;
using Serilog;
using BillingService.Api.Data;
using BillingService.Api.Services;
using BillingService.Api.Clients;
using BillingService.Api.Resilience;
using BillingService.Api.Middleware;
using BillingService.Api.Configuration;
using BillingService.Api.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog Logging ──────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddConfiguredApiControllers();
builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SchemaFilter<RequiredPropertiesSchemaFilter>();
    c.SwaggerDoc("v1", new()
    {
        Title = "Billing Service API",
        Version = "v1",
        Description = "Microsserviço responsável pela emissão e gerenciamento de notas fiscais."
    });
});

// ── PostgreSQL + Entity Framework Core ───────────────────────────────────────
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    ).UseSnakeCaseNamingConvention()
);

// ── HttpClient para comunicação com Inventory Service (com Polly) ─────────────
builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryService:BaseUrl"]
        ?? throw new InvalidOperationException("InventoryService:BaseUrl not configured."));
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddPolicyHandler(ResiliencePolicy.GetRetryPolicy())
.AddPolicyHandler(ResiliencePolicy.GetCircuitBreakerPolicy());

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

// ── Análise consultiva de notas fiscais com Groq ─────────────────────────────
builder.Services.Configure<GroqOptions>(options =>
{
    builder.Configuration.GetSection(GroqOptions.SectionName).Bind(options);

    var environmentApiKey = builder.Configuration["GROQ_API_KEY"];
    if (!string.IsNullOrWhiteSpace(environmentApiKey))
        options.ApiKey = environmentApiKey;
});

var groqTimeoutSeconds = Math.Clamp(
    builder.Configuration.GetValue<int?>("Groq:TimeoutSeconds") ?? 8,
    3,
    30);

builder.Services.AddHttpClient<IInvoiceAiAnalyzer, GroqInvoiceAiAnalyzer>(client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    client.Timeout = TimeSpan.FromSeconds(groqTimeoutSeconds);
});

// ── MassTransit (RabbitMQ + EF Core Outbox) ────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    // Outbox Pattern: usa o BillingDbContext para persistir a mensagem
    // atomicamente junto com o SaveChangesAsync da Nota Fiscal.
    x.AddEntityFrameworkOutbox<BillingDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq")
            ?? "amqp://guest:guest@localhost:5672");

        cfg.ConfigureEndpoints(ctx);
    });
});

// ── Health Checks ────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BillingDbContext>(name: "postgres");

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
        throw;
    }
}

// ── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseGlobalExceptionHandler(); // Deve ser o primeiro middleware
app.UseApiErrorStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Billing Service v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
