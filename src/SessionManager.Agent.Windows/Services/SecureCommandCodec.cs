using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SessionManager.Agent.Windows.Options;

namespace SessionManager.Agent.Windows.Services;

public sealed class SecureCommandCodec
{
    private const string Prefix = "encps:v1:";
    private readonly byte[] _decryptionKey;

    public SecureCommandCodec(IOptions<AgentOptions> options)
    {
        var apiKey = options.Value.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Agent:ApiKey precisa ser configurado.");
        }

        _decryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
    }

    public bool TryDecodeCommand(string rawCommandText, out string commandText, out string? error)
    {
        commandText = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(rawCommandText))
        {
            error = "Comando vazio.";
            return false;
        }

        if (!rawCommandText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            commandText = rawCommandText;
            return true;
        }

        var payload = rawCommandText[Prefix.Length..];
        var parts = payload.Split(':', StringSplitOptions.None);
        if (parts.Length != 3)
        {
            error = "Formato de comando protegido inválido.";
            return false;
        }

        try
        {
            var nonce = Convert.FromBase64String(parts[0]);
            var ciphertext = Convert.FromBase64String(parts[1]);
            var tag = Convert.FromBase64String(parts[2]);
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(_decryptionKey, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            commandText = Encoding.UTF8.GetString(plaintext);
            if (string.IsNullOrWhiteSpace(commandText))
            {
                error = "Comando protegido decodificado como vazio.";
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            error = "Falha ao decodificar comando protegido.";
            return false;
        }
    }
}
