using System.Text.Json.Serialization;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddSingleton<IPokerHandEvaluator, PokerHandEvaluator>();
builder.Services.AddSingleton<IScoringService, ScoringService>();
builder.Services.AddSingleton<IShopService, ShopService>();
builder.Services.AddSingleton<IConsumableEffectHandler, ConsumableEffectHandler>();
builder.Services.AddSingleton<IGameSessionService, GameSessionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BalatroGila Core Game Engine API",
        Version = "v1",
        Description = "RESTful Web API untuk Core Game Engine Balatro Roguelike Card Game"
    });
});

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

//public partial class Program { }