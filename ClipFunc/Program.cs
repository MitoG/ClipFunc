using ClipFunc;
using ClipFunc.Configuration;
using ClipFunc.DataContext;
using ClipFunc.Invocables;
using Coravel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Logging.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.AddSerilog(Log.Logger);

{
    var assembly = typeof(Program).Assembly;
    var assemblyName = assembly.GetName();
    var version = assemblyName.Version ?? new Version(0, 0, 0, 0);
    Log.Logger.ForContext<Program>().Information("Starting up ClipFunc v{version}", version.ToString(3));
}

{
    var broadcasterId = builder.Configuration.GetValue<string?>(ConfigurationKeys.BroadcasterId);
    var webhookUrl = builder.Configuration.GetValue<string?>(ConfigurationKeys.DiscordWebhookUrl);
    var webhookProfileName = builder.Configuration.GetValue<string?>(ConfigurationKeys.DiscordWebhookProfileName);

    var preventWebhookOnFirstLoad =
        builder.Configuration.GetValue<bool?>(ConfigurationKeys.PreventWebhookOnFirstLoad) ?? true;

    var channelConfiguration =
        new ChannelConfiguration(broadcasterId, webhookUrl, webhookProfileName, preventWebhookOnFirstLoad);

    builder.Services.TryAddSingleton(channelConfiguration);
}

{
    var clientId = builder.Configuration.GetValue<string>(ConfigurationKeys.TwitchClientId);
    var clientSecret = builder.Configuration.GetValue<string>(ConfigurationKeys.TwitchClientSecret);
    var twitchCredentials = new TwitchCredentials(clientId, clientSecret);
    builder.Services.TryAddSingleton(twitchCredentials);
}

builder.Services.AddDbContextFactory<ClipFuncContext>(optionsBuilder =>
{
    try
    {
        optionsBuilder
            .UseSqlite(connectionString: builder.Configuration.GetConnectionString("ClipFunc"))
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

builder.Services.AddTransient<HandleClipsInvocable>();
builder.Services.AddScheduler();

var host = builder.Build();

host.Services.UseScheduler(s =>
    {
        var secondsBetweenRuns = host.Services
            .GetRequiredService<IConfiguration>()
            .GetValue<int?>(ConfigurationKeys.SecondsBetweenRuns) ?? 10;

        secondsBetweenRuns = secondsBetweenRuns < 10 ? 10 : secondsBetweenRuns;

        Log.Logger.ForContext<Program>().Information("Scheduling run for every {seconds} seconds", secondsBetweenRuns);
        s.Schedule<HandleClipsInvocable>()
            .EverySeconds(secondsBetweenRuns)
            .PreventOverlapping(nameof(HandleClipsInvocable));
    })
    .LogScheduledTaskProgress()
    .OnError(exception =>
    {
        host.Services.GetRequiredService<ILogger<Program>>()
            .LogError(exception, "Unhandled exception during scheduled job");
    });

{
    var channelConfiguration = host.Services.GetRequiredService<ChannelConfiguration>();
    Log.Logger.Information("Using channel configuration: {@channel_configuration}", channelConfiguration);
}

host.Run();