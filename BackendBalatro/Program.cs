using System.Text.Json.Serialization;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;

var builder = WebApplication.CreateBuilder(args);

// 1. DAFTARKAN CONTROLLERS DENGAN JSON OPTIONS
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 2. DAFTARKAN DEPENDENCY INJECTION SERVICES
builder.Services.AddSingleton<IPokerHandEvaluator, PokerHandEvaluator>();
builder.Services.AddSingleton<IScoringService, ScoringService>();
builder.Services.AddSingleton<IShopService, ShopService>();
builder.Services.AddSingleton<IConsumableEffectHandler, ConsumableEffectHandler>();
builder.Services.AddSingleton<IGameSessionService, GameSessionService>();

// 3. DAFTARKAN SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BalatroGila Core Game Engine API",
        Version = "v1",
        Description = "RESTful Web API untuk Core Game Engine Dopamine Rush (Balatro Roguelike Card Game)"
    });
});

// 4. DAFTARKAN CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// 5. MIDDLEWARE PIPELINE
app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BalatroGila API v1");
        c.RoutePrefix = "swagger";
    });
}

app.MapGet("/api/status", () => Results.Ok(new
{
    Message = "Backend BalatroGila is running!",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
}));

app.MapControllers();

app.Run();

public partial class Program { }