using Authentication.Application;
using Data;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Domain.Repositories;
using Microsoft.AspNetCore.Authentication.OAuth;
using Authentication.Domain.Repositories;
using Authenitcation.Infrastructure.Repositories;
using Infrastructure.Repositories;
using Authentication.Infrastructure.Repositories;
using Authentication.Infrastructure.Services;
using Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Distributed Cache Implementation
builder.Services.AddDistributedMemoryCache(); // This resolves the IDistributedCache dependency

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AutheDbContext>(options => 
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));    
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<DistributedCacheService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IUserPropertyRepository, UserPropertyRepository>();
builder.Services.AddScoped<IOAuthTokenRepository, OAuthTokenRepository>();
builder.Services.AddScoped<OAuthService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CheckboxCaptchaService>();
builder.Services.AddScoped<PuzzleCaptchaService>();
builder.Services.AddHttpContextAccessor();
services.AddHttpClient<KavenegarOtpService>();
services.AddScoped<KavenegarOtpService>();

// Remove or comment out the old OtpService registration
// services.AddScoped<OtpService>();

var app = builder.Build();

app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}