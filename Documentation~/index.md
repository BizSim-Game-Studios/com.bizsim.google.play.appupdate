# BizSim Google Play In-App Updates

Last reviewed: 2026-04-16

## Overview

This package provides a production-ready Unity bridge for the Google Play In-App Updates API
(v2.1.0). It wraps the native `AppUpdateManager` via a JNI bridge with a headless fragment
shim for `onActivityResult` interception, exposes both flexible (background download) and
immediate (full-screen blocking) update flows, and ships with a smart policy engine, install
state streaming, install-source detection, and a mock provider for editor testing.

The package compiles only for Android and Editor platforms. On non-Android builds and in the
Unity Editor, the mock provider is used automatically so you can iterate without a device.

## Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Step-by-step installation and first API call |
| [api-reference.md](api-reference.md) | Full public API surface with types, methods, and parameters |
| [configuration.md](configuration.md) | AppUpdateSettings asset fields and Editor window walkthrough |
| [architecture.md](architecture.md) | JNI bridge diagram, thread model, fragment shim, provider selection |
| [troubleshooting.md](troubleshooting.md) | Common errors with root causes and fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form input |

## Additional documentation

| File | Description |
|------|-------------|
| [TELEMETRY-DASHBOARD.md](TELEMETRY-DASHBOARD.md) | Firebase Analytics event reference and dashboard setup |
| [UNITY_ACTIVITY_OVERRIDE.md](UNITY_ACTIVITY_OVERRIDE.md) | How to make classic UnityPlayerActivity extend FragmentActivity |
| [UPGRADE-1.x.md](UPGRADE-1.x.md) | Migration guide for 1.x releases |

## Links

- [README](../README.md) — Quick-start experience and feature overview
- [CHANGELOG](../CHANGELOG.md) — Release history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate)
