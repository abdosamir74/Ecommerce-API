using Application.Authorization;
using Application.Common.Interfaces;
using Application.Helpers;
using Domain.Entities.Identity;
using Ecommerce.Application.Helpers;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Middleware;
using Hangfire;
using Infrastructure.Authorization;
using Infrastructure.Data;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. ≈⁄œ«œ Serilog ··‹ Logging «·ÂÌﬂ·Ì
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: Serilog.RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true
    )
    .CreateLogger();

builder.Host.UseSerilog();

// 2. ≈⁄œ«œ OpenTelemetry Tracing & Metrics
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    });

//  ”ÃÌ· Auditing Interceptor
builder.Services.AddScoped<UpdateAuditableEntitiesInterceptor>();

//  ”ÃÌ· ApplicationDbContext „⁄ «·‹ Interceptor
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<UpdateAuditableEntitiesInterceptor>();

    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
    ).AddInterceptors(interceptor);
});

//  ”ÃÌ· AppIdentityDbContext
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(AppIdentityDbContext).Assembly.FullName)
    )
);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

//  ”ÃÌ· Response Caching Service
builder.Services.AddResponseCaching();

//  ”ÃÌ· Rate Limiting Service (Õ„«Ì… «·‹ APIs)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// OpenAPI / Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Ecommerce API", Version = "v1" });

    var securitySchema = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securitySchema);

    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securitySchema, new[] { "Bearer" } }
    };

    c.AddSecurityRequirement(securityRequirement);
});

//  ”ÃÌ· UnitOfWork Ê Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IOrderService, OrderService>();

//  ”ÃÌ· AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfiles>();
    cfg.AddProfile<OrderMappingProfile>();
});

builder.Services.AddScoped<ProductUrlResolver>();

// ≈⁄œ«œ «·« ’«· »‹ Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis ConnectionString is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(c =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

//  ”ÃÌ· BasketRepository
builder.Services.AddScoped<IBasketRepository, BasketRepository>();

//  ”ÃÌ· Œœ„«  Identity ·œ⁄„ Roles
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequiredLength = 6;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppIdentityDbContext>()
.AddSignInManager<SignInManager<AppUser>>();

//  ”ÃÌ· JWT Authentication
var jwtKey = builder.Configuration["JWT:Key"]
    ?? throw new InvalidOperationException("JWT:Key is missing from appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateIssuer = true,
            ValidateAudience = false
        };
    });

// Dynamic Authorization Registration
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

//  ”ÃÌ· TokenService Ê Stripe
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
builder.Services.AddScoped<IPaymentService, PaymentService>();

//  ”ÃÌ· Email Service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// ---  ”ÃÌ· Health Checks ---
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

builder.Services.AddScoped<ICouponService, CouponService>();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: defaultConnectionString,
        name: "sqlserver",
        tags: new[] { "db", "data" })
    .AddRedis(
        redisConnectionString: redisConnectionString,
        name: "redis",
        tags: new[] { "cache", "redis" });

//  ”ÃÌ· Hangfire »«” Œœ«„ SQL Server
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(defaultConnectionString));

//  ‘€Ì· «·‹ Hangfire Server
builder.Services.AddHangfireServer();




var app = builder.Build();

// Middleware Pipeline
app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePagesWithReExecute("/errors/{0}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseResponseCaching();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

//  ›⁄Ì· Hangfire Dashboard ·„—«ﬁ»… «·‹ Background Jobs
app.UseHangfireDashboard("/hangfire");

// ≈÷«›… Recurring Job Ì „  ‰›Ì–Â ÌÊ„Ì« «·”«⁄… 12 „‰ ’› «··Ì·
RecurringJob.AddOrUpdate<IBasketRepository>(
    "cleanup-expired-baskets",
    repo => repo.CleanUpExpiredBasketsAsync(),
    Cron.Daily
);

// --- ≈÷«›… Custom JSON Response ··‹ Health Checks Endpoint ---
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.MapControllers();

//  ÿ»Ìﬁ Migrations Ê Seeding „⁄ Retry Policy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    int maxRetries = 10;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var identityContext = services.GetRequiredService<AppIdentityDbContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();
            await identityContext.Database.MigrateAsync();

            await StoreContextSeed.SeedAsync(context);
            await StoreContextSeed.SeedUsersAsync(userManager, roleManager);

            // ·«“„   ‰›– »⁄œ SeedUsersAsync „»«‘—…° ·√‰Â« „Õ «Ã… «·‹ "Admin" Role ÌﬂÊ‰
            // „ÊÃÊœ »«·›⁄· ⁄‘«‰  ﬁœ—  ÷Ì›·Â «·‹ Permission Claims » «⁄ Â«
            await PermissionSeeder.SeedAsync(roleManager);

            logger.LogInformation("Database connected and migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogWarning(ex, "Attempt {RetryCount}/{MaxRetries}: SQL Server is not ready yet. Retrying in 5 seconds...", retryCount, maxRetries);

            if (retryCount >= maxRetries)
            {
                logger.LogError(ex, "Could not connect to SQL Server after multiple attempts.");
                throw;
            }

            await Task.Delay(5000);
        }
    }
}

app.Run();