using System.Net;
using System.Net.Http.Json;
using AiAgentLab.Api.Models.Health;

namespace AiAgentLab.Tests.Controllers;

public sealed class HealthControllerTests : IClassFixture<MockProviderWebApplicationFactory>
{
    private readonly MockProviderWebApplicationFactory _factory;

    public HealthControllerTests(MockProviderWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body!.Status);
    }
}
