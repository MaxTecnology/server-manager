using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SessionManager.Application.Interfaces.Security;
using SessionManager.Infrastructure.Options;

namespace SessionManager.Infrastructure.Security;

public sealed class AgentCommandProtector : IAgentCommandProtector
{
    private const string Prefix = "encps:v1";
    private readonly byte[] _encryptionKey;

    public AgentCommandProtector(IOptions<AgentOptions> agentOptions)
    {
        var apiKey = agentOptions.Value.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Agent:ApiKey precisa ser configurado para proteger comandos sensíveis.");
        }

        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
    }

    public string ProtectCommand(string plainCommandText)
    {
        if (string.IsNullOrWhiteSpace(plainCommandText))
        {
            throw new ArgumentException("Comando vazio.", nameof(plainCommandText));
        }

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(plainCommandText);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_encryptionKey, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(':',
            Prefix,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(tag));
    }
}
