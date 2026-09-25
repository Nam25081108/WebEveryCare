using Microsoft.EntityFrameworkCore;
using EveryCare.Api.Infrastructure.Persistence;
using EveryCare.Api.Services;
using System.Text.Json.Serialization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Chỉ ghi log ra terminal để backend chạy được bằng PowerShell thường trên Windows.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseNetTopologySuite()));
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PartnerSessionService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PartnerAvailabilityService>();
builder.Services.AddScoped<BookingDispatchService>();
builder.Services.AddScoped<BookingLifecycleService>();
builder.Services.AddHostedService<BookingDispatchWorker>();
builder.Services.AddHostedService<BookingLifecycleWorker>();

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var message = exception switch
    {
        PostgresException { SqlState: "28P01" } => "Backend không thể đăng nhập PostgreSQL. Hãy kiểm tra username và password trong ConnectionStrings:DefaultConnection.",
        PostgresException => "PostgreSQL từ chối yêu cầu. Hãy kiểm tra database everycare và chạy migrations.",
        NpgsqlException => "Không thể kết nối PostgreSQL. Hãy kiểm tra PostgreSQL đang chạy và cấu hình kết nối.",
        _ => "Backend không thể xử lý yêu cầu. Vui lòng kiểm tra cửa sổ backend."
    };

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { message });
}));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
    await db.Database.MigrateAsync();
    }
    await DatabaseSeeder.SeedAsync(db);
}

app.Run();
