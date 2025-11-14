using ClipFunc.Configuration;
using ClipFunc.DataContext;
using ClipFunc.DataContext.Models;
using ClipFunc.Invocables;
using Coravel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;

namespace ClipFunc.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static HostApplicationBuilder SetupConfiguration(this HostApplicationBuilder builder, string[] args)
    {
        builder.Configuration
            .AddJsonFile($"{AppContext.BaseDirectory}appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"{AppContext.BaseDirectory}appsettings.Logging.json", optional: false, reloadOnChange: true);

        builder.Configuration
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        return builder;
    }

    public static HostApplicationBuilder SetupLogging(this HostApplicationBuilder builder)
    {
        Log.Logger = BuildLoggerConfiguration(builder).CreateLogger();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddSerilog(Log.Logger);
        return builder;
    }

    private static LoggerConfiguration BuildLoggerConfiguration(HostApplicationBuilder builder)
    {
        var configurationLogLevel = builder.Configuration.GetValue<string?>(ConfigurationKeys.LogLevel);

        if (string.IsNullOrWhiteSpace(configurationLogLevel) ||
            !Enum.TryParse(configurationLogLevel, out LogEventLevel logLevel))
        {
            logLevel = LogEventLevel.Information;
        }

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.WithSensitiveDataMasking(options =>
            {
                var first3Last3MaskOptions = new MaskOptions
                {
                    PreserveLength = true,
                    ShowFirst = 3,
                    ShowLast = 3
                };

                options.MaskProperties.AddRange([
                    new MaskProperty
                    {
                        Name = nameof(TwitchCredentials.ClientId), Options = first3Last3MaskOptions
                    },
                    new MaskProperty
                    {
                        Name = nameof(TwitchCredentials.ClientId), Options = first3Last3MaskOptions
                    },
                    new MaskProperty
                    {
                        Name = nameof(AccessTokenModel.AccessToken), Options = first3Last3MaskOptions
                    },
                    new MaskProperty
                    {
                        Name = "*Url", Options = new MaskOptions
                        {
                            PreserveLength = false,
                            WildcardMatch = true
                        }
                    }
                ]);
            })
            .MinimumLevel.Warning()
            .MinimumLevel.Override(nameof(Program), logLevel)
            .MinimumLevel.Override(nameof(ClipFunc), logLevel)
            .MinimumLevel.Override(nameof(Invocables), logLevel)
            .MinimumLevel.Override(nameof(MigrationWorker), logLevel)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", logLevel)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} <s:{SourceContext}> {Level:u3}] {Message:lj}{NewLine}{Exception}");

        return loggerConfiguration;
    }

    public static HostApplicationBuilder AddChannelConfiguration(this HostApplicationBuilder builder)
    {
        var broadcasterId = builder.Configuration.GetValue<string?>(ConfigurationKeys.BroadcasterId);
        var webhookUrl = builder.Configuration.GetValue<string?>(ConfigurationKeys.DiscordWebhookUrl);
        var webhookProfileName = builder.Configuration.GetValue<string?>(ConfigurationKeys.DiscordWebhookProfileName)
                                 ?? ConfigurationDefaults.DefaultWebhookProfileName;

        var preventWebhookOnFirstLoad =
            builder.Configuration.GetValue<bool?>(ConfigurationKeys.PreventWebhookOnFirstLoad)
            ?? ConfigurationDefaults.DefaultPreventWebhookOnFirstLoad;

        var channelConfiguration =
            new ChannelConfiguration(broadcasterId, webhookUrl, webhookProfileName, preventWebhookOnFirstLoad);

        builder.Services.TryAddSingleton(channelConfiguration);

        return builder;
    }

    public static HostApplicationBuilder AddTwitchCredentials(this HostApplicationBuilder builder)
    {
        var clientId = builder.Configuration.GetValue<string>(ConfigurationKeys.TwitchClientId);
        var clientSecret = builder.Configuration.GetValue<string>(ConfigurationKeys.TwitchClientSecret);
        var twitchCredentials = new TwitchCredentials(clientId, clientSecret);
        builder.Services.TryAddSingleton(twitchCredentials);

        return builder;
    }

    public static HostApplicationBuilder AddClipFuncDatabase(this HostApplicationBuilder builder)
    {
        var databasePath = builder.Configuration.GetValue<string?>(ConfigurationKeys.DatabasePath)
                           ?? ConfigurationDefaults.DefaultDatabasePath;

        if (!TryCreateDirectory(databasePath))
            throw new DirectoryNotFoundException($"Directory for `{databasePath}` not found and not created");

        builder.Services.AddDbContextFactory<ClipFuncContext>(optionsBuilder =>
        {
            try
            {
                optionsBuilder
                    .UseSqlite($"Data Source={databasePath}")
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                if (!builder.Environment.IsProduction())
                    optionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Unhandled exception during EntityFrameworkCore initialization");
                throw;
            }
        });

        builder.Services.AddHostedService<MigrationWorker>();

        return builder;
    }

    private static bool TryCreateDirectory(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            Log.Logger.Error("databasePath is empty");
            return false;
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            Log.Logger.Error("Unable to get directory from databasePath: `{databasePath}`", databasePath);
            return false;
        }

        if (Directory.Exists(directory))
            return true;

        try
        {
            var di = Directory.CreateDirectory(directory);
            Log.Logger.Information("Directory created: `{directory}`", di.FullName);

            return true;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Unable to create directory");
            return false;
        }
    }

    public static HostApplicationBuilder AddScheduling(this HostApplicationBuilder builder)
    {
        builder.Services.AddTransient<HandleClipsInvocable>();
        builder.Services.AddScheduler();

        return builder;
    }
}