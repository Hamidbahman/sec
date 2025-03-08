using Authentication.Application;
using Authentication.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Kavenegar;
using Microsoft.Extensions.Logging;
using Authentication.Domain.Repositories;
using Data;
using Authenitcation.Infrastructure.Repositories;
using Domain.Repositories;
using Application;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddDbContext<AutheDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IUserPropertyRepository, UserPropertyRepository>();
builder.Services.AddScoped<OAuthService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TokenValidationService>();
builder.Services.AddScoped<CheckboxCaptchaService>();
builder.Services.AddScoped<PuzzleCaptchaService>();
builder.Services.AddScoped<RecaptchaService>();
builder.Services.AddScoped<IOAuthTokenRepository, OauthTokenRepository>();

builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<KavenegarOptions>(builder.Configuration.GetSection("Kavenegar"));

builder.Services.AddLogging(builder => builder.AddConsole());

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// WeatherForecast record for endpoint
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
