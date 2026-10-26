---
title: Date and time
description: Predicates on DateTime that take a TimeProvider and handle DateTimeKind explicitly.
---

# Date and time

`DateTimeExtensions` in the namespace `Terp` (package `Terp`) adds predicates to `DateTime`.

```csharp
using Terp;
```

## Methods

| Method | Result |
|--------|--------|
| `IsToday(timeProvider?)` | the value falls on the current local calendar day of the provider's time zone |
| `IsInFuture(timeProvider?)` | the value is later than now |
| `IsPast(timeProvider?)` | the value is earlier than now |
| `IsBetween(lower, upper)` | the value lies within the inclusive range; the bounds may come in either order |
| `IsWeekend()` | Saturday or Sunday |
| `IsWorkday()` | not `IsWeekend()` |

`IsWorkday` knows weekends only. It does not know public holidays.

## Time source

Methods that need the current time take an optional `TimeProvider`. The default is `TimeProvider.System`. Pass a fake provider in tests.

```csharp
var provider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero));   // Microsoft.Extensions.TimeProvider.Testing

var due = new DateTime(2026, 3, 2, 18, 0, 0, DateTimeKind.Utc);
var soon = due.IsInFuture(provider);
```

## DateTimeKind

The methods that compare with the current time honour `DateTime.Kind`:

| Kind | Interpreted as |
|------|----------------|
| `Utc` | UTC |
| `Local` | local time of the system (`ToUniversalTime()`) |
| `Unspecified` | local time of `timeProvider.LocalTimeZone` |

`IsInFuture` and `IsPast` convert the value to UTC using these rules and compare it with `GetUtcNow()`. Both comparisons are strict, so a value equal to now is neither in the future nor in the past.

`IsToday` converts `Utc` values to the provider's local time zone. It compares other kinds as wall-clock time. It then compares the date with `GetLocalNow().Date`.

::: info Unspecified values
A `DateTime` read from a database often has kind `Unspecified`. Terp treats it as local time of the provider's time zone. Set the kind when you know it is UTC.
:::

`IsBetween`, `IsWeekend` and `IsWorkday` look at the value as given and ignore the kind.

```csharp
var start = new DateTime(2026, 3, 1);
var end = new DateTime(2026, 3, 31);
var inMarch = new DateTime(2026, 3, 15).IsBetween(end, start);   // true, bounds in either order
var weekend = new DateTime(2026, 3, 7).IsWeekend();             // true, a Saturday
```
