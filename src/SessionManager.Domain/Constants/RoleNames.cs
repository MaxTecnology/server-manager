namespace SessionManager.Domain.Constants;

public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string Operator = "Operator";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Administrator,
        Operator
    };
}
