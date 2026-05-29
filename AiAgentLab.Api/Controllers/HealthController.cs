using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Models.Health;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly TimeProvider _timeProvider;

    public HealthController(IOptions<AppSettings> appSettings, TimeProvider timeProvider)
    {
        _appSettings = appSettings.Value;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var response = new HealthResponse
        {
            Status = "Healthy",
            Name = _appSettings.Name,
            Version = _appSettings.Version,
            Timestamp = _timeProvider.GetUtcNow()
        };

        return Ok(response);
    }
}
