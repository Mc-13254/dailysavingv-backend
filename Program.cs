using System.Text;
using DailySavingV.API.Data;
using DailySavingV.API.Services;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC / Swagger ----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DailySavingV API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---- EF Core / SQL Server ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Current-user (per-request) + application services ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICollectorPerformanceService, CollectorPerformanceService>();
builder.Services.AddScoped<DailySavingV.API.Services.INotificationService, DailySavingV.API.Services.NotificationService>();
builder.Services.AddScoped<DailySavingV.API.Services.IJournalPostingService, DailySavingV.API.Services.JournalPostingService>();
builder.Services.AddScoped<DailySavingV.API.Services.IFraudDetectionService, DailySavingV.API.Services.FraudDetectionService>();
builder.Services.AddScoped<DailySavingV.API.Services.NumberingService>();

// ---- JWT Authentication ----
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Without this, ASP.NET Core silently remaps the "role" claim to the long
    // ClaimTypes.Role URI when parsing the token, which makes every
    // RequireClaim("role", ...) policy below fail to find it -> mysterious
    // 403s with no server-side exception (the claim literally isn't there
    // under the name the policy is looking for).
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireClaim("roleType", "ADMIN"));
    options.AddPolicy("SupervisorOrAdmin", p => p.RequireClaim("roleType", "ADMIN", "SUPERVISOR"));

    // Accounting Management RBAC. ACCOUNTANT / AUDITOR / FINANCE_OFFICER are not
    // yet seeded as Role codes in this system — create them via Role Management
    // if you want dedicated accounting staff; these policies already recognize
    // them the moment they exist, no code change needed.
    options.AddPolicy("AccountingView", p => p.RequireClaim("roleType", "ADMIN", "MANAGER", "SUPERVISOR", "ACCOUNTANT", "AUDITOR", "FINANCE_OFFICER"));
    options.AddPolicy("AccountingAdmin", p => p.RequireClaim("roleType", "ADMIN", "ACCOUNTANT", "FINANCE_OFFICER"));
});

// ---- CORS (adjust the allowed origin to your deployed frontend URL) ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration["Frontend:Url"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Logs every unhandled exception to ErrorLogs (Security > Error Logs / System
// Health) AND converts common business-rule exceptions into a proper JSON
// {message} response — without this, every `throw new
// InvalidOperationException(...)` used throughout the controllers (the
// primary way business-rule violations are reported) surfaced as a bare,
// message-less 500 to the frontend.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (ex is not (System.Threading.Tasks.TaskCanceledException or OperationCanceledException))
    {
        // The useful detail for DbUpdateException/SqlException is almost always
        // in the InnerException, not ex.Message itself ("An error occurred while
        // saving..." tells you nothing on its own).
        var fullMessage = ex.InnerException != null ? $"{ex.Message} | Inner: {ex.InnerException.Message}" : ex.Message;

        try
        {
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DailySavingV.API.Data.AppDbContext>();
            db.ErrorLogs.Add(new DailySavingV.API.Entities.ErrorLog
            {
                Message = fullMessage,
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace,
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method,
                CodeUser = context.User?.FindFirst("codeUser")?.Value,
                IPAddress = context.Connection.RemoteIpAddress?.ToString()
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // Never let logging failures mask the original exception.
        }

        if (context.Response.HasStarted) throw;

        var (status, message) = ex switch
        {
            InvalidOperationException => (400, ex.Message),
            KeyNotFoundException => (404, ex.Message),
            UnauthorizedAccessException => (401, ex.Message),
            ArgumentException => (400, ex.Message),
            _ => (500, "Une erreur interne est survenue. L'équipe technique a été notifiée.")
        };

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves /uploads/clients/* (KYC documents, photos, signatures)
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
