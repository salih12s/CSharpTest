using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TedarikCore.Application;
using TedarikCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
// /health: yalnızca uygulama ayakta mı (veritabanı kontrolü çalışmaz).
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
// /health/ready: bağımlılıklar (veritabanı) hazır mı.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
});

app.Run();

// WebApplicationFactory<Program> kullanabilsin diye (IntegrationTests).
public partial class Program;
