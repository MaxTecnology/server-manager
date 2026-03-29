namespace SessionManager.Agent.Windows.Models;

public sealed record AgentIdentity(
    string AgentId,
    string ServerName,
    string Hostname,
    string AgentVersion,
    bool SupportsRds,
    bool SupportsAd);
