# Swap Left and Right Speakers on Windows

Windows has no setting to swap stereo channels. The balance slider doesn't work — it mutes one side instead of swapping. Here's what actually works.

## Download the Toggle Tool

**[Download SwapAudio.exe](https://github.com/OrientedDeer/switch-windows-audio-output/releases/latest/download/SwapAudio.exe)** — run once to swap, run again to swap back. Requires [Equalizer APO](https://sourceforge.net/projects/equalizerapo/).

## Manual Method

1. Install [Equalizer APO](https://sourceforge.net/projects/equalizerapo/) (select your output device during setup)
2. Open `C:\Program Files\EqualizerAPO\config\config.txt` as Administrator
3. Add this line at the top:
   ```
   Copy: L=R R=L
   ```
4. Save. Takes effect immediately. Remove the line to undo.

## Build from Source

```
dotnet publish src/SwapAudio/ -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
