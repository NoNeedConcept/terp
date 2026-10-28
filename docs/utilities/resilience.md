---
title: Resilience
description: Exponential backoff delays with a cap, optional jitter and an injectable random source.
---

# Resilience

`Terp.Resilience` (package `Terp`) computes retry delays. It contains `Backoff`, a factory, and `BackoffPolicy`, the object it returns. It does not run retries for you; it tells you how long to wait.

```csharp
using Terp.Resilience;
```

## Create a policy

```csharp
var backoff = Backoff.Create(
    initialDelay: TimeSpan.FromMilliseconds(100),
    multiplier: 2,
    maxDelay: TimeSpan.FromSeconds(1));
```

| Parameter | Default | Meaning |
|-----------|---------|---------|
| `initialDelay` | required | delay for attempt 0 |
| `multiplier` | `2.0` | factor applied per attempt; values below 1 throw `ArgumentOutOfRangeException` |
| `maxDelay` | none (`TimeSpan.MaxValue`) | upper bound for every delay |
| `random` | `Random.Shared` | source for jitter |

## Delays

`Delay(attempt)` returns `initialDelay * multiplier^attempt`, capped at `maxDelay`. Attempts start at 0, and a negative attempt throws `ArgumentOutOfRangeException`.

```csharp
Console.WriteLine(string.Join(", ",
    Enumerable.Range(0, 5).Select(attempt => $"{backoff.Delay(attempt).TotalMilliseconds}ms")));
// 100ms, 200ms, 400ms, 800ms, 1000ms
```

`DelayWithJitter(attempt)` multiplies the base delay by a random factor between 0.75 and 1.25 and caps the result at `maxDelay`. Use it when many clients retry the same service, so they do not retry in step.

```csharp
for (var attempt = 0; ; attempt++)
{
    if (await TryOnceAsync(ct))
    {
        break;
    }

    await Task.Delay(backoff.DelayWithJitter(attempt), ct);
}
```

::: info Cap applies after jitter
With jitter the delay can reach `maxDelay` but never exceed it. Near the cap the spread shrinks to the downward side.
:::

## Overflow safety

The delay grows exponentially, so a high attempt number can overflow. When the computed delay is infinite or above `maxDelay`, `Delay` returns `maxDelay`. This also holds with the default `maxDelay` of `TimeSpan.MaxValue`, and `DelayWithJitter` does not throw at the cap.

```csharp
var unbounded = Backoff.Create(TimeSpan.FromSeconds(1));
var delay = unbounded.Delay(10_000);   // TimeSpan.MaxValue, no exception
```

## Deterministic tests

Pass your own `Random` to make jitter repeatable:

```csharp
var policy = Backoff.Create(TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30), random: new Random(42));
var first = policy.DelayWithJitter(3);
```

A policy holds no state besides its settings, so one instance can serve many callers.

::: info Where Terp uses it
The [startup gates](/hosting/gates) wait with `Delay` (no jitter): 1 second initial delay, doubling, capped at 30 seconds.
:::

For restarting Akka actors with backoff see [Backoff helpers](/akka/backoff).
