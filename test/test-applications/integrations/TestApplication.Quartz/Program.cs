// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
var scheduler = await schedulerFactory.GetScheduler();

var job = JobBuilder.Create<TestJob>()
    .WithIdentity("testJob", "group1")
    .Build();

var trigger = TriggerBuilder.Create()
    .WithIdentity("testTrigger", "group1")
    .StartNow()
    .WithSimpleSchedule(x => x
        .WithRepeatCount(0))
    .Build();

await scheduler.ScheduleJob(job, trigger);

using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await builder.RunAsync(cancellationTokenSource.Token);
