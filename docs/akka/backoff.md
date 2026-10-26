---
title: Backoff helpers
description: Wrap Props in a BackoffSupervisor with sensible defaults for restarts after failure or stop.
---

# Backoff helpers

`BackoffExtensions` (package `Terp.Akka`) wraps the `Props` of an actor in an Akka `BackoffSupervisor`. When the child fails or stops, the supervisor restarts it after a delay that grows exponentially. The extensions return `Props`, so you can pass them to `ActorOf`.

```csharp
using Akka.Actor;
using Terp.Akka;
```

## BackoffOnFailure

`BackoffOnFailure` restarts the child when it fails, that is, when it throws.

```csharp
var flaky = system.ActorOf(
    Props.Create(() => new FlakyActor(result))
        .BackoffOnFailure("flaky", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), randomFactor: 0.1));
```

`"flaky"` is the name of the supervised child. The name you give to `ActorOf` is the name of the supervisor. Messages sent to the supervisor go to the child.

## BackoffOnStop

`BackoffOnStop` restarts the child when it stops.

```csharp
var props = Props.Create<ConnectionActor>().BackoffOnStop("connection");
```

::: info Choosing one
Use `BackoffOnFailure` for actors that crash on errors. Use `BackoffOnStop` for actors that end themselves, for example when a connection closes, and should be started again.
:::

## Defaults

Both methods have the same parameters and defaults:

| Parameter | Default |
|-----------|---------|
| `childName` | required |
| `minBackoff` | 3 seconds |
| `maxBackoff` | 30 seconds |
| `randomFactor` | `0.2` |
| retries | unlimited (`maxNrOfRetries` is `-1`) |
| `configure` | none |

`randomFactor` adds random jitter to the delay as a fraction. `minBackoff` and `maxBackoff` accept `null` for the default.

## Adjust the options

The optional `configure` function receives the `BackoffOptions` before the supervisor props are created and returns the changed options. Use it for anything the parameters do not cover, such as a retry limit:

```csharp
var props = Props.Create<PollWorker>()
    .BackoffOnFailure("poll", configure: options => options.WithMaxNrOfRetries(5));
```

::: warning Messages during the backoff
While the child is down, messages to the supervisor are not delivered to it and end up in dead letters. `src/Terp.Akka.Example` sends the message again until the child answers.
:::

For delay calculation outside Akka see [Resilience](/resilience/).
