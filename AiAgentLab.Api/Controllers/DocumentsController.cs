using AiAgentLab.Api.Services.Documents;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentLab.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentIngestionService _ingestionService;

    public DocumentsController(IDocumentIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    /// <summary>Ingest all .txt/.md files from the configured documents folder into the vector store.</summary>
    [HttpPost("ingest")]
    public async Task<ActionResult<DocumentIngestionResult>> Ingest(CancellationToken cancellationToken)
    {
        var result = await _ingestionService.IngestAsync(cancellationToken);
        return Ok(result);
    }
}
