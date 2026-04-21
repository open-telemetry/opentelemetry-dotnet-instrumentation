// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using TestApplication.Quartz;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices((cxt, services) =>
    {
#if QUARTZ_3_7_0_OR_GREATER
        services.AddQuartz();
#else
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
        });
#endif
        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });
    }).Build();

var schedulerFactory = builder.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler().ConfigureAwait(false);

var job = JobBuilder.Create<TestJob>()
    .WithIdentity("testJob", "group1")
    .Build();

var trigger = TriggerBuilder.Create()
    .WithIdentity("testTrigger", "group1")
    .StartNow()
    .WithSimpleSchedule(x => x
        .WithRepeatCount(0))
    .Build();

await scheduler.ScheduleJob(job, trigger).ConfigureAwait(false);

using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await builder.RunAsync(cancellationTokenSource.Token).ConfigureAwait(false);
