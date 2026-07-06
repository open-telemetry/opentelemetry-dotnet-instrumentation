// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace TestApplication.ContinuousProfiler.Contention;

internal static class Program
{
    private static readonly object LockA = new();
    private static readonly object LockB = new();
    private static readonly object LockC = new();
    private static readonly object ConvoyLock = new();

    public static void Main(string[] args)
    {
        // ArgumentNullException.ThrowIfNull(args);

        var scenario = args.Length > 0 ? args[0] : "deadlock";

        using var activitySource = new ActivitySource("TestApplication.ContinuousProfiler.Contention", "1.0.0");
        using var activity = activitySource.StartActivity();

        switch (scenario)
        {
            case "deadlock":
                RunDeadlockScenario();
                break;
            case "convoy":
                RunConvoyScenario();
                break;
            default:
                RunDeadlockScenario();
                break;
        }
    }

    /// <summary>
    /// Classic 3-thread deadlock: A->B->C->A cycle.
    /// Each thread acquires its first lock then attempts to acquire the next,
    /// forming a circular wait chain. The app stays alive long enough for the
    /// persistence gate (2 ticks at ~1s each) to confirm the cycle.
    /// </summary>
    private static void RunDeadlockScenario()
    {
        using var barrier = new Barrier(3);

        var t1 = new Thread(() => DeadlockWorker("ShippingService", LockA, LockB, barrier))
        {
            Name = "ShippingService",
            IsBackground = true
        };
        var t2 = new Thread(() => DeadlockWorker("OrderService", LockB, LockC, barrier))
        {
            Name = "OrderService",
            IsBackground = true
        };
        var t3 = new Thread(() => DeadlockWorker("PaymentService", LockC, LockA, barrier))
        {
            Name = "PaymentService",
            IsBackground = true
        };

        t1.Start();
        t2.Start();
        t3.Start();

        // Wait long enough for:
        // - ContentionStart events to fire (~immediate)
        // - 2+ sampler ticks to pass the persistence gate (kPersistenceScans = 2)
        // - Export interval to flush the profile data
        // 5 seconds is generous for a 1s sampling interval + 500ms export interval.
        Thread.Sleep(TimeSpan.FromSeconds(5));

        // Threads are deadlocked; they will never exit on their own. The process
        // exits naturally since they are background threads.
    }

    private static void DeadlockWorker(string name, object firstLock, object secondLock, Barrier barrier)
    {
        // All threads acquire their first lock before any attempt the second.
        lock (firstLock)
        {
            barrier.SignalAndWait(); // synchronize: everyone holds their first lock
            lock (secondLock)
            {
                // Never reached in a deadlock
                Console.WriteLine($"{name} acquired both locks (unexpected)");
            }
        }
    }

    /// <summary>
    /// Lock convoy: one thread holds a lock for a long time while multiple threads
    /// queue behind it. The owner thread holds ConvoyLock and sleeps, so all waiters
    /// observe > 450ms stall (the kMinStall threshold).
    /// </summary>
    private static void RunConvoyScenario()
    {
        // Owner thread: grab the lock and hold it for a long time.
        using var ownerReady = new ManualResetEventSlim(false);
        var owner = new Thread(() =>
        {
            lock (ConvoyLock)
            {
                ownerReady.Set();
                // Hold the lock for 4 seconds - well above kMinStall (450ms)
                Thread.Sleep(TimeSpan.FromSeconds(4));
            }
        })
        {
            Name = "ConnectionPool",
            IsBackground = true
        };
        owner.Start();
        ownerReady.Wait();

        // Spawn waiters that will queue behind the owner.
        var waiters = new Thread[3];
        for (int i = 0; i < waiters.Length; i++)
        {
            var idx = i;
            waiters[i] = new Thread(() =>
            {
                lock (ConvoyLock)
                {
                    // Reached after owner releases
                    Console.WriteLine($"RequestHandler-{idx} acquired lock");
                }
            })
            {
                Name = $"RequestHandler-{idx}",
                IsBackground = true
            };
            waiters[i].Start();
        }

        // Wait for export to capture the stalled group.
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }
}
