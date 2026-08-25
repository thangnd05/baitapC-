using Microsoft.EntityFrameworkCore;
using Npgsql;
using Week2.School.Api.Data;
using Week2.School.Api.Infrastructure;
using Week2.School.Api.Services;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "Week 2 School API", Version = "v1" }));

builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseNpgsql(ResolveConnectionString(builder.Configuration)));

builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IStudentService, StudentService>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Week 2 School API v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var direct = configuration.GetConnectionString("SchoolDb");
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
                $"Thiếu biến môi trường '{key}'. Copy .env.example thành .env rồi điền giá trị, " +
                "hoặc đặt biến môi trường trước khi chạy.");
}
