using ClipFunc.Configuration;
using ClipFunc.Invocables;
using Coravel;
using Serilog;

namespace ClipFunc.Extensions;

public static class HostExtensions
{
    public static IHost UseScheduling(this IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        host.Services.UseScheduler(s =>
            {
                var secondsBetweenRuns = host.Services
                    .GetRequiredService<IConfiguration>()
                    .GetValue<int?>(ConfigurationKeys.SecondsBetweenRuns);

                if (secondsBetweenRuns is null)
                    secondsBetweenRuns = ConfigurationDefaults.DefaultSecondsBetweenRuns;
                else
                    secondsBetweenRuns = secondsBetweenRuns < ConfigurationDefaults.DefaultSecondsBetweenRuns
                        ? ConfigurationDefaults.DefaultSecondsBetweenRuns
                        : secondsBetweenRuns;

                logger.LogInformation("Scheduling run for every {seconds} seconds", secondsBetweenRuns);

                s.Schedule<HandleClipsInvocable>()
                    .EverySeconds(secondsBetweenRuns.Value)
                    .PreventOverlapping(nameof(HandleClipsInvocable));
            })
            .LogScheduledTaskProgress()
            .OnError(exception =>
            {
                logger.LogError(exception, "Unhandled exception during scheduled job");
            });

        return host;
    }
}