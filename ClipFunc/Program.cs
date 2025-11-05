using ClipFunc;
using ClipFunc.Configuration;
using ClipFunc.DataContext;
using ClipFunc.Invocables;
using Coravel;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.Logging.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddSerilog(configuration => { configuration.ReadFrom.Configuration(builder.Configuration); });

builder.Services.AddOptions<ClipFuncOptions>()
    .Bind(builder.Configuration.GetSection(nameof(ClipFuncOptions)))
    .ValidateDataAnnotations();

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
        Console.WriteLine(e);
        throw;
    }
});

builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddTransient<HandleClipsInvocable>();
builder.Services.AddScheduler();

var host = builder.Build();

host.Services.UseScheduler(s =>
    {
        s.Schedule<HandleClipsInvocable>()
            .EveryTenSeconds()
            .PreventOverlapping(nameof(HandleClipsInvocable));
    })
    .LogScheduledTaskProgress()
    .OnError(exception =>
    {
        host.Services.GetRequiredService<ILogger<Program>>()
            .LogError(exception, "Unhandled exception during scheduled job");
    });

host.Run();