---
tags: [project, windows-audio, switch-windows-audio-output]
status: testing
created: 2026-05-29
---

# Left/Right Speaker Swap on Windows — test plan & handoff

> [!abstract] The goal
> Be **the answer** for the next person who Googles *"left right speaker doesn't work Windows."*
> Google is full of unverified, often-wrong fixes. This repo's value is a **verified** answer + a tiny tool that makes the real fix one click.
> Repo name: `switch-windows-audio-output`

This note is the working doc to run on the **Windows** machine after reboot. Tick the boxes as you go and paste results inline.

---

## Where we landed (decisions)

- **Not** building a personal hotkey/tray toggle for *switching default output device* — that was a wrong turn. The real problem is **swapping the L/R stereo channels** of one pair of speakers.
- The deliverable is **"the answer"** (a clear, tested writeup), with a small tool in service of it — not "download my app."
- **Open fork** (decide after testing):
  - **Option A** — Definitive writeup + a one-click tool that toggles an **Equalizer APO** config. Ships this weekend. Cost: user installs Equalizer APO once.
  - **Option B** — Self-contained, zero-dependency app (package + **code-sign your own APO**, based on Microsoft's `SwapAPO` sample). Much bigger; the signing step is a real bureaucratic wall. End-user payoff over A is just "one fewer install."
- Leaning **A**, but testing may change this (e.g. if your hardware has a usable native option).

---

## What's already known (from web research — to be verified)

| Claim | Status | Note |
|---|---|---|
| Windows 11 has a native channel-swap **setting** | ❌ likely no | No reliable user-facing toggle found |
| The **balance slider** (L=0 / R=100) swaps channels | ❌ **myth** | It **mutes** one side; it does **not** move audio to the other speaker. We want to debunk this with receipts. |
| Realtek Audio Console can swap L/R | ❌ mostly gone | Old Realtek HD panel had it; modern software removed it for most codecs |
| `PKEY_Endpoint_Enable_Channel_Swap_SFX` = a user toggle | ❌ no | It's an INF/driver-level property tied to MS's `SwapAPO`, baked in at driver install — not a runtime switch |
| **Swap the physical cables** works | ✅ yes | The honest free fix when reachable |
| **Equalizer APO** + `L=R R=L` swaps channels | ✅ yes (to confirm) | The reliable universal software fix |

Sources: [tenforums](https://www.tenforums.com/sound-audio/217990-swap-left-right-speaker-channels.html) · [windowsreport / Realtek](https://windowsreport.com/realtek-audio-console-swap-left-right/) · [MS driver docs / SwapAPO](https://github.com/MicrosoftDocs/windows-driver-docs/blob/staging/windows-driver-docs-pr/audio/windows-11-apis-for-audio-processing-objects.md) · [ms.codes / balance myth](https://ms.codes/blogs/windows/windows-11-swap-left-and-right-audio)

---

## Test checklist (do these on Windows)

> [!tip] First, get an unambiguous signal
> You can't judge a swap by ear on music. Use a **stereo channel-identification** signal — a YouTube "left right stereo test" that says *"left"* out of one speaker and *"right"* out of the other. (Claude can also generate a labeled stereo WAV.)

### 1. Does YOUR hardware have a native option? (~2 min)
- [x] Settings → System → Sound → (your output device) → **Properties** — any channel/swap control?
- [x] Sound → **More sound settings** (classic panel) → device → Properties tabs — anything?
- [x] Your audio driver's own app (Realtek Audio Console / Dolby / etc.) — any swap?
- **Result:** ❌ No native channel-swap option found in any of the three locations.

### 2. Debunk the balance slider (~2 min)
- [x] Classic Sound panel → device → Properties → **Levels** → **Balance** → set Left **0**, Right **100**
- [x] Play the L/R test signal. Does the left speaker go **silent** (myth confirmed) or does left's audio come out the **right** speaker (actual swap)?
- **Result:** ❌ Myth confirmed. Setting one channel to 0 simply **mutes** that speaker — audio does not move to the other side.

### 3. Equalizer APO — the linchpin (~15 min, needs a reboot)
- [x] Download & install Equalizer APO
- [x] During install, **select your output device** to hook
- [x] ~~**Reboot**~~ — installer did not require a reboot; APO worked immediately
- [x] Add the swap config (see snippet below)
- [x] Play the L/R test signal — do channels actually swap?
- [x] **Make-or-break for the tool:** edit `config.txt` while audio plays — does it **hot-reload live** (no reboot)? This decides whether a one-click toggle app is trivially real.
- **Result:** ✅ `Copy: L=R R=L` swaps channels correctly. ✅ Config **hot-reloads instantly** — adding/removing the line while audio plays takes effect immediately with no reboot or service restart.

### 4. Capture config details for the tool
- [x] Exact config folder path: `C:\Program Files\EqualizerAPO\config\`
- [x] How the main `config.txt` / `Include:` mechanism works: APO reads `config.txt` as the entry point; it supports `Include:` directives to pull in other files. Default contents are a preamp, an include of `example.txt`, and a flat graphic EQ.
- [x] Confirm that simply **writing the file** is enough to apply: ✅ Yes — saving the file triggers an immediate live reload.
- **Result:** ✅ All details captured. Tool just needs to prepend/remove `Copy: L=R R=L` from `config.txt`.

---

## Equalizer APO config snippet

The whole swap is one line. Put it in a config file (e.g. `config\config.txt`):

```text
# Swap left and right channels
Copy: L=R R=L
```

The tool's job (Option A) is just to write/clear this file to toggle:
- **Swapped:** file contains `Copy: L=R R=L`
- **Normal:** file empty (or the line removed)

---

## After testing — next steps
- [x] Decide Option A vs B based on results (esp. whether hot-reload works) — **Option A wins.** Hot-reload works, so the tool is just a file edit. No reason to pursue Option B.
- [ ] Scaffold the repo (`switch-windows-audio-output`): README = the verified answer; small C# tool for the toggle
- [ ] Write the README as the SEO-friendly "answer" (debunk balance myth → cables → Equalizer APO + tool)

## Results log
- **2026-05-29:** All four tests completed. Channels confirmed swapped on hardware. No native fix exists. Balance slider myth debunked (mutes, doesn't swap). Equalizer APO `Copy: L=R R=L` works perfectly and hot-reloads instantly. **Option A confirmed — proceeding with Equalizer APO toggle tool.**
