/*
 * ProgramTests.cs - Integration Tests for Application Startup
 *
 * These tests verify the application's dependency registrations, middleware,
 * JSON configuration, CORS policy, Swagger availability, endpoint mapping,
 * and HTTP application startup.
 */

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackendBalatro.Enums;
using BackendBalatro.Services.Consumables;
using BackendBalatro.Services.Evaluators;
using BackendBalatro.Services.Sessions;
using BackendBalatro.Services.Shop;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BackendBalatro.Tests;

/// <summary>
/// Integration tests for the HTTP application configured in Program.cs.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ProgramTests
{
    /// <summary>
    /// Verifies that the status endpoint returns the expected application
    /// metadata and a current UTC timestamp.
    /// </summary>
    [Test]
    public async Task StatusEndpoint_WhenApplicationStarts_ReturnsExpectedMetadata()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var earliestExpectedTimestamp = DateTime.UtcNow.AddSeconds(-1);

        var response = await client.GetAsync("/api/status");
        var payload = await response.Content.ReadFromJsonAsync<StatusResponse>();

        var latestExpectedTimestamp = DateTime.UtcNow.AddSeconds(1);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(payload, Is.Not.Null);
            Assert.That(
                payload!.Message,
                Is.EqualTo("Backend BalatroGila is running!"));
            Assert.That(payload.Version, Is.EqualTo("1.0.0"));
            Assert.That(
                payload.Timestamp,
                Is.InRange(
                    earliestExpectedTimestamp,
                    latestExpectedTimestamp));
        });
    }

    /// <summary>
    /// Verifies that all core application services are registered using their
    /// expected concrete implementations.
    /// </summary>
    [Test]
    public void DependencyInjection_WhenApplicationStarts_ResolvesCoreServices()
    {
        using var factory = CreateFactory("Production");

        var pokerHandEvaluator =
            factory.Services.GetRequiredService<IPokerHandEvaluator>();
        var scoringService =
            factory.Services.GetRequiredService<IScoringService>();
        var shopService =
            factory.Services.GetRequiredService<IShopService>();
        var consumableHandler =
            factory.Services.GetRequiredService<IConsumableEffectHandler>();
        var sessionService =
            factory.Services.GetRequiredService<IGameSessionService>();

        Assert.Multiple(() =>
        {
            Assert.That(
                pokerHandEvaluator,
                Is.TypeOf<PokerHandEvaluator>());

            Assert.That(
                scoringService,
                Is.TypeOf<ScoringService>());

            Assert.That(
                shopService,
                Is.TypeOf<ShopService>());

            Assert.That(
                consumableHandler,
                Is.TypeOf<ConsumableEffectHandler>());

            Assert.That(
                sessionService,
                Is.TypeOf<GameSessionService>());
        });
    }

    /// <summary>
    /// Verifies that core services use singleton lifetime across dependency
    /// injection scopes.
    /// </summary>
    [Test]
    public void DependencyInjection_AcrossScopes_ReturnsSameSingletonInstances()
    {
        using var factory = CreateFactory("Production");
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var firstScoringService =
            firstScope.ServiceProvider.GetRequiredService<IScoringService>();
        var secondScoringService =
            secondScope.ServiceProvider.GetRequiredService<IScoringService>();

        var firstShopService =
            firstScope.ServiceProvider.GetRequiredService<IShopService>();
        var secondShopService =
            secondScope.ServiceProvider.GetRequiredService<IShopService>();

        var firstSessionService =
            firstScope.ServiceProvider.GetRequiredService<IGameSessionService>();
        var secondSessionService =
            secondScope.ServiceProvider.GetRequiredService<IGameSessionService>();

        Assert.Multiple(() =>
        {
            Assert.That(
                secondScoringService,
                Is.SameAs(firstScoringService));

            Assert.That(
                secondShopService,
                Is.SameAs(firstShopService));

            Assert.That(
                secondSessionService,
                Is.SameAs(firstSessionService));
        });
    }

    /// <summary>
    /// Verifies that JSON options serialize enums as strings and deserialize
    /// property names without case sensitivity.
    /// </summary>
    [Test]
    public void JsonOptions_WhenResolved_UseStringEnumsAndCaseInsensitiveProperties()
    {
        using var factory = CreateFactory("Production");

        var jsonOptions = factory.Services
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .JsonSerializerOptions;

        var serializedPhase = JsonSerializer.Serialize(
            GameStatePhase.Playing,
            jsonOptions);

        var deserializedPhase = JsonSerializer.Deserialize<GameStatePhase>(
            "\"playing\"",
            jsonOptions);

        var deserializedObject = JsonSerializer.Deserialize<JsonProbe>(
            """
            {
                "VALUE": "case-insensitive"
            }
            """,
            jsonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(
                jsonOptions.PropertyNameCaseInsensitive,
                Is.True);

            Assert.That(
                serializedPhase,
                Is.EqualTo("\"Playing\""));

            Assert.That(
                deserializedPhase,
                Is.EqualTo(GameStatePhase.Playing));

            Assert.That(
                deserializedObject,
                Is.Not.Null);

            Assert.That(
                deserializedObject!.Value,
                Is.EqualTo("case-insensitive"));
        });
    }

    /// <summary>
    /// Verifies that each configured React development origin is permitted by
    /// the application's CORS policy.
    /// </summary>
    [TestCase("http://localhost:5173")]
    [TestCase("http://localhost:3000")]
    [TestCase("http://127.0.0.1:5173")]
    public async Task CorsPolicy_ConfiguredOrigin_AllowsPreflightRequest(
        string origin)
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/status");

        request.Headers.Add("Origin", origin);
        request.Headers.Add(
            "Access-Control-Request-Method",
            "GET");
        request.Headers.Add(
            "Access-Control-Request-Headers",
            "X-Session-Id");

        var response = await client.SendAsync(request);

        var containsOriginHeader = response.Headers.TryGetValues(
            "Access-Control-Allow-Origin",
            out var allowedOrigins);

        Assert.Multiple(() =>
        {
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.NoContent));

            Assert.That(containsOriginHeader, Is.True);

            Assert.That(
                allowedOrigins,
                Does.Contain(origin));
        });
    }

    /// <summary>
    /// Verifies that an origin outside the configured allowlist does not
    /// receive a CORS allow-origin header.
    /// </summary>
    [Test]
    public async Task CorsPolicy_UnconfiguredOrigin_DoesNotAllowOrigin()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/status");

        request.Headers.Add(
            "Origin",
            "https://untrusted.example.com");

        request.Headers.Add(
            "Access-Control-Request-Method",
            "GET");

        var response = await client.SendAsync(request);

        Assert.That(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            Is.False);
    }

    /// <summary>
    /// Verifies that Swagger JSON and its user interface are available in the
    /// Development environment.
    /// </summary>
    [Test]
    public async Task Swagger_WhenEnvironmentIsDevelopment_IsAvailable()
    {
        using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        var documentResponse =
            await client.GetAsync("/swagger/v1/swagger.json");

        var userInterfaceResponse =
            await client.GetAsync("/swagger/index.html");

        Assert.Multiple(() =>
        {
            Assert.That(
                documentResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));

            Assert.That(
                documentResponse.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/json"));

            Assert.That(
                userInterfaceResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>
    /// Verifies that Swagger endpoints are not exposed outside the Development
    /// environment.
    /// </summary>
    [Test]
    public async Task Swagger_WhenEnvironmentIsProduction_IsNotAvailable()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var documentResponse =
            await client.GetAsync("/swagger/v1/swagger.json");

        var userInterfaceResponse =
            await client.GetAsync("/swagger/index.html");

        Assert.Multiple(() =>
        {
            Assert.That(
                documentResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));

            Assert.That(
                userInterfaceResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// Verifies that attribute-routed controllers are mapped into the
    /// application pipeline.
    /// </summary>
    [Test]
    public async Task ControllerRouting_StartGameEndpoint_IsMapped()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var sessionId = $"program-test-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync(
            "/api/game/start",
            new
            {
                SessionId = sessionId,
                PlayerName = "Program integration test"
            });

        Assert.Multiple(() =>
        {
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));

            Assert.That(
                response.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/json"));
        });
    }

    /// <summary>
    /// Verifies that an unmapped route passes through the pipeline and returns
    /// HTTP 404.
    /// </summary>
    [Test]
    public async Task Routing_UnknownEndpoint_ReturnsNotFound()
    {
        using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/endpoint-that-does-not-exist");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static EnvironmentWebApplicationFactory CreateFactory(
        string environment)
    {
        return new EnvironmentWebApplicationFactory(environment);
    }

    private sealed class EnvironmentWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _environment;

        public EnvironmentWebApplicationFactory(string environment)
        {
            _environment = environment;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environment);
        }
    }

    public sealed record StatusResponse(
        string Message,
        DateTime Timestamp,
        string Version);

    public sealed class JsonProbe
    {
        public string? Value { get; init; }
    }
}