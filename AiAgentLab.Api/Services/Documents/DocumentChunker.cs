using AiAgentLab.Api.Core.Configuration;
using Microsoft.Extensions.Options;
using TiktokenSharp;

namespace AiAgentLab.Api.Services.Documents;

/// <summary>
/// Splits document text into overlapping token-sized chunks using TiktokenSharp, so
/// chunk boundaries reflect what the LLM actually sees rather than a character-count guess.
/// </summary>
public sealed class DocumentChunker
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static TikToken? _tikToken;

    private readonly DocumentIngestionSettings _settings;

    public DocumentChunker(IOptions<DocumentIngestionSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<string>> ChunkAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        var tikToken = await GetTikTokenAsync();
        var tokens = tikToken.Encode(text);

        var chunkSize = _settings.ChunkSizeTokens;
        var overlap = _settings.ChunkOverlapTokens;
        var stride = Math.Max(1, chunkSize - overlap);

        var chunks = new List<string>();
        var start = 0;
        while (start < tokens.Count)
        {
            var length = Math.Min(chunkSize, tokens.Count - start);
            var chunkTokens = tokens.GetRange(start, length);
            chunks.Add(tikToken.Decode(chunkTokens));

            if (start + length >= tokens.Count)
                break;

            start += stride;
        }

        return chunks;
    }

    // TikToken's model/vocab load is async and shared across the process, so we cache
    // a single instance instead of re-initializing it for every document.
    private static async Task<TikToken> GetTikTokenAsync()
    {
        if (_tikToken is not null)
            return _tikToken;

        await InitLock.WaitAsync();
        try
        {
            _tikToken ??= await TikToken.EncodingForModelAsync("gpt-3.5-turbo");
            return _tikToken;
        }
        finally
        {
            InitLock.Release();
        }
    }
}
