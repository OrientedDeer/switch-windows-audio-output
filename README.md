# Swap Left and Right Speakers on Windows

Your left and right speakers are backwards and Windows has no setting to fix it. Here's the verified answer.

**Quick version:** If you can reach the cables, swap them physically. If you can't (laptop speakers, sealed desk setup, Bluetooth), install [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) and add one config line. Or download the toggle tool below.

## The Balance Slider Myth

Every forum thread says "just drag the balance slider all the way left or right." This does not swap your channels. It **mutes** one side. The audio from the muted speaker does not move to the other one — it disappears.

Tested on Windows 11 (May 2026): Settings > Sound > device Properties > Balance. Setting Left to 0 and Right to 100 silences the left speaker entirely. Right still plays right-channel audio only. No swap occurs.

## How to Swap Left and Right Audio Channels

### 1. Install Equalizer APO

Download [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) and run the installer. Select your output device during setup. No reboot required on most systems.

### 2. Edit the config

Open `C:\Program Files\EqualizerAPO\config\config.txt` in a text editor (as Administrator). Add this line at the top:

```
Copy: L=R R=L
```

Save the file. The swap takes effect immediately — no restart, no reboot. Equalizer APO hot-reloads its config on every file save.

### 3. To undo

Delete or comment out the `Copy: L=R R=L` line and save. Channels return to normal instantly.

## One-Click Toggle Tool

Don't want to edit config files by hand? Download `SwapAudio.exe` from [Releases](../../releases).

Run it once → channels swap. Run it again → channels go back to normal. That's it.

Requires [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) to be installed. The tool will tell you if it's missing.

### Build from source

```
dotnet publish src/SwapAudio/ -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The exe lands in `src/SwapAudio/bin/Release/net8.0/win-x64/publish/SwapAudio.exe`.

## How It Works

Equalizer APO is a system-wide audio processing driver. The line `Copy: L=R R=L` tells it to copy the left channel to the right output and the right channel to the left output — a channel swap. The config file is monitored for changes and reloaded live.

The toggle tool just adds or removes that one line from the config file.
