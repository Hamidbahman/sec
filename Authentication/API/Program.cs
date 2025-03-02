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
using Application;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("Appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder=>
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
builder..AddSingleton<DistributedCacheService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IUserPropertyRepository, UserPropertyRepository>();
builder.Services.AddScoped<IOAuthTokenRepository, OauthTokenRepository>(); // Ensure IOAuthTokenRepository is registered
builder.Services.AddScoped<OAuthService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TokenValidationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CheckboxCaptchaService>();
builder.Services.AddScoped<PuzzleCaptchaService>();
builder.Services.AddHttpContextAccessor();



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
