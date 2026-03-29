using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionManager.Agent.Windows;
using SessionManager.Agent.Windows.Options;
using SessionManager.Agent.Windows.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SessionManagerAgent";
});

if (OperatingSystem.IsWindows())
{
    ConfigureWindowsEventLog(builder);
}

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(static options => Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _), "Agent:ApiBaseUrl precisa ser URL valida.")
    .Validate(static options => !string.IsNullOrWhiteSpace(options.ApiKey), "Agent:ApiKey e obrigatorio.")
    .Validate(static options => options.HeartbeatIntervalSeconds >= 5, "Agent:HeartbeatIntervalSeconds deve ser >= 5.")
    .Validate(static options => options.PollIntervalSeconds >= 1, "Agent:PollIntervalSeconds deve ser >= 1.")
    .Validate(static options => options.CommandTimeoutSeconds >= 5, "Agent:CommandTimeoutSeconds deve ser >= 5.")
    .Validate(static options => options.SupportsRds || options.SupportsAd, "Agent: habilite ao menos uma capacidade (SupportsRds ou SupportsAd).")
    .ValidateOnStart();

builder.Services.AddHttpClient<AgentApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AgentOptions>>().Value;
    client.BaseAddress = BuildBaseAddress(options.ApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Remove("X-Agent-Key");
    client.DefaultRequestHeaders.Add("X-Agent-Key", options.ApiKey.Trim());
});

builder.Services.AddSingleton<CommandExecutionService>();
builder.Services.AddSingleton<PendingCommandResultStore>();
builder.Services.AddSingleton<SecureCommandCodec>();
builder.Services.AddHostedService<AgentWorker>();

await builder.Build().RunAsync();

static Uri BuildBaseAddress(string rawApiBaseUrl)
{
    var baseUrl = rawApiBaseUrl.Trim();
    if (!baseUrl.EndsWith('/'))
    {
        baseUrl = $"{baseUrl}/";
    }

    return new Uri(baseUrl, UriKind.Absolute);
}

[SupportedOSPlatform("windows")]
static void ConfigureWindowsEventLog(HostApplicationBuilder builder)
{
#pragma warning disable CA1416
    builder.Logging.AddEventLog(settings =>
    {
        settings.LogName = "Application";
        settings.SourceName = "SessionManagerAgent";
    });
#pragma warning restore CA1416
}
