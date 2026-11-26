using BdCommerce.Api.Data;
using BdCommerce.Api.Endpoints;
using BdCommerce.Api.Providers;
using BdCommerce.Api.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default is required.");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<DbInitializer>();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddPaymentProviders(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BdCommerce.Api", Version = "v1" }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DbInitializer>().InitializeAsync();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BdCommerce.Api v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/health", () => Results.Text("ok", "text/plain"))
    .WithName("Health")
    .WithTags("Health")
    .Produces<string>(StatusCodes.Status200OK);

app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapPaymentEndpoints();

app.Run();

// Exposed so WebApplicationFactory can reference the entry point.
public partial class Program
{
}
