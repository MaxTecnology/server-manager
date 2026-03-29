namespace SessionManager.Application.Interfaces.Security;

public interface IAgentCommandProtector
{
    string ProtectCommand(string plainCommandText);
}
