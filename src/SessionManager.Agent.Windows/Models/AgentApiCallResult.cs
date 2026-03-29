using System.Net;

namespace SessionManager.Agent.Windows.Models;

public sealed record AgentApiCallResult<T>(
    bool Success,
    T? Value,
    string? Error,
    HttpStatusCode StatusCode);
