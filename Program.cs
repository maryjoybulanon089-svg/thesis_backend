using Microsoft.EntityFrameworkCore;
using Npgsql;
using ThesisRepository.Data;
using ThesisRepository.Services;
using ThesisRepository.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Read frontend URL from environment or configuration so deployments can override
var frontendUrl = builder.Configuration["FrontendUrl"]
                 ?? "https://thesis-qrve-im4m9e47r-maryjoybulanon22-4304s-projects.vercel.app";

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ensure all DTOs and models are serialised as camelCase for the frontend
        options.JsonSerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS – allow the Vite dev server, common local ports and the deployed Vercel frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                frontendUrl,
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "https://thesis-73pt.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database – PostgreSQL only. Connection must be provided via environment variable
// ConnectionStrings__DefaultConnection (GetConnectionString("DefaultConnection"))
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    Console.Error.WriteLine("FATAL: ConnectionStrings__DefaultConnection is not set. Set the PostgreSQL connection string in the environment.");
    Environment.Exit(1);
}

// Accept either a full ADO.NET connection string or a postgres:// URI
if (conn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(conn);
    var userInfo = uri.UserInfo.Split(':');
    conn = new NpgsqlConnectionStringBuilder()
    {
        Host = uri.Host,
        Port = uri.Port,
        Username = userInfo.Length > 0 ? userInfo[0] : string.Empty,
        Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
        Database = uri.AbsolutePath.TrimStart('/'),
        Pooling = true,
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    }.ToString();
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(conn));

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
                ?? "YourSuperSecretKeyForThesisRepositorySystem2024!@#$%";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme   = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken            = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(key),
        ValidateIssuer           = false,
        ValidateAudience         = false,
        ClockSkew                = TimeSpan.Zero
    };
});

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IThesisService, ThesisService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Add Background Services
builder.Services.AddHostedService<ThesisAutoDeleteService>();

// ── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure the uploads directory exists for PDF file storage under the app content root
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

// Trust proxy headers (X-Forwarded-For / X-Forwarded-Proto) when deployed behind a reverse proxy
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// Clear the default known networks/proxies so the proxy headers from the platform are accepted
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Apply CORS early so static files and other middleware include the CORS headers
app.UseCors("AllowFrontend");

// Serve uploaded files from /uploads when deployed (e.g. Render)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AdminSessionTimeoutMiddleware>();
app.MapControllers();

// ── Database initialisation ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger  = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // EnsureCreated creates the DB + all tables when the DB does not yet exist.
        // When the SSMS DB already exists this is a no-op.
        context.Database.EnsureCreated();

        // Apply schema patches (add IsApproved, create PasswordResetRequests, etc.)
        // and seed initial data if the Users table is empty.
        DbInitializer.Initialize(context);

        logger.LogInformation("Database initialised successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initialising the database.");
    }
}

// If running on Render (or other PaaS) bind to the PORT environment variable
// Bind to PORT (Render) or ASPNETCORE_URLS when provided by the hosting platform.
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    var url = $"http://0.0.0.0:{portEnv}";
    app.Urls.Clear();
    app.Urls.Add(url);
    Console.WriteLine($"Binding to {url} (from PORT)");
}
else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Console.WriteLine($"Using ASPNETCORE_URLS={Environment.GetEnvironmentVariable("ASPNETCORE_URLS")} ");
}

app.Run();
