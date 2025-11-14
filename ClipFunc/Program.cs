using ClipFunc.Configuration;
using ClipFunc.Extensions;

namespace ClipFunc;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder
            .SetupConfiguration(args)
            .SetupLogging()
            .AddChannelConfiguration()
            .AddTwitchCredentials()
            .AddClipFuncDatabase()
            .AddScheduling();

        var host = builder.Build().UseScheduling();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var version = typeof(Program).Assembly.GetName().Version ?? new Version(0, 0, 0, 0);
        logger.LogInformation("Starting up ClipFunc v{version}", version.ToString(3));

        var channelConfiguration = host.Services.GetRequiredService<ChannelConfiguration>();
        logger.LogInformation("Using channel configuration: {@channel_configuration}", channelConfiguration);

        host.Run();
    }
}