using System.ComponentModel.DataAnnotations;

namespace SessionManager.Agent.Windows.Options;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    [Required]
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    [Required]
    public string ApiKey { get; set; } = "CHANGE_THIS_AGENT_API_KEY";

    public string? AgentId { get; set; }
    public string? ServerName { get; set; }
    public string? Hostname { get; set; }

    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int PollIntervalSeconds { get; set; } = 5;
    public int CommandTimeoutSeconds { get; set; } = 120;
    public bool SupportsRds { get; set; } = true;
    public bool SupportsAd { get; set; }

    [Required]
    public string DataDirectory { get; set; } = @"C:\ProgramData\SessionManagerAgent\data";

    public int MaxResultOutputLength { get; set; } = 4000;
}
