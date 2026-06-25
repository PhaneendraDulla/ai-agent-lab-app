using System.Text.Json;
using AiAgentLab.Api.Tools;

namespace AiAgentLab.Api.Tools
{
    public sealed class GetCurrentDateTool : ITool
    {
        public string Name => "get_current_date";
        public string Description => "Gets the current date and time in America/New_York timezone.";

        public object GetDeclaration()
        {
            return new
            {
                name = Name,
                description = Description,
                parameters = new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            };
        }

        public Task<JsonElement> ExecuteAsync(JsonElement args, CancellationToken cancellationToken)
        {
            // Use Windows-friendly timezone id
            var tzId = TimeZoneInfo.GetSystemTimeZones()
                                  .Any(t => t.Id == "America/New_York") ? "America/New_York" : "Eastern Standard Time";

            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            var result = new
            {
                currentDateTime = now.ToString("o"),
                timezone = "America/New_York"
            };

            var json = JsonSerializer.SerializeToElement(result);
            return Task.FromResult(json);
        }
    }
}