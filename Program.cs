using Hangfire;
using Hangfire.SqlServer;
using KinectCare.API.Data;
using KinectCare.API.Helpers;
using KinectCare.API.Hubs;
using KinectCare.API.Middleware;
using KinectCare.API.Services;
using KinectCare.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

// ── JWT Authentication ────────────────────────────────────
var jwt = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["SecretKey"]!))
        };

        // SignalR يحتاج التوكن من query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// ── Authorization ─────────────────────────────────────────
builder.Services.AddAuthorization();

// ── Hangfire ──────────────────────────────────────────────
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration
        .GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            TryAutoDetectSchemaDependentOptions = false
        }
        ));
builder.Services.AddHangfireServer();

// ── SignalR ───────────────────────────────────────────────
builder.Services.AddSignalR();


// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "https://taheel.runasp.net"
        )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── Controllers + OpenAPI (.NET 10 built-in) ──────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── HttpContextAccessor ───────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>(); 

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChildService, ChildService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<AIBridgeService>();

builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRehabPlanService, RehabPlanService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<PdfService>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "KinectCare API";
    options.Theme = ScalarTheme.Purple;
});

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// ── تأكد أن قاعدة البيانات موجودة ─────────────────────────
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.Migrate();
//}


using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Migration failed");
    }
}
app.Run();