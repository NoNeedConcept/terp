---
layout: home
title: Terp
titleTemplate: Small foundation libraries for .NET
description: Small foundation libraries for .NET apps. Host setup, startup gates, metrics helpers, backoff and Akka.NET helpers.
hero:
  name: Terp
  text: Small foundation libraries for .NET
  tagline: Host setups, startup gates, metrics helpers, backoff and Akka.NET helpers.
  image:
    src: /logo.svg
    alt: Terp
  actions:
    - theme: brand
      text: Getting started
      link: /getting-started
    - theme: alt
      text: Hosting
      link: /hosting/
---

## Packages

| Package | What it holds | Version | Install |
| --- | --- | --- | --- |
| `Terp` | Host setups and startup gates, diagnostics and metrics helpers, resilience, network and date helpers. Depends on the Microsoft.Extensions abstractions only. | <PackageVersion package="Terp" bare /> | <InstallCommand cmd="dotnet add package Terp" /> |
| `Terp.Akka` | Akka.NET helpers on Akka.Hosting: actor system setup, resolving actors, a registry and backoff supervision. Builds on `Terp`. | <PackageVersion package="Terp.Akka" bare /> | <InstallCommand cmd="dotnet add package Terp.Akka" /> |

Both packages share one version and are always released together.

## Where to start

- [Getting started](/getting-started) shows a host with one setup and one startup gate.
- [Hosting](/hosting/) explains setups, their order and gates.
- [Terp.Akka](/akka/) covers the actor system setup and its helpers.
