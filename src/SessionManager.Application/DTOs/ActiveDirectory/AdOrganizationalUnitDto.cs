namespace SessionManager.Application.DTOs.ActiveDirectory;

public sealed record AdOrganizationalUnitDto(
    string Name,
    string DistinguishedName,
    string CanonicalName,
    int Depth);
