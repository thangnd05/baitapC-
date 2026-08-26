using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Infrastructure;
using Week1.Rbac.Api.Services;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC, ProblemDetails, Swagger (co nut Authorize de dan Bearer token)
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "School API - JWT + RBAC", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Dán access token nhận từ POST /api/auth/login"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = []
    });
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
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------------------------------------------------------------------
// Xac thuc: phat va validate JWT
// ---------------------------------------------------------------------------
var jwt = ResolveJwtOptions(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,

            // Mac dinh thu vien cho lech 5 phut, nghia la token het han van duoc chap nhan
            // them 5 phut. Rut xuong 30 giay de quan sat duoc hanh vi het han ngay trong buoi lab.
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            // Chot thuat toan: khong cho token tu xung "alg": "none" hay thuat toan khac.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });

// ---------------------------------------------------------------------------
// Phan quyen: role-based bang [Authorize(Roles=...)], resource-based bang policy
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IAuthorizationHandler, StudentOwnerHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.CanEditStudent, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new StudentOwnerRequirement());
    })
    .AddPolicy(AppPolicies.CanReadStudent, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new StudentOwnerRequirement { ReadOnly = true });
    });

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

// ---------------------------------------------------------------------------
// Thu tu middleware. UseAuthentication PHAI truoc UseAuthorization:
// dat nguoc lai thi moi policy chay tren mot identity rong va endpoint duoc bao ve
// luon tra 401 du token hoan toan hop le.
// (Khong dung UseHttpsRedirection: lab nay chay http-only theo launchSettings.)
// ---------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseSecurityHeaders();

app.UseRouting();

app.UseCors(CorsPolicies.SpaAllowlist);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "School API - JWT + RBAC v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

static JwtOptions ResolveJwtOptions(IConfiguration configuration)
{
    var options = new JwtOptions();
    configuration.GetSection(JwtOptions.SectionName).Bind(options);

    if (string.IsNullOrWhiteSpace(options.SigningKey))
    {
        throw new InvalidOperationException(
            "Thiếu Jwt:SigningKey. Đặt Jwt__SigningKey trong .env hoặc "
            + "dotnet user-secrets set \"Jwt:SigningKey\" \"<chuỗi ngẫu nhiên >= 32 ký tự>\".");
    }

    // HMAC-SHA256 doi khoa toi thieu 32 byte; ngan hon thu vien se nem loi luc ky.
    var keyBytes = Encoding.UTF8.GetByteCount(options.SigningKey);
    if (keyBytes < JwtOptions.MinimumKeyBytes)
    {
        throw new InvalidOperationException(
            $"Jwt:SigningKey chỉ dài {keyBytes} byte, cần tối thiểu {JwtOptions.MinimumKeyBytes} byte.");
    }

    return options;
}

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
