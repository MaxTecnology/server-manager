namespace SessionManager.Application.Common;

public sealed record ActionContext(string OperatorUsername, string? ClientIpAddress);
