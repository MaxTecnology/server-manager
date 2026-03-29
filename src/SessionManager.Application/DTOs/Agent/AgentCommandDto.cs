namespace SessionManager.Application.DTOs.Agent;

public sealed record AgentCommandDto(
    Guid Id,
    Guid ServerId,
    string ServerName,
    string Hostname,
    string RequestedBy,
    string CommandText,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? PickedAtUtc,
    DateTime? CompletedAtUtc,
    string? AssignedAgentId,
    string? ResultOutput,
    string? ErrorMessage);
