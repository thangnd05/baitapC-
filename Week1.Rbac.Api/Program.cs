using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Infrastructure;
using Week1.Rbac.Api.Services;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC, ProblemDetails, Swagger
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Week 1 RBAC API", Version = "v1" });
});

// ---------------------------------------------------------------------------
// Persistence
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(ResolveConnectionString(builder.Configuration)));

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

// ---------------------------------------------------------------------------
// CORS allowlist - liet ke origin cu the, khong dung AllowAnyOrigin
// ---------------------------------------------------------------------------
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173", "http://localhost:3000"];

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicies.SpaAllowlist, policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type")));

// ---------------------------------------------------------------------------
// Rate limit cho endpoint dang nhap
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }

        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(
            """{"title":"Quá nhiều yêu cầu, thử lại sau","status":429}""", ct);
    };

    // Phan hoach theo dia chi goi: neu khong phan hoach, moi client dung chung mot han muc,
    // mot nguoi spam la ca lop bi chan.
    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSecurityHeaders();

app.UseRouting();

app.UseCors(CorsPolicies.SpaAllowlist);
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Week 1 RBAC API v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var direct = configuration.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(direct))
    {
        return direct;
    }

    var connectionString = new NpgsqlConnectionStringBuilder
    {
        Host = Require(configuration, "DB_HOST"),
        Port = int.TryParse(configuration["DB_PORT"], out var port) ? port : 5432,
        Database = Require(configuration, "DB_NAME"),
        Username = Require(configuration, "DB_USER"),
        Password = Require(configuration, "DB_PASSWORD")
    };

    return connectionString.ConnectionString;

    static string Require(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Thiếu biến môi trường '{key}'. Copy .env.example thành .env rồi điền giá trị, "
                + "hoặc đặt biến môi trường trước khi chạy.");
}
