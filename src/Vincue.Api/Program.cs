using Microsoft.EntityFrameworkCore;
using Vin.Api.Data;
using Vin.Api.Seed;
using Vin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<VinDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VinDb")));

builder.Services.AddScoped<IInventoryAggregationService, InventoryAggregationService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VinDbContext>();
    db.Database.Migrate();
    await DatabaseSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.UseCors("FrontendDev");

app.MapControllers();

app.Run();
