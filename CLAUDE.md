# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A Windows tool to swap left/right audio channels. Two components:

- **SwapAPO.dll** (C++, `src/SwapAPO/`): A Windows Audio Processing Object (APO) — a COM DLL loaded by `audiodg.exe` to process audio in real-time. Swaps L/R samples in the audio buffer.
- **SwapAudio.exe** (C#, `src/SwapAudio/`): Installer and toggle tool. Embeds the DLL, handles registry setup, COM/APO registration, and audio service restart.

## Build Commands

```bash
# Build APO DLL (requires MinGW GCC — comes with Strawberry Perl on this machine)
cmake -B build -S src/SwapAPO -G "MinGW Makefiles"
cmake --build build

# Copy built DLL into C# project for embedding
cp build/SwapAPO.dll src/SwapAudio/SwapAPO.dll

# Build and publish C# exe (single-file, trimmed, self-contained)
dotnet publish src/SwapAudio/ -c Release
# Output: src/SwapAudio/bin/Release/net8.0-windows/win-x64/publish/SwapAudio.exe
```

## Architecture

### APO DLL (C-style COM, raw vtables)

The DLL implements COM interfaces using **manual C-style vtable structs** — not C++ inheritance. This avoids MinGW/MSVC vtable layout mismatches. Key interfaces:

- `IAudioProcessingObject` — format negotiation, initialization
- `IAudioProcessingObjectRT` — real-time `APOProcess()` that swaps L/R
- `IAudioProcessingObjectConfiguration` — `LockForProcess`/`UnlockForProcess`
- `IAudioSystemEffects` — marker interface

The audio engine **aggregates** APOs (passes non-null `pOuter` to `CreateInstance`). The DLL implements proper COM aggregation with a non-delegating IUnknown.

CLSID: `{1B5C2483-B741-4C18-9B0E-8B07FF3CA0F2}`

### C# Installer

Registers the APO by writing to:
- `HKLM\SOFTWARE\Classes\CLSID\{guid}\InprocServer32` — COM registration
- `HKLM\SOFTWARE\Classes\AudioEngine\AudioProcessingObjects\{guid}` — APO registration
- `HKLM\...\MMDevices\Audio\Render\{device}\FxProperties` — endpoint hookup
- `HKLM\...\Audio\DisableProtectedAudioDG = 1` — allows unsigned APOs

## Key Constraints and Gotchas

- **MMDevices registry keys** have restricted ACLs. Admins can modify values (`SetValue`) but NOT create/delete keys. Use `OpenSubKey(path, ReadWriteSubTree, QueryValues | SetValue | EnumerateSubKeys)` — never request `RegistryRights.Delete` on these keys.
- **FxProperties slot `{d04e05a6...},5`** is `PKEY_FX_StreamEffectClsid` (SFX). Replacing it removes the original format-conversion APO, breaking the audio pipeline unless you chain to the original.
- **`{d3993a3f...},5`** is `PKEY_SFX_ProcessingModes_Supported_For_Streaming` — NOT a CLSID slot. It contains processing mode GUIDs like `AUDIO_SIGNALPROCESSINGMODE_DEFAULT`.
- **Composite FX (`{d04e05a6...},13`)** only works on endpoints where the driver originally configured it. Can't be added after the fact on legacy-only endpoints.
- **Restarting `Audiosrv`** preserves FxProperties. Killing `audiodg.exe` alone doesn't cause the audio engine to re-read FxProperties.
- The DLL must be **statically linked** (`-static-libgcc -static-libstdc++ -static`) — `audiodg.exe` won't find MinGW runtime DLLs.
- `APO_FLAG_INPLACE` (0x01) is required — the original SFX APOs have it and the engine expects it.

## Current Status (WIP)

The APO DLL loads in audiodg, passes COM aggregation, gets `Initialize` and `IsInputFormatSupported` called, but the engine never calls `LockForProcess`. Root cause under investigation — likely the engine needs the APO to chain/wrap the original SFX APO (like Equalizer APO does) rather than replace it outright.
