namespace BillingService.Api.Configuration;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public int TimeoutSeconds { get; set; } = 8;
}
